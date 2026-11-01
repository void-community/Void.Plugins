using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace Menus.Protocol.Packets.Serverbound;

// Only observed, the original packet is forwarded as is
public class UseItemOnServerboundPacket : IMinecraftServerboundPacket<UseItemOnServerboundPacket>
{
  // Also used to use items in the air before 1.9
  public static readonly MinecraftPacketIdMapping[] Mappings = [
    new(0x08, ProtocolVersion.MINECRAFT_1_7_2),
    new(0x1C, ProtocolVersion.MINECRAFT_1_9),
    new(0x1F, ProtocolVersion.MINECRAFT_1_12),
    new(0x29, ProtocolVersion.MINECRAFT_1_13),
    new(0x2C, ProtocolVersion.MINECRAFT_1_14),
    new(0x2D, ProtocolVersion.MINECRAFT_1_16),
    new(0x2E, ProtocolVersion.MINECRAFT_1_16_2),
    new(0x30, ProtocolVersion.MINECRAFT_1_19),
    new(0x31, ProtocolVersion.MINECRAFT_1_19_1),
    new(0x34, ProtocolVersion.MINECRAFT_1_20_2),
    new(0x35, ProtocolVersion.MINECRAFT_1_20_3),
    new(0x38, ProtocolVersion.MINECRAFT_1_20_5),

    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.Latest)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Serverbound, "minecraft:use_item_on"), i))
  ];

  public static UseItemOnServerboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    return new UseItemOnServerboundPacket();
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
