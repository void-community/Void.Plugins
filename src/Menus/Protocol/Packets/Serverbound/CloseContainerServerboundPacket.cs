using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace Menus.Protocol.Packets.Serverbound;

public class CloseContainerServerboundPacket : IMinecraftServerboundPacket<CloseContainerServerboundPacket>
{
  public static readonly MinecraftPacketIdMapping[] Mappings =
  [
    new(0x0D, ProtocolVersion.MINECRAFT_1_7_2),
    new(0x08, ProtocolVersion.MINECRAFT_1_9),
    new(0x09, ProtocolVersion.MINECRAFT_1_12),
    new(0x08, ProtocolVersion.MINECRAFT_1_12_1),
    new(0x09, ProtocolVersion.MINECRAFT_1_13),
    new(0x0A, ProtocolVersion.MINECRAFT_1_14),
    new(0x09, ProtocolVersion.MINECRAFT_1_17),
    new(0x0B, ProtocolVersion.MINECRAFT_1_19),
    new(0x0C, ProtocolVersion.MINECRAFT_1_19_1),
    new(0x0B, ProtocolVersion.MINECRAFT_1_19_3),
    new(0x0C, ProtocolVersion.MINECRAFT_1_19_4),
    new(0x0E, ProtocolVersion.MINECRAFT_1_20_2),
    new(0x0F, ProtocolVersion.MINECRAFT_1_20_5),

    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.Latest)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Serverbound, "minecraft:container_close"), i))
  ];

  public int ContainerId { get; set; }

  public static CloseContainerServerboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    return new CloseContainerServerboundPacket
    {
      ContainerId = buffer.ReadVarInt()
    };
  }

  public void Encode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    buffer.WriteVarInt(ContainerId);
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
  }
}
