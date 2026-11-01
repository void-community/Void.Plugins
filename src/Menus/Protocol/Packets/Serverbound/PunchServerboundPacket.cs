using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace Menus.Protocol.Packets.Serverbound;

// Only observed to cancel it, the original packet is forwarded as is
public class PunchServerboundPacket : IMinecraftServerboundPacket<PunchServerboundPacket>
{
  // Replaced the swing packet in 26.3, sent with no payload whenever the player left clicks
  public static readonly MinecraftPacketIdMapping[] Mappings = [
    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_26_3, ProtocolVersion.Latest)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Serverbound, "minecraft:punch"), i))
  ];

  public static PunchServerboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    return new PunchServerboundPacket();
  }

  public void Encode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    throw new NotSupportedException("This packet is never sent by the proxy.");
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
  }
}
