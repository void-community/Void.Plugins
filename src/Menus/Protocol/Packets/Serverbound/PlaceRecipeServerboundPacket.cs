using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace Menus.Protocol.Packets.Serverbound;

// Only observed to cancel it, the original packet is forwarded as is
public class PlaceRecipeServerboundPacket : IMinecraftServerboundPacket<PlaceRecipeServerboundPacket>
{
  // Added in 1.12.1
  public static readonly MinecraftPacketIdMapping[] Mappings = [
    new(0x12, ProtocolVersion.MINECRAFT_1_12_1),
    new(0x16, ProtocolVersion.MINECRAFT_1_13),
    new(0x18, ProtocolVersion.MINECRAFT_1_14),
    new(0x19, ProtocolVersion.MINECRAFT_1_16),
    new(0x18, ProtocolVersion.MINECRAFT_1_17),
    new(0x1A, ProtocolVersion.MINECRAFT_1_19),
    new(0x1B, ProtocolVersion.MINECRAFT_1_19_1),
    new(0x1A, ProtocolVersion.MINECRAFT_1_19_3),
    new(0x1B, ProtocolVersion.MINECRAFT_1_19_4),
    new(0x1E, ProtocolVersion.MINECRAFT_1_20_2),
    new(0x1F, ProtocolVersion.MINECRAFT_1_20_3),
    new(0x22, ProtocolVersion.MINECRAFT_1_20_5),

    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.Latest)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Serverbound, "minecraft:place_recipe"), i))
  ];

  public int ContainerId { get; set; }

  public static PlaceRecipeServerboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    // Upgrade transformations bring older layouts to the latest one
    return new PlaceRecipeServerboundPacket
    {
      ContainerId = buffer.ReadVarInt()
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
