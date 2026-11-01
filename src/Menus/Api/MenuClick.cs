using Void.Proxy.Api.Players;

namespace Menus.Api;

/// <param name="HotbarSlot">The pressed hotbar key from 0 to 8 for <see cref="ClickType.NumberKey"/> clicks.</param>
public record MenuClick(IPlayer Player, Menu Menu, int Slot, MenuItem Item, ClickType Type, int? HotbarSlot = null);
