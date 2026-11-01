using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace Menus.Protocol.Packets.Serverbound;

// Only observed to cancel it, the original packet is forwarded as is
public class SwingArmServerboundPacket : IMinecraftServerboundPacket<SwingArmServerboundPacket>
{
  public const int MainHand = 0;

  // Until 26.3 replaced it with the punch packet
  public static readonly MinecraftPacketIdMapping[] Mappings = [
    new(0x0A, ProtocolVersion.MINECRAFT_1_7_2),
    new(0x1A, ProtocolVersion.MINECRAFT_1_9),
    new(0x1D, ProtocolVersion.MINECRAFT_1_12),
    new(0x27, ProtocolVersion.MINECRAFT_1_13),
    new(0x2A, ProtocolVersion.MINECRAFT_1_14),
    new(0x2B, ProtocolVersion.MINECRAFT_1_16),
    new(0x2C, ProtocolVersion.MINECRAFT_1_16_2),
    new(0x2E, ProtocolVersion.MINECRAFT_1_19),
    new(0x2F, ProtocolVersion.MINECRAFT_1_19_1),
    new(0x32, ProtocolVersion.MINECRAFT_1_20_2),
    new(0x33, ProtocolVersion.MINECRAFT_1_20_3),
    new(0x36, ProtocolVersion.MINECRAFT_1_20_5),

    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.MINECRAFT_26_2)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Serverbound, "minecraft:swing"), i))
  ];

  public int Hand { get; set; }

  public static SwingArmServerboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    // Upgrade transformations bring older layouts to the 26.2 one, the last with this packet
    return new SwingArmServerboundPacket
    {
      Hand = buffer.ReadVarInt()
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
