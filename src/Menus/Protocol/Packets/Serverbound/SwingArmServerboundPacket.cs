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
  // Other animations of 1.7 clients don't swing a hand
  public const int NoHand = -1;
  private const int SwingAnimation = 1;

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
    // The off hand was added in 1.9
    if (protocolVersion >= ProtocolVersion.MINECRAFT_1_9)
      return new SwingArmServerboundPacket { Hand = buffer.ReadVarInt() };

    // 1.8 removed the entity id and the animation that 1.7 clients send
    if (protocolVersion >= ProtocolVersion.MINECRAFT_1_8)
      return new SwingArmServerboundPacket { Hand = MainHand };

    buffer.ReadInt();
    return new SwingArmServerboundPacket { Hand = buffer.ReadUnsignedByte() == SwingAnimation ? MainHand : NoHand };
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
