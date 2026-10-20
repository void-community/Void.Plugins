using BossBars.Protocol.Packets.Clientbound;
using BossBars.Protocol.Transformations;
using Microsoft.Extensions.Logging;
using Void.Minecraft.Events;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Players.Extensions;
using Void.Proxy.Api.Events;
using Void.Proxy.Api.Network.Channels;
using Void.Proxy.Api.Players.Contexts;

namespace BossBars.Services;

internal delegate void Transformer(IMinecraftBinaryPacketWrapper wrapper, ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion);

internal class TransformationService(
  ILogger<TransformationService> logger,
  IPlayerContext playerContext,
  BossBarTransformation bossBarTransformation
) : IEventListener
{
  public List<MinecraftPacketTransformationMapping> RepeatForRange(ProtocolVersion startProtocolVersion, ProtocolVersion endProtocolVersion, Transformer transformer)
  {
    var result = new List<MinecraftPacketTransformationMapping>();
    var range = ProtocolVersion.Range(startProtocolVersion, endProtocolVersion).ToArray();

    for (var i = 0; i < range.Length - 1; i++)
    {
      var from = range[i];
      var to = range[i + 1];

      if (from == to)
        continue;

      result.Insert(0, new MinecraftPacketTransformationMapping(from, to, wrapper => transformer(wrapper, from, to)));
    }

    return result;
  }

  [Subscribe]
  private void OnPhaseChanged(PhaseChangedEvent @event)
  {
    var handler = @event.Phase switch
    {
      Phase.Play => RegisterPlayTransformations,
      _ => null as Action<INetworkChannel>
    };

    handler?.Invoke(@event.Channel);
  }

  private void RegisterPlayTransformations(INetworkChannel channel)
  {
    playerContext.Player.RegisterTransformations<BossBarClientboundPacket>(channel, [
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_20_2, ProtocolVersion.MINECRAFT_1_20_3, bossBarTransformation.UpgradeTo_1_20_3),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_20_3, ProtocolVersion.MINECRAFT_1_20_2, bossBarTransformation.DowngradeTo_1_20_2)
    ]);

    logger.LogInformation($"Registered play transformations for {playerContext.Player}");
  }
}
