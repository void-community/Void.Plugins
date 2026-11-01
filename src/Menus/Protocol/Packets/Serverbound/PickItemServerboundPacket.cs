using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace Menus.Protocol.Packets.Serverbound;

// Only observed to cancel it, the original packet is forwarded as is
public class PickItemServerboundPacket : IMinecraftServerboundPacket<PickItemServerboundPacket>
{
  // From 1.13 until 1.21.4 split it into picking from blocks and entities
  public static readonly MinecraftPacketIdMapping[] Mappings = [
    new(0x15, ProtocolVersion.MINECRAFT_1_13),
    new(0x17, ProtocolVersion.MINECRAFT_1_14),
    new(0x18, ProtocolVersion.MINECRAFT_1_16),
    new(0x17, ProtocolVersion.MINECRAFT_1_17),
    new(0x19, ProtocolVersion.MINECRAFT_1_19),
    new(0x1A, ProtocolVersion.MINECRAFT_1_19_1),
    new(0x19, ProtocolVersion.MINECRAFT_1_19_3),
    new(0x1A, ProtocolVersion.MINECRAFT_1_19_4),
    new(0x1C, ProtocolVersion.MINECRAFT_1_20_2),
    new(0x1D, ProtocolVersion.MINECRAFT_1_20_3),
    new(0x20, ProtocolVersion.MINECRAFT_1_20_5),

    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.MINECRAFT_1_21_2)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Serverbound, "minecraft:pick_item"), i))
  ];

  public static PickItemServerboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    return new PickItemServerboundPacket();
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
