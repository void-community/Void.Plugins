using Microsoft.Extensions.Logging;
using PlayerPositions.Protocol.Packets.Serverbound;
using PlayerPositions.Protocol.Transformations;
using Void.Minecraft.Events;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Players.Extensions;
using Void.Proxy.Api.Events;
using Void.Proxy.Api.Network.Channels;
using Void.Proxy.Api.Players.Contexts;

namespace PlayerPositions.Services;

internal delegate void Transformer(IMinecraftBinaryPacketWrapper wrapper, ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion);

internal class TransformationService(
  ILogger<TransformationService> logger,
  IPlayerContext playerContext,
  SetPlayerPositionTransformation setPlayerPositionTransformation,
  SetPlayerRotationTransformation setPlayerRotationTransformation,
  SetPlayerPositionAndRotationTransformation setPlayerPositionAndRotationTransformation
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
    playerContext.Player.RegisterTransformations<SetPlayerPositionServerboundPacket>(channel, [
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_7_6, ProtocolVersion.MINECRAFT_1_8, setPlayerPositionTransformation.UpgradeTo_1_8),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.MINECRAFT_1_21_2, setPlayerPositionTransformation.UpgradeTo_1_21_2),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_21_2, ProtocolVersion.MINECRAFT_1_21, setPlayerPositionTransformation.DowngradeTo_1_21),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_8, ProtocolVersion.MINECRAFT_1_7_6, setPlayerPositionTransformation.DowngradeTo_1_7_6)
    ]);

    playerContext.Player.RegisterTransformations<SetPlayerRotationServerboundPacket>(channel, [
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.MINECRAFT_1_21_2, setPlayerRotationTransformation.UpgradeTo_1_21_2),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_21_2, ProtocolVersion.MINECRAFT_1_21, setPlayerRotationTransformation.DowngradeTo_1_21)
    ]);

    playerContext.Player.RegisterTransformations<SetPlayerPositionAndRotationServerboundPacket>(channel, [
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_7_6, ProtocolVersion.MINECRAFT_1_8, setPlayerPositionAndRotationTransformation.UpgradeTo_1_8),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.MINECRAFT_1_21_2, setPlayerPositionAndRotationTransformation.UpgradeTo_1_21_2),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_21_2, ProtocolVersion.MINECRAFT_1_21, setPlayerPositionAndRotationTransformation.DowngradeTo_1_21),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_8, ProtocolVersion.MINECRAFT_1_7_6, setPlayerPositionAndRotationTransformation.DowngradeTo_1_7_6)
    ]);

    logger.LogInformation($"Registered play transformations for {PlayerExtensions.get_Profile(playerContext.Player)?.Username ?? "unknown"}");
  }
}
