using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace PlayerPositions.Protocol.Packets.Serverbound;

public class SetPlayerPositionServerboundPacket : IMinecraftServerboundPacket<SetPlayerPositionServerboundPacket>
{
  public static readonly MinecraftPacketIdMapping[] Mappings = [
    new(0x04, ProtocolVersion.MINECRAFT_1_7_2),
    new(0x0C, ProtocolVersion.MINECRAFT_1_9),
    new(0x0E, ProtocolVersion.MINECRAFT_1_12),
    new(0x0D, ProtocolVersion.MINECRAFT_1_12_1),
    new(0x10, ProtocolVersion.MINECRAFT_1_13),
    new(0x11, ProtocolVersion.MINECRAFT_1_14),
    new(0x12, ProtocolVersion.MINECRAFT_1_16),
    new(0x11, ProtocolVersion.MINECRAFT_1_17),
    new(0x13, ProtocolVersion.MINECRAFT_1_19),
    new(0x14, ProtocolVersion.MINECRAFT_1_19_1),
    new(0x13, ProtocolVersion.MINECRAFT_1_19_3),
    new(0x14, ProtocolVersion.MINECRAFT_1_19_4),
    new(0x16, ProtocolVersion.MINECRAFT_1_20_2),
    new(0x17, ProtocolVersion.MINECRAFT_1_20_3),
    new(0x1A, ProtocolVersion.MINECRAFT_1_20_5),

    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.Latest)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Serverbound, "minecraft:move_player_pos"), i))
  ];

  public double X { get; set; }
  public double Y { get; set; }
  public double Z { get; set; }
  public int Flags { get; set; }

  public static SetPlayerPositionServerboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    return new SetPlayerPositionServerboundPacket
    {
      X = buffer.ReadDouble(),
      Y = buffer.ReadDouble(),
      Z = buffer.ReadDouble(),
      Flags = buffer.ReadUnsignedByte()
    };
  }

  public void Encode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    buffer.WriteDouble(X);
    buffer.WriteDouble(Y);
    buffer.WriteDouble(Z);
    buffer.WriteUnsignedByte((byte) Flags);
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
  }
}
