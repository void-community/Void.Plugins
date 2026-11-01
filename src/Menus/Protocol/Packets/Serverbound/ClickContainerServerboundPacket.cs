using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace Menus.Protocol.Packets.Serverbound;

public class ClickContainerServerboundPacket : IMinecraftServerboundPacket<ClickContainerServerboundPacket>
{
  public static readonly MinecraftPacketIdMapping[] Mappings = [
    new(0x0E, ProtocolVersion.MINECRAFT_1_7_2),
    new(0x07, ProtocolVersion.MINECRAFT_1_9),
    new(0x08, ProtocolVersion.MINECRAFT_1_12),
    new(0x07, ProtocolVersion.MINECRAFT_1_12_1),
    new(0x08, ProtocolVersion.MINECRAFT_1_13),
    new(0x09, ProtocolVersion.MINECRAFT_1_14),
    new(0x08, ProtocolVersion.MINECRAFT_1_17),
    new(0x0A, ProtocolVersion.MINECRAFT_1_19),
    new(0x0B, ProtocolVersion.MINECRAFT_1_19_1),
    new(0x0A, ProtocolVersion.MINECRAFT_1_19_3),
    new(0x0B, ProtocolVersion.MINECRAFT_1_19_4),
    new(0x0D, ProtocolVersion.MINECRAFT_1_20_2),
    new(0x0E, ProtocolVersion.MINECRAFT_1_20_5),

    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.Latest)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Serverbound, "minecraft:container_click"), i))
  ];

  public int ContainerId { get; set; }
  public int StateId { get; set; }
  public int Slot { get; set; }
  public int Button { get; set; }
  public int Mode { get; set; }
  public required byte[] SkippedBytes { get; set; }

  public static ClickContainerServerboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    return new ClickContainerServerboundPacket
    {
      ContainerId = buffer.ReadVarInt(),
      StateId = buffer.ReadVarInt(),
      Slot = buffer.ReadShort(),
      Button = buffer.ReadUnsignedByte(),
      Mode = buffer.ReadVarInt(),
      SkippedBytes = [..buffer.ReadToEnd()]
    };
  }

  public void Encode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    buffer.WriteVarInt(ContainerId);
    buffer.WriteVarInt(StateId);
    buffer.WriteUnsignedShort((ushort) Slot);
    buffer.WriteUnsignedByte((byte) Button);
    buffer.WriteVarInt(Mode);
    buffer.Write(SkippedBytes);
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
  }
}
