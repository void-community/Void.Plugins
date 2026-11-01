using Menus.Api;
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
using Void.Proxy.Api.Links;
using Void.Proxy.Api.Network;
using Void.Proxy.Api.Players.Contexts;

namespace Menus.Services;

public class MenuService(ILogger<MenuService> logger, IPlayerContext playerContext, ICommandService commandService, ILinkService linkService) : IEventListener
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

  private static Menu? defaultInventoryMenu;

  private Menu? ownedInventoryMenu;

  // The backend sent the player inventory since joining, so the client is ready for inventory packets
  private bool inventoryInitialized;

  // Backend containers show the real player inventory below them
  private bool backendContainerOpen;

  public Menu? InventoryMenu => ownedInventoryMenu ?? defaultInventoryMenu;
  public Menu? CurrentMenu;

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

    if (!isPlayerInventory)
    {
      CurrentMenu = menu;

      // Opening a screen replaces whatever the backend had open, so close it on the backend too.
      // This keeps the backend on the player inventory, which ResyncAsync relies on.
      if (linkService.TryGetLink(player, out var link))
        await link.SendPacketAsync(new CloseContainerServerboundPacket { ContainerId = PlayerInventoryContainerId }, cancellationToken);

      backendContainerOpen = false;

      logger.LogTrace($"Opening menu {menu.Name} for {PlayerExtensions.get_Profile(playerContext.Player)?.Username ?? "unknown"}");
      var openContainerPacket = new OpenContainerClientboundPacket
      {
        ContainerId = MenuContainerId,
        Type = MinecraftMenuRegistry.GetId(ProtocolVersion.Latest, menu.Type),
        Title = menu.Title
      };
      await player.SendPacketAsync(openContainerPacket, cancellationToken);
    }

    await UpdateSlotsAsync(menu, cancellationToken);
  }

  public async ValueTask CloseAsync(CancellationToken cancellationToken)
  {
    if (CurrentMenu is null)
      return;

    CurrentMenu = null;

    var closeContainerPacket = new CloseContainerClientboundPacket
    {
      ContainerId = MenuContainerId
    };

    await playerContext.Player.SendPacketAsync(closeContainerPacket, cancellationToken);
  }

  public async ValueTask SetProperty(int property, int value, CancellationToken cancellationToken)
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

    logger.LogTrace(
      $"Setting slot {slot} in container {(isPlayerInventory ? PlayerInventoryContainerId : MenuContainerId)} to {itemStack?.Identifier ?? "air"} for {PlayerExtensions.get_Profile(playerContext.Player)?.Username ?? "unknown"}");
    var setSlotPacket = new SetContainerSlotClientboundPacket
    {
      ContainerId = isPlayerInventory ? PlayerInventoryContainerId : MenuContainerId,
      Slot = slot,
      ItemStack = itemStack
    };

    await playerContext.Player.SendPacketAsync(setSlotPacket, cancellationToken);
  }

  public async ValueTask ClickSlotAsync(Menu menu, int slot, ClickType clickType, CancellationToken cancellationToken)
  {
    MenuItem? item = null;
    var isPlayerInventory = menu.Type == "minecraft:inventory";

    // Slots past a container menu belong to the player inventory below it
    if (!isPlayerInventory && slot >= menu.Size)
    {
      if (InventoryMenu is not null)
      {
        var inventorySlot = slot - menu.Size + InventoryMenu.Size;
        item = InventoryMenu.ItemsMap.GetValueOrDefault(inventorySlot);
      }
    }
    else
      item = menu.ItemsMap.GetValueOrDefault(slot);

    if (item is null)
      return;

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
    logger.LogTrace(
      $"Setting {size} slots in container {containerId} from menu {menu.Name} for {PlayerExtensions.get_Profile(playerContext.Player)?.Username ?? "unknown"}");
    var setContentPacket = new SetContainerContentClientboundPacket
    {
      ContainerId = containerId,
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

    if (!linkService.TryGetLink(player, out var link))
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

    return new ItemStack
    {
      Identifier = item.Identifier,
      Count = item.Count,
      Components = components
    };
  }

  [Subscribe]
  private void OnPhaseChanged(PhaseChangedEvent @event)
  {
    if (@event.Side is not Side.Server)
      return;

    // Joining a server starts with a fresh client inventory and no containers open
    inventoryInitialized = false;
    backendContainerOpen = false;
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
      CurrentMenu = null;
      return;
    }

    if (@event.Message is CloseContainerClientboundPacket or CloseContainerServerboundPacket
        or OpenContainerClientboundPacket)
    {
      CurrentMenu = null;
    }

    if (@event.Message is ClickContainerServerboundPacket containerServerboundPacket)
    {
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

      if (containerServerboundPacket.Mode > 0)
        return;

      await ClickSlotAsync(menu, containerServerboundPacket.Slot, (ClickType) containerServerboundPacket.Button, cancellationToken);
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

        if (clickPacket.Mode == 0)
          await ClickSlotAsync(inventoryMenu, clickPacket.Slot, (ClickType) clickPacket.Button, cancellationToken);

        return true;

      case SetCreativeModeSlotServerboundPacket:
      case PlayerActionServerboundPacket { Status: PlayerActionServerboundPacket.DropItemStackStatus or PlayerActionServerboundPacket.DropItemStatus or PlayerActionServerboundPacket.SwapItemInHandStatus }:
        @event.Cancel();
        await RestoreInventoryMenuAsync(inventoryMenu, cancellationToken);
        return true;

      // Backend containers show the real player inventory below them, so show the menu again once they close
      case CloseContainerServerboundPacket { ContainerId: not (PlayerInventoryContainerId or MenuContainerId) }:
      case CloseContainerClientboundPacket:
        CurrentMenu = null;
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
