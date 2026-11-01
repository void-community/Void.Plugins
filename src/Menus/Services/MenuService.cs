using System.Diagnostics;
using Menus.Api;
using Menus.Events;
using Menus.Minecraft;
using Menus.Minecraft.Components.Item;
using Menus.Protocol.Packets.Clientbound;
using Menus.Protocol.Packets.Serverbound;
using Microsoft.Extensions.Logging;
using Void.Data.Api.Minecraft;
using Void.Minecraft.Events;
using Void.Minecraft.Links.Extensions;
using Void.Minecraft.Network;
using Void.Minecraft.Players.Extensions;
using Void.Proxy.Api.Commands;
using Void.Proxy.Api.Events;
using Void.Proxy.Api.Events.Network;
using Void.Proxy.Api.Events.Player;
using Void.Proxy.Api.Events.Services;
using Void.Proxy.Api.Links;
using Void.Proxy.Api.Network;
using Void.Proxy.Api.Players.Contexts;

namespace Menus.Services;

public class MenuService(ILogger<MenuService> logger, IPlayerContext playerContext, ICommandService commandService, ILinkService linkService, IEventService events) : IEventListener
{
  // Vanilla servers cycle container ids through 1..100, so 127 never collides with a backend container
  // and still fits into the signed byte used by older protocol versions.
  private const int MenuContainerId = 127;
  private const int PlayerInventoryContainerId = 0;
  private const int CursorContainerId = -1;
  // Used by vanilla 1.17.1 to 1.21.1 backends to set player inventory slots regardless of the open container
  private const int PlayerInventoryDirectContainerId = -2;
  private const int CursorSlot = -1;
  private const int OutsideSlot = -999;

  // Container sizes the client expects, for types whose size doesn't change between versions
  private static readonly Dictionary<Identifier, int> KnownMenuSizes = new()
  {
    ["minecraft:generic_9x1"] = 9,
    ["minecraft:generic_9x2"] = 18,
    ["minecraft:generic_9x3"] = 27,
    ["minecraft:generic_9x4"] = 36,
    ["minecraft:generic_9x5"] = 45,
    ["minecraft:generic_9x6"] = 54,
    ["minecraft:generic_3x3"] = 9,
    ["minecraft:shulker_box"] = 27,
    ["minecraft:hopper"] = 5,
    ["minecraft:anvil"] = 3,
    ["minecraft:beacon"] = 1,
    ["minecraft:brewing_stand"] = 5,
    ["minecraft:enchantment"] = 2,
    ["minecraft:furnace"] = 3,
    ["minecraft:blast_furnace"] = 3,
    ["minecraft:smoker"] = 3,
    ["minecraft:grindstone"] = 3,
    ["minecraft:crafting"] = 10
  };

  private static Menu? defaultInventoryMenu;

  private Menu? ownedInventoryMenu;

  // The backend sent the player inventory since joining, so the client is ready for inventory packets
  private bool inventoryInitialized;

  // Backend containers show the real player inventory below them
  private bool backendContainerOpen;

  // Clients echo it back in clicks, vanilla wraps it the same way
  private int stateId;

  // Each backend resync sends the whole inventory and fires a click event on Paper, so clicking fast sends at most one per tick
  private static readonly TimeSpan BackendResyncInterval = TimeSpan.FromMilliseconds(50);
  private long lastBackendResyncTimestamp;
  private int backendResyncScheduled;

  // The player clicked in their real inventory since a delayed resync was scheduled
  private int playerInventoryClickForwarded;

  public Menu? InventoryMenu => ownedInventoryMenu ?? defaultInventoryMenu;
  private Menu? currentMenu;

  public Menu? CurrentMenu => currentMenu;

  /// <summary>
  /// Shows <paramref name="menu"/> as the player inventory. While it is set, the proxy owns the player inventory:
  /// backend inventory updates and the player's own inventory changes are blocked, and the menu is shown again after
  /// backend containers close. Pass <c>null</c> to hand the inventory back to the backend.
  /// Can be called before the player joins a server, for example on <c>PlayerJoinedServerEvent</c>: the menu is then
  /// shown once the backend sends the player inventory.
  /// </summary>
  public async ValueTask SetInventoryMenuAsync(Menu? menu, CancellationToken cancellationToken)
  {
    var wasOwned = ownedInventoryMenu is not null;
    ownedInventoryMenu = menu;

    // Otherwise the backend sending the player inventory or closing its container shows the menu
    if (menu is not null)
    {
      if (inventoryInitialized && !backendContainerOpen)
        await RestoreInventoryMenuAsync(menu, cancellationToken);

      return;
    }

    // Right after joining, the backend sends the real inventory anyway.
    // Backends older than 1.17.1 show the real items again on their next inventory update, for example on server switch.
    if (wasOwned && inventoryInitialized)
      await RequestBackendResyncAsync(cancellationToken);
  }

  public async ValueTask OpenAsync(Menu menu, CancellationToken cancellationToken)
  {
    var player = playerContext.Player;

    var isPlayerInventory = menu.Type == "minecraft:inventory";
    Menu? previousMenu = null;

    if (!isPlayerInventory)
    {
      if (KnownMenuSizes.TryGetValue(menu.Type, out var expectedSize) && menu.Size != expectedSize)
        logger.LogWarning("Menu {Menu} of type {Type} has size {Size}, the client expects {ExpectedSize}", menu.Name, menu.Type, menu.Size, expectedSize);

      previousMenu = Interlocked.Exchange(ref currentMenu, menu);

      // Opening a screen replaces whatever the backend had open, so close it on the backend too.
      // This keeps the backend on the player inventory, which ResyncAsync relies on.
      if (linkService.TryGetLink(player, out var link))
        await link.SendPacketAsync(new CloseContainerServerboundPacket { ContainerId = PlayerInventoryContainerId }, cancellationToken);

      backendContainerOpen = false;

      logger.LogTrace("Opening menu {Menu} for {Player}", menu.Name, player.Profile?.Username ?? "unknown");
      var openContainerPacket = new OpenContainerClientboundPacket
      {
        ContainerId = MenuContainerId,
        Type = MinecraftMenuRegistry.GetId(ProtocolVersion.Latest, menu.Type),
        Title = menu.Title
      };
      await player.SendPacketAsync(openContainerPacket, cancellationToken);
    }

    await UpdateSlotsAsync(menu, cancellationToken);

    // Raised once the client shows the menu, so handlers can change or close it
    if (!isPlayerInventory)
      await RaiseMenuChangedAsync(previousMenu, menu, cancellationToken);
  }

  public async ValueTask CloseAsync(CancellationToken cancellationToken)
  {
    if (Interlocked.Exchange(ref currentMenu, null) is not { } menu)
      return;

    var closeContainerPacket = new CloseContainerClientboundPacket
    {
      ContainerId = MenuContainerId
    };

    await playerContext.Player.SendPacketAsync(closeContainerPacket, cancellationToken);

    // Raised once the client closed the menu, so handlers can open another one
    await RaiseMenuChangedAsync(menu, null, cancellationToken);
  }

  [Obsolete($"Use {nameof(SetPropertyAsync)} instead.")]
  public ValueTask SetProperty(int property, int value, CancellationToken cancellationToken)
  {
    return SetPropertyAsync(property, value, cancellationToken);
  }

  public async ValueTask SetPropertyAsync(int property, int value, CancellationToken cancellationToken)
  {
    if (CurrentMenu is null)
      return;

    var setContainerPropertyPacket = new SetContainerPropertyClientboundPacket
    {
      ContainerId = MenuContainerId,
      Property = property,
      Value = value
    };

    await playerContext.Player.SendPacketAsync(setContainerPropertyPacket, cancellationToken);
  }

  /// <summary>
  /// Sets or clears a slot of the open menu without reopening it. The menu passed to <see cref="OpenAsync"/> is not changed,
  /// <see cref="CurrentMenu"/> and the menu in later events are a copy with the new item.
  /// </summary>
  public async ValueTask SetItemAsync(int slot, MenuItem? item, CancellationToken cancellationToken)
  {
    if (CurrentMenu is not { } menu)
      return;

    ArgumentOutOfRangeException.ThrowIfNegative(slot);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, menu.Size);

    var itemsMap = new Dictionary<int, MenuItem>(menu.ItemsMap);

    if (item is null)
      itemsMap.Remove(slot);
    else
      itemsMap[slot] = item;

    var updatedMenu = menu with { ItemsMap = itemsMap };

    // The menu may have been closed or replaced meanwhile, for example from a timer.
    // Same menu for the player, so no opened or closed events.
    if (Interlocked.CompareExchange(ref currentMenu, updatedMenu, menu) != menu)
      return;

    await UpdateSlotAsync(updatedMenu, slot, cancellationToken);
  }

  public async ValueTask UpdateSlotsAsync(Menu menu, CancellationToken cancellationToken)
  {
    var isInventory = menu.Type == "minecraft:inventory";

    // Only the menu slots are sent, clients fill slots in order and keep the remaining player inventory slots as is
    if (!isInventory)
      await SendContentAsync(MenuContainerId, menu, menu.Size, cancellationToken);

    if (InventoryMenu is null)
      return;

    await SendContentAsync(PlayerInventoryContainerId, InventoryMenu, GetInventoryMenuSize(InventoryMenu), cancellationToken);
  }

  public async ValueTask UpdateSlotAsync(Menu menu, int slot, CancellationToken cancellationToken)
  {
    var isPlayerInventory = menu.Type == "minecraft:inventory";
    var itemStack = CreateItemStack(menu, slot);

    var containerId = isPlayerInventory ? PlayerInventoryContainerId : MenuContainerId;

    logger.LogTrace("Setting slot {Slot} in container {ContainerId} to {Item} for {Player}", slot, containerId, itemStack?.Identifier.ToString() ?? "air", playerContext.Player.Profile?.Username ?? "unknown");
    var setSlotPacket = new SetContainerSlotClientboundPacket
    {
      ContainerId = containerId,
      StateId = NextStateId(),
      Slot = slot,
      ItemStack = itemStack
    };

    await playerContext.Player.SendPacketAsync(setSlotPacket, cancellationToken);
  }

  public ValueTask ClickSlotAsync(Menu menu, int slot, ClickType clickType, CancellationToken cancellationToken)
  {
    return ClickSlotAsync(menu, slot, clickType, hotbarSlot: null, cancellationToken);
  }

  private async ValueTask ClickSlotAsync(Menu menu, int slot, ClickType clickType, int? hotbarSlot, CancellationToken cancellationToken)
  {
    var isPlayerInventory = menu.Type == "minecraft:inventory";

    // Slots past a container menu belong to the player inventory below it
    if (!isPlayerInventory && slot >= menu.Size)
    {
      if (InventoryMenu is null)
        return;

      slot = slot - menu.Size + InventoryMenu.Size;
      menu = InventoryMenu;
    }

    if (!menu.ItemsMap.TryGetValue(slot, out var item))
      return;

    var click = new MenuClick(playerContext.Player, menu, slot, item, clickType, hotbarSlot);
    await RaiseAsync(new MenuClickedEvent(click), cancellationToken);

    if (item.OnClick is not null)
    {
      // Clicks are handled inside the packet pipeline, which must not see plugin exceptions
      try
      {
        await item.OnClick(click, cancellationToken);
      }
      catch (Exception exception) when (exception is not OperationCanceledException)
      {
        logger.LogError(exception, "Click handler of slot {Slot} in menu {Menu} failed", slot, menu.Name);
      }
    }

    switch (clickType)
    {
      case ClickType.RightMouseButton when item.RightClickCommand is not null:
        await commandService.ExecuteAsync(playerContext.Player, item.RightClickCommand, cancellationToken);
        break;
      case ClickType.LeftMouseButton when item.LeftClickCommand is not null:
        await commandService.ExecuteAsync(playerContext.Player, item.LeftClickCommand, cancellationToken);
        break;
    }
  }

  private async ValueTask SendContentAsync(int containerId, Menu menu, int size, CancellationToken cancellationToken)
  {
    logger.LogTrace("Setting {Size} slots in container {ContainerId} from menu {Menu} for {Player}", size, containerId, menu.Name, playerContext.Player.Profile?.Username ?? "unknown");
    var setContentPacket = new SetContainerContentClientboundPacket
    {
      ContainerId = containerId,
      StateId = NextStateId(),
      ItemStacks = Enumerable.Range(0, size).Select(slot => CreateItemStack(menu, slot)).ToList()
    };

    await playerContext.Player.SendPacketAsync(setContentPacket, cancellationToken);
  }

  // The client applies a click to its own copy of the container before the server confirms it,
  // so a cancelled click has to be undone by resending everything it may have touched.
  private async ValueTask ResyncAsync(Menu menu, CancellationToken cancellationToken)
  {
    var player = playerContext.Player;

    // Restores the menu slots and clears the cursor on 1.17.1+
    await SendContentAsync(MenuContainerId, menu, menu.Size, cancellationToken);

    // Older clients have no cursor item in container content
    await player.SendPacketAsync(new SetContainerSlotClientboundPacket { ContainerId = CursorContainerId, Slot = CursorSlot }, cancellationToken);

    if (InventoryMenu is not null)
    {
      await SendContentAsync(PlayerInventoryContainerId, InventoryMenu, GetInventoryMenuSize(InventoryMenu), cancellationToken);
      return;
    }

    await RequestBackendResyncAsync(cancellationToken);
  }

  private async ValueTask RestoreInventoryMenuAsync(Menu inventoryMenu, CancellationToken cancellationToken)
  {
    // Clears the cursor on 1.17.1+
    await SendContentAsync(PlayerInventoryContainerId, inventoryMenu, GetInventoryMenuSize(inventoryMenu), cancellationToken);

    // Older clients have no cursor item in container content
    await playerContext.Player.SendPacketAsync(new SetContainerSlotClientboundPacket { ContainerId = CursorContainerId, Slot = CursorSlot }, cancellationToken);
  }

  private async ValueTask RequestBackendResyncAsync(CancellationToken cancellationToken)
  {
    var player = playerContext.Player;

    // Older backends don't resync on a stale state id, and the click below has no downgrade transformations
    if (player.ProtocolVersion < ProtocolVersion.MINECRAFT_1_17_1)
      return;

    var lastTimestamp = Interlocked.Read(ref lastBackendResyncTimestamp);
    var elapsed = Stopwatch.GetElapsedTime(lastTimestamp);

    // Claiming the timestamp keeps concurrent requests from both sending right away
    if (elapsed >= BackendResyncInterval && Interlocked.CompareExchange(ref lastBackendResyncTimestamp, Stopwatch.GetTimestamp(), lastTimestamp) == lastTimestamp)
    {
      await SendBackendResyncAsync(cancellationToken);
      return;
    }

    // The answer to the last resync may reach the client before this click's prediction, so send one more once the interval passed
    if (Interlocked.Exchange(ref backendResyncScheduled, 1) != 0)
      return;

    Interlocked.Exchange(ref playerInventoryClickForwarded, 0);

    var delay = BackendResyncInterval - Stopwatch.GetElapsedTime(Interlocked.Read(ref lastBackendResyncTimestamp));
    _ = SendDelayedBackendResyncAsync(delay > TimeSpan.Zero ? delay : TimeSpan.Zero);
  }

  private async Task SendDelayedBackendResyncAsync(TimeSpan delay)
  {
    // The player context may be gone once the delay passed
    var playerName = playerContext.Player.Profile?.Username ?? "unknown";

    try
    {
      await Task.Delay(delay);

      // Claim the timestamp before allowing new requests, so a request in between waits instead of sending right away
      Interlocked.Exchange(ref lastBackendResyncTimestamp, Stopwatch.GetTimestamp());
      Interlocked.Exchange(ref backendResyncScheduled, 0);

      if (!IsDelayedBackendResyncSafe())
        return;

      // The player may have disconnected meanwhile, so the request's cancellation token is not used
      await SendBackendResyncAsync(CancellationToken.None);
    }
    catch (Exception exception)
    {
      logger.LogDebug(exception, "Delayed backend inventory resync for {Player} failed", playerName);
    }
  }

  // Things may have changed during the delay, and the resync click is not harmless in every state
  private bool IsDelayedBackendResyncSafe()
  {
    // The backend drops a stack the player holds on its cursor in their real inventory,
    // and answers the player's own clicks there anyway
    if (Volatile.Read(ref playerInventoryClickForwarded) != 0)
      return false;

    // The backend ignores clicks for container 0 while its own container is open
    if (backendContainerOpen)
      return false;

    // Play packets would disconnect a player the backend moved back to configuration
    return playerContext.Player.Phase == Phase.Play;
  }

  private async ValueTask SendBackendResyncAsync(CancellationToken cancellationToken)
  {
    if (!linkService.TryGetLink(playerContext.Player, out var link))
      return;

    // The proxy doesn't know the real player inventory, so ask the backend to resend it.
    // Clicking outside the inventory with an empty cursor changes nothing, and the stale state id
    // makes the backend answer with a full inventory resync.
    var resyncClickPacket = new ClickContainerServerboundPacket
    {
      ContainerId = PlayerInventoryContainerId,
      StateId = -1,
      Slot = OutsideSlot,
      Button = 0,
      Mode = 0,
      SkippedBytes = [
        0x00, // No changed slots
        0x00 // Empty carried item
      ]
    };

    await link.SendPacketAsync(resyncClickPacket, cancellationToken);
  }

  private async ValueTask RaiseMenuChangedAsync(Menu? previousMenu, Menu? menu, CancellationToken cancellationToken)
  {
    if (previousMenu is not null)
      await RaiseAsync(new MenuClosedEvent(playerContext.Player, previousMenu), cancellationToken);

    // A closed handler may have opened another menu already
    if (menu is not null && currentMenu == menu)
      await RaiseAsync(new MenuOpenedEvent(playerContext.Player, menu), cancellationToken);
  }

  // The client already closed the menu, for example because the backend opened a container
  private async ValueTask ClearCurrentMenuAsync(CancellationToken cancellationToken)
  {
    if (Interlocked.Exchange(ref currentMenu, null) is { } menu)
      await RaiseAsync(new MenuClosedEvent(playerContext.Player, menu), cancellationToken);
  }

  private async ValueTask RaiseAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : IScopedEvent
  {
    // Menu events are raised inside the packet pipeline, which must not see plugin exceptions
    try
    {
      await events.ThrowAsync(@event, cancellationToken);
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
      logger.LogError(exception, "Handling {Event} failed", typeof(TEvent).Name);
    }
  }

  private int NextStateId()
  {
    return Interlocked.Increment(ref stateId) & 0x7FFF;
  }

  private static ClickType? GetClickType(int mode, int button)
  {
    return (mode, button) switch
    {
      (0, 0) => ClickType.LeftMouseButton,
      (0, 1) => ClickType.RightMouseButton,
      (1, 0) => ClickType.ShiftLeftMouseButton,
      (1, 1) => ClickType.ShiftRightMouseButton,
      (2, >= 0 and <= 8) => ClickType.NumberKey,
      (2, 40) => ClickType.SwapOffhand,
      (3, _) => ClickType.MiddleMouseButton,
      (4, 0) => ClickType.Drop,
      (4, 1) => ClickType.DropStack,
      (6, _) => ClickType.DoubleClick,
      // Dragging spreads the cursor over slots instead of clicking one
      _ => null
    };
  }

  private async ValueTask HandleClickAsync(Menu menu, ClickContainerServerboundPacket clickPacket, CancellationToken cancellationToken)
  {
    if (GetClickType(clickPacket.Mode, clickPacket.Button) is not { } clickType)
      return;

    int? hotbarSlot = clickType is ClickType.NumberKey ? clickPacket.Button : null;
    await ClickSlotAsync(menu, clickPacket.Slot, clickType, hotbarSlot, cancellationToken);
  }

  // Main inventory and hotbar, plus the offhand slot since 1.9
  private int GetInventoryMenuSize(Menu inventoryMenu)
  {
    return inventoryMenu.Size + (playerContext.Player.ProtocolVersion >= ProtocolVersion.MINECRAFT_1_9 ? 37 : 36);
  }

  private static ItemStack? CreateItemStack(Menu menu, int slot)
  {
    if (!menu.ItemsMap.TryGetValue(slot, out var item))
      return null;

    var components = new List<IItemComponent>();

    if (item.Title is not null)
      components.Add(new CustomNameItemComponent { Value = item.Title });

    if (item.Lore is not null)
      components.Add(new LoreItemComponent { Value = item.Lore });

    if (item.Profile is not null)
      components.Add(new ResolvableProfileItemComponent { Kind = Kind.Partial, Profile = item.Profile });

    if (item.Damage is { } damage)
      components.Add(new DamageItemComponent { Value = damage });

    if (item.CustomData is not null)
      components.Add(new CustomDataItemComponent { Value = item.CustomData });

    return new ItemStack
    {
      Identifier = item.Identifier,
      Count = item.Count,
      Components = components
    };
  }

  [Subscribe]
  private async ValueTask OnPhaseChanged(PhaseChangedEvent @event, CancellationToken cancellationToken)
  {
    if (@event.Side is not Side.Server)
      return;

    // Joining a server starts with a fresh client inventory and no screens open
    inventoryInitialized = false;
    backendContainerOpen = false;
    await ClearCurrentMenuAsync(cancellationToken);
  }

  [Subscribe]
  private async ValueTask OnPlayerDisconnected(PlayerDisconnectedEvent @event, CancellationToken cancellationToken)
  {
    if (@event.Player != playerContext.Player)
      return;

    await ClearCurrentMenuAsync(cancellationToken);
  }

  [Subscribe]
  private async ValueTask OnMessageReceived(MessageReceivedEvent @event, CancellationToken cancellationToken)
  {
    switch (@event.Message)
    {
      case SetContainerContentClientboundPacket { ContainerId: PlayerInventoryContainerId }:
        inventoryInitialized = true;
        break;
      case OpenContainerClientboundPacket:
        backendContainerOpen = true;
        break;
      case CloseContainerServerboundPacket { ContainerId: not (PlayerInventoryContainerId or MenuContainerId) }:
      case CloseContainerClientboundPacket:
        backendContainerOpen = false;
        break;
    }

    if (ownedInventoryMenu is { } inventoryMenu && await HandleOwnedInventoryAsync(@event, inventoryMenu, cancellationToken))
      return;

    if (@event.Message is CloseContainerServerboundPacket { ContainerId: MenuContainerId })
    {
      // The backend never saw this container, and vanilla closes whatever is open regardless of the id
      @event.Cancel();
      await ClearCurrentMenuAsync(cancellationToken);
      return;
    }

    // Both replace whatever screen the client shows. Closes of other containers sent by the client
    // only reach the backend after the menu was opened over them.
    if (@event.Message is CloseContainerClientboundPacket or OpenContainerClientboundPacket)
      await ClearCurrentMenuAsync(cancellationToken);

    if (@event.Message is ClickContainerServerboundPacket containerServerboundPacket)
    {
      if (containerServerboundPacket.ContainerId == PlayerInventoryContainerId)
        Interlocked.Exchange(ref playerInventoryClickForwarded, 1);

      if (containerServerboundPacket.ContainerId != MenuContainerId)
        return;

      @event.Cancel();

      var menu = CurrentMenu;
      if (menu is null)
        return;

      // Dragging sends a packet per slot, the client only applies it once the drag ends
      if (containerServerboundPacket.Mode == 5 && containerServerboundPacket.Button is not (2 or 6 or 10))
        return;

      await ResyncAsync(menu, cancellationToken);
      await HandleClickAsync(menu, containerServerboundPacket, cancellationToken);
    }
  }

  // Keeps the backend and the player from changing the player inventory while the proxy owns it
  private async ValueTask<bool> HandleOwnedInventoryAsync(MessageReceivedEvent @event, Menu inventoryMenu, CancellationToken cancellationToken)
  {
    switch (@event.Message)
    {
      // While a backend container is open these update the real items shown below it, the menu is shown again once it closes
      case SetContainerSlotClientboundPacket { ContainerId: PlayerInventoryContainerId or PlayerInventoryDirectContainerId } when !backendContainerOpen:
      case SetPlayerInventorySlotClientboundPacket when !backendContainerOpen:
        @event.Cancel();
        return true;

      // Sent on join, respawn and world change, when the client may have reset its inventory
      case SetContainerContentClientboundPacket { ContainerId: PlayerInventoryContainerId } when !backendContainerOpen:
        @event.Cancel();
        await RestoreInventoryMenuAsync(inventoryMenu, cancellationToken);
        return true;

      // The client consumes or moves the used item itself, placing a block or throwing a snowball for example.
      // The backend may need the packet, so it is only observed.
      case UseItemOnServerboundPacket:
      case UseItemServerboundPacket:
        await RestoreInventoryMenuAsync(inventoryMenu, cancellationToken);
        return true;

      case ClickContainerServerboundPacket { ContainerId: PlayerInventoryContainerId } clickPacket:
        @event.Cancel();

        // Dragging sends a packet per slot, the client only applies it once the drag ends
        if (clickPacket.Mode == 5 && clickPacket.Button is not (2 or 6 or 10))
          return true;

        await RestoreInventoryMenuAsync(inventoryMenu, cancellationToken);
        await HandleClickAsync(inventoryMenu, clickPacket, cancellationToken);
        return true;

      // Picking moves real items into the hotbar, and the recipe book moves them into the crafting grid
      case PickItemServerboundPacket:
      case PickItemFromBlockServerboundPacket:
      case PickItemFromEntityServerboundPacket:
      case PlaceRecipeServerboundPacket { ContainerId: PlayerInventoryContainerId }:
      case SetCreativeModeSlotServerboundPacket:
      case PlayerActionServerboundPacket { Status: PlayerActionServerboundPacket.DropItemStackStatus or PlayerActionServerboundPacket.DropItemStatus or PlayerActionServerboundPacket.SwapItemInHandStatus }:
        @event.Cancel();
        await RestoreInventoryMenuAsync(inventoryMenu, cancellationToken);
        return true;

      // Backend containers show the real player inventory below them, so show the menu again once they close
      case CloseContainerServerboundPacket { ContainerId: not (PlayerInventoryContainerId or MenuContainerId) }:
        await RestoreInventoryMenuAsync(inventoryMenu, cancellationToken);
        return true;

      case CloseContainerClientboundPacket:
        await ClearCurrentMenuAsync(cancellationToken);
        await RestoreInventoryMenuAsync(inventoryMenu, cancellationToken);
        return true;

      default:
        return false;
    }
  }

  [Obsolete($"The default inventory menu is shown in menus but doesn't own the player inventory, use {nameof(SetInventoryMenuAsync)} per player instead.")]
  public static void SetDefaultInventoryMenu(Menu menu)
  {
    defaultInventoryMenu = menu;
  }
}
