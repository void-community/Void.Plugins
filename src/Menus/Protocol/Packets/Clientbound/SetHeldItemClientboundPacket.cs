using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace Menus.Protocol.Packets.Clientbound;

// Only observed, the original packet is forwarded as is
public class SetHeldItemClientboundPacket : IMinecraftClientboundPacket<SetHeldItemClientboundPacket>
{
  public static readonly MinecraftPacketIdMapping[] Mappings = [
    new(0x09, ProtocolVersion.MINECRAFT_1_7_2),
    new(0x37, ProtocolVersion.MINECRAFT_1_9),
    new(0x39, ProtocolVersion.MINECRAFT_1_12),
    new(0x3A, ProtocolVersion.MINECRAFT_1_12_1),
    new(0x3D, ProtocolVersion.MINECRAFT_1_13),
    new(0x3F, ProtocolVersion.MINECRAFT_1_14),
    new(0x40, ProtocolVersion.MINECRAFT_1_15),
    new(0x3F, ProtocolVersion.MINECRAFT_1_16),
    new(0x48, ProtocolVersion.MINECRAFT_1_17),
    new(0x47, ProtocolVersion.MINECRAFT_1_19),
    new(0x4A, ProtocolVersion.MINECRAFT_1_19_1),
    new(0x49, ProtocolVersion.MINECRAFT_1_19_3),
    new(0x4D, ProtocolVersion.MINECRAFT_1_19_4),
    new(0x4F, ProtocolVersion.MINECRAFT_1_20_2),
    new(0x51, ProtocolVersion.MINECRAFT_1_20_3),
    new(0x53, ProtocolVersion.MINECRAFT_1_20_5),

    new(MinecraftPacketRegistry.GetId(ProtocolVersion.MINECRAFT_1_21, Phase.Play, Direction.Clientbound, "minecraft:set_carried_item"), ProtocolVersion.MINECRAFT_1_21),

    // Renamed in 1.21.2
    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21_2, ProtocolVersion.Latest)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Clientbound, "minecraft:set_held_slot"), i))
  ];

  public int Slot { get; set; }

  public static SetHeldItemClientboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    // Upgrade transformations bring older layouts to the latest one
    return new SetHeldItemClientboundPacket
    {
      Slot = buffer.ReadVarInt()
    };
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
