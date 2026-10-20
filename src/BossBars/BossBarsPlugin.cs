using BossBars.Protocol.Transformations;
using BossBars.Services;
using Microsoft.Extensions.DependencyInjection;
using Void.Proxy.Api.Events;
using Void.Proxy.Api.Events.Plugins;
using Void.Proxy.Api.Plugins;
using Void.Proxy.Api.Plugins.Dependencies;

namespace BossBars;

public class BossBarsPlugin(IDependencyService dependencyService) : IApiPlugin
{
  public string Name => nameof(BossBarsPlugin);

  [Subscribe]
  private void OnPluginLoading(PluginLoadingEvent @event)
  {
    if (@event.Plugin != this)
      return;

    dependencyService.Register(services =>
    {
      services.AddSingleton<BossBarTransformation>();

      services.AddScoped<PacketService>();
      services.AddScoped<TransformationService>();
      services.AddScoped<BossBarService>();
    });
  }
}
