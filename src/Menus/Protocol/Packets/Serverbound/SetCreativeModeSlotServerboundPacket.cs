using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace Menus.Protocol.Packets.Serverbound;

// Only observed to cancel it, the original packet is forwarded as is
public class SetCreativeModeSlotServerboundPacket : IMinecraftServerboundPacket<SetCreativeModeSlotServerboundPacket>
{
  public static readonly MinecraftPacketIdMapping[] Mappings = [
    new(0x10, ProtocolVersion.MINECRAFT_1_7_2),
    new(0x18, ProtocolVersion.MINECRAFT_1_9),
    new(0x1B, ProtocolVersion.MINECRAFT_1_12),
    new(0x24, ProtocolVersion.MINECRAFT_1_13),
    new(0x26, ProtocolVersion.MINECRAFT_1_14),
    new(0x27, ProtocolVersion.MINECRAFT_1_16),
    new(0x28, ProtocolVersion.MINECRAFT_1_16_2),
    new(0x2A, ProtocolVersion.MINECRAFT_1_19),
    new(0x2B, ProtocolVersion.MINECRAFT_1_19_1),
    new(0x2E, ProtocolVersion.MINECRAFT_1_20_2),
    new(0x2F, ProtocolVersion.MINECRAFT_1_20_3),
    new(0x32, ProtocolVersion.MINECRAFT_1_20_5),

    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.Latest)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Serverbound, "minecraft:set_creative_mode_slot"), i))
  ];

  public static SetCreativeModeSlotServerboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    return new SetCreativeModeSlotServerboundPacket();
  }

  public void Encode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    throw new NotSupportedException("Set creative mode slot packet is never sent by the proxy.");
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
  }
}
