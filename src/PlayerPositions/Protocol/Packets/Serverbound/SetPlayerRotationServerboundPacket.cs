using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace PlayerPositions.Protocol.Packets.Serverbound;

public class SetPlayerRotationServerboundPacket : IMinecraftServerboundPacket<SetPlayerRotationServerboundPacket>
{
  public static readonly MinecraftPacketIdMapping[] Mappings = [
    new(0x05, ProtocolVersion.MINECRAFT_1_7_2),
    new(0x0E, ProtocolVersion.MINECRAFT_1_9),
    new(0x10, ProtocolVersion.MINECRAFT_1_12),
    new(0x0F, ProtocolVersion.MINECRAFT_1_12_1),
    new(0x12, ProtocolVersion.MINECRAFT_1_13),
    new(0x13, ProtocolVersion.MINECRAFT_1_14),
    new(0x14, ProtocolVersion.MINECRAFT_1_16),
    new(0x13, ProtocolVersion.MINECRAFT_1_17),
    new(0x15, ProtocolVersion.MINECRAFT_1_19),
    new(0x16, ProtocolVersion.MINECRAFT_1_19_1),
    new(0x15, ProtocolVersion.MINECRAFT_1_19_3),
    new(0x16, ProtocolVersion.MINECRAFT_1_19_4),
    new(0x18, ProtocolVersion.MINECRAFT_1_20_2),
    new(0x19, ProtocolVersion.MINECRAFT_1_20_3),
    new(0x1C, ProtocolVersion.MINECRAFT_1_20_5),

    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.Latest)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Serverbound, "minecraft:move_player_rot"), i))
  ];

  public float Yaw { get; set; }
  public float Pitch { get; set; }
  public int Flags { get; set; }

  public static SetPlayerRotationServerboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    return new SetPlayerRotationServerboundPacket
    {
      Yaw = buffer.ReadFloat(),
      Pitch = buffer.ReadFloat(),
      Flags = buffer.ReadUnsignedByte()
    };
  }

  public void Encode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    buffer.WriteFloat(Yaw);
    buffer.WriteFloat(Pitch);
    buffer.WriteUnsignedByte((byte) Flags);
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
  }
}
