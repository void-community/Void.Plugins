using Menus.Api;
using Void.Proxy.Api.Events;
using Void.Proxy.Api.Players;

namespace Menus.Events;

/// <summary>
/// Raised when the menu is closed by the player or the proxy, replaced by another menu or a backend container,
/// or when the player switches server or disconnects. <see cref="Menu"/> includes changes made with
/// <c>MenuService.SetItemAsync</c>, so compare menus by name.
/// </summary>
public record MenuClosedEvent(IPlayer Player, Menu Menu) : IScopedEvent;
