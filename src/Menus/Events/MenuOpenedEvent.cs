using Menus.Api;
using Void.Proxy.Api.Events;
using Void.Proxy.Api.Players;

namespace Menus.Events;

public record MenuOpenedEvent(IPlayer Player, Menu Menu) : IScopedEvent;
