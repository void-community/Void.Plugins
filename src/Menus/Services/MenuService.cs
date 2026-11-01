using Menus.Api;
using Menus.Minecraft;
using Menus.Minecraft.Components.Item;
using Menus.Protocol.Packets.Clientbound;
using Menus.Protocol.Packets.Serverbound;
using Microsoft.Extensions.Logging;
using Void.Data.Api.Minecraft;
using Void.Minecraft.Links.Extensions;
using Void.Minecraft.Network;
using Void.Minecraft.Players.Extensions;
using Void.Proxy.Api.Commands;
using Void.Proxy.Api.Events;
using Void.Proxy.Api.Events.Network;
using Void.Proxy.Api.Links;
using Void.Proxy.Api.Players.Contexts;

namespace Menus.Services;

public class MenuService(ILogger<MenuService> logger, IPlayerContext playerContext, ICommandService commandService, ILinkService linkService) : IEventListener
{
  // Vanilla servers cycle container ids through 1..100, so 127 never collides with a backend container
  // and still fits into the signed byte used by older protocol versions.
  private const int MenuContainerId = 127;
  private const int PlayerInventoryContainerId = 0;
  private const int CursorContainerId = -1;
  private const int CursorSlot = -1;
  private const int OutsideSlot = -999;

  private static Menu? defaultInventoryMenu;

  public Menu? InventoryMenu = defaultInventoryMenu;
  public Menu? CurrentMenu;

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

    if (slot >= menu.Size)
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
  private async ValueTask OnMessageReceived(MessageReceivedEvent @event, CancellationToken cancellationToken)
  {
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

  public static void SetDefaultInventoryMenu(Menu menu)
  {
    defaultInventoryMenu = menu;
  }
}
