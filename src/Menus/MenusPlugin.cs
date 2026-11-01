using Menus.Protocol.Transformations;
using Menus.Services;
using Microsoft.Extensions.DependencyInjection;
using Void.Proxy.Api.Events;
using Void.Proxy.Api.Events.Proxy;
using Void.Proxy.Api.Plugins;
using Void.Proxy.Api.Plugins.Dependencies;

namespace Menus;

public class MenusPlugin(IDependencyService dependencyService) : IApiPlugin
{
  public string Name => nameof(MenusPlugin);

  [Subscribe]
  private void OnProxyStarting(ProxyStartingEvent @event)
  {
    dependencyService.Register(services =>
    {
      services.AddSingleton<ItemStackTransformation>();
      services.AddSingleton<SetContainerSlotTransformation>();
      services.AddSingleton<SetContainerContentTransformation>();
      services.AddSingleton<SetContainerPropertyTransformation>();
      services.AddSingleton<OpenContainerTransformation>();
      services.AddSingleton<CloseContainerTransformation>();
      services.AddSingleton<ClickContainerTransformation>();

      services.AddScoped<PacketService>();
      services.AddScoped<TransformationService>();
      services.AddScoped<MenuService>();
    });
  }
}
