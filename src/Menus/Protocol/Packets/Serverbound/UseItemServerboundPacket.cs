using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace Menus.Protocol.Packets.Serverbound;

// Only observed, the original packet is forwarded as is
public class UseItemServerboundPacket : IMinecraftServerboundPacket<UseItemServerboundPacket>
{
  // Added in 1.9
  public static readonly MinecraftPacketIdMapping[] Mappings = [
    new(0x1D, ProtocolVersion.MINECRAFT_1_9),
    new(0x20, ProtocolVersion.MINECRAFT_1_12),
    new(0x2A, ProtocolVersion.MINECRAFT_1_13),
    new(0x2D, ProtocolVersion.MINECRAFT_1_14),
    new(0x2E, ProtocolVersion.MINECRAFT_1_16),
    new(0x2F, ProtocolVersion.MINECRAFT_1_16_2),
    new(0x31, ProtocolVersion.MINECRAFT_1_19),
    new(0x32, ProtocolVersion.MINECRAFT_1_19_1),
    new(0x35, ProtocolVersion.MINECRAFT_1_20_2),
    new(0x36, ProtocolVersion.MINECRAFT_1_20_3),
    new(0x39, ProtocolVersion.MINECRAFT_1_20_5),

    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.Latest)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Serverbound, "minecraft:use_item"), i))
  ];

  public static UseItemServerboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    return new UseItemServerboundPacket();
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
