using Menus.Api;
using Void.Proxy.Api.Events;
using Void.Proxy.Api.Players;

namespace Menus.Events;

public record MenuClickedEvent(MenuClick Click) : IScopedEvent
{
  public IPlayer Player => Click.Player;
}
