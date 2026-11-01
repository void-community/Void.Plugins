using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace Menus.Protocol.Packets.Serverbound;

// Only observed to cancel it, the original packet is forwarded as is
public class PlayerActionServerboundPacket : IMinecraftServerboundPacket<PlayerActionServerboundPacket>
{
  public const int DropItemStackStatus = 4;
  public const int DropItemStatus = 5;
  public const int SwapItemInHandStatus = 7;

  public static readonly MinecraftPacketIdMapping[] Mappings = [
    new(0x07, ProtocolVersion.MINECRAFT_1_7_2),
    new(0x13, ProtocolVersion.MINECRAFT_1_9),
    new(0x14, ProtocolVersion.MINECRAFT_1_12),
    new(0x18, ProtocolVersion.MINECRAFT_1_13),
    new(0x1A, ProtocolVersion.MINECRAFT_1_14),
    new(0x1B, ProtocolVersion.MINECRAFT_1_16),
    new(0x1A, ProtocolVersion.MINECRAFT_1_17),
    new(0x1C, ProtocolVersion.MINECRAFT_1_19),
    new(0x1D, ProtocolVersion.MINECRAFT_1_19_1),
    new(0x1C, ProtocolVersion.MINECRAFT_1_19_3),
    new(0x1D, ProtocolVersion.MINECRAFT_1_19_4),
    new(0x20, ProtocolVersion.MINECRAFT_1_20_2),
    new(0x21, ProtocolVersion.MINECRAFT_1_20_3),
    new(0x24, ProtocolVersion.MINECRAFT_1_20_5),

    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.Latest)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Serverbound, "minecraft:player_action"), i))
  ];

  public int Status { get; set; }

  public static PlayerActionServerboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    // Upgrade transformations bring older layouts to the latest one
    return new PlayerActionServerboundPacket
    {
      Status = buffer.ReadVarInt()
    };
  }

  public void Encode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    throw new NotSupportedException("Player action packet is never sent by the proxy.");
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
  }
}
