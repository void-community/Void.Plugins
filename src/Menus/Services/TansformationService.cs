using Menus.Protocol.Packets.Clientbound;
using Menus.Protocol.Packets.Serverbound;
using Menus.Protocol.Transformations;
using Microsoft.Extensions.Logging;
using Void.Minecraft.Events;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Players.Extensions;
using Void.Proxy.Api.Events;
using Void.Proxy.Api.Network.Channels;
using Void.Proxy.Api.Players.Contexts;

namespace Menus.Services;

internal delegate void Transformer(IMinecraftBinaryPacketWrapper wrapper, ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion);

internal class TransformationService(
  ILogger<TransformationService> logger,
  IPlayerContext playerContext,
  SetContainerSlotTransformation setContainerSlotTransformation,
  SetContainerPropertyTransformation setContainerPropertyTransformation,
  OpenContainerTransformation openContainerTransformation,
  CloseContainerTransformation closeContainerTransformation,
  ClickContainerTransformation clickContainerTransformation
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
    playerContext.Player.RegisterTransformations<SetContainerSlotClientboundPacket>(channel, [
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_17, ProtocolVersion.MINECRAFT_1_7_2, setContainerSlotTransformation.Passthrough_1_7_2_plus),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_17_1, ProtocolVersion.MINECRAFT_1_17, setContainerSlotTransformation.DowngradeTo_1_17),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_20_3, ProtocolVersion.MINECRAFT_1_17_1, setContainerSlotTransformation.Passthrough_1_17_1_plus),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_20_5, ProtocolVersion.MINECRAFT_1_20_3, setContainerSlotTransformation.DowngradeTo_1_20_3),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.MINECRAFT_1_20_5, setContainerSlotTransformation.Passthrough_1_20_5_plus),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_21_2, ProtocolVersion.MINECRAFT_1_21, setContainerSlotTransformation.DowngradeTo_1_21),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_21_4, ProtocolVersion.MINECRAFT_1_21_2, setContainerSlotTransformation.DowngradeTo_1_21_2),
      ..RepeatForRange(ProtocolVersion.Latest, ProtocolVersion.MINECRAFT_1_21_4, setContainerSlotTransformation.Passthrough_1_21_4_plus),
    ]);

    playerContext.Player.RegisterTransformations<SetContainerPropertyClientboundPacket>(channel, [
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_21_2, ProtocolVersion.MINECRAFT_1_21, setContainerPropertyTransformation.DowngradeTo_1_21)
    ]);

    playerContext.Player.RegisterTransformations<OpenContainerClientboundPacket>(channel, [
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_7_6, ProtocolVersion.MINECRAFT_1_7_2, openContainerTransformation.Passthrough_1_7_2_plus),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_8, ProtocolVersion.MINECRAFT_1_7_6, openContainerTransformation.DowngradeTo_1_7_6),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_13_2, ProtocolVersion.MINECRAFT_1_8, openContainerTransformation.Passthrough_1_8_plus),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_14, ProtocolVersion.MINECRAFT_1_13_2, openContainerTransformation.DowngradeTo_1_13_2),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_20_2, ProtocolVersion.MINECRAFT_1_14, openContainerTransformation.Passthrough_1_14_plus),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_20_3, ProtocolVersion.MINECRAFT_1_20_2, openContainerTransformation.DowngradeTo_1_20_2),
      ..RepeatForRange(ProtocolVersion.Latest, ProtocolVersion.MINECRAFT_1_20_3, openContainerTransformation.Passthrough_1_20_3_plus)
    ]);

    playerContext.Player.RegisterTransformations<CloseContainerClientboundPacket>(channel, [
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_21_2, ProtocolVersion.MINECRAFT_1_21, closeContainerTransformation.DowngradeTo_1_21)
    ]);

    playerContext.Player.RegisterTransformations<ClickContainerServerboundPacket>(channel, [
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_8, ProtocolVersion.MINECRAFT_1_9, clickContainerTransformation.UpgradeTo_1_9),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_16_4, ProtocolVersion.MINECRAFT_1_17, clickContainerTransformation.UpgradeTo_1_17),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_17, ProtocolVersion.MINECRAFT_1_17_1, clickContainerTransformation.UpgradeTo_1_17_1),
      ..RepeatForRange(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.MINECRAFT_1_21_2, clickContainerTransformation.UpgradeTo_1_21_2)
    ]);

    logger.LogInformation($"Registered play transformations for {PlayerExtensions.get_Profile(playerContext.Player)?.Username ?? "unknown"}");
  }
}
