using Menus.Extensions;
using Menus.Minecraft;
using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace Menus.Protocol.Packets.Clientbound;

public class SetContainerSlotClientboundPacket : IMinecraftClientboundPacket<SetContainerSlotClientboundPacket>
{
  public static readonly MinecraftPacketIdMapping[] Mappings = [
    new(0x2F, ProtocolVersion.MINECRAFT_1_7_2),
    new(0x16, ProtocolVersion.MINECRAFT_1_9),
    new(0x17, ProtocolVersion.MINECRAFT_1_13),
    new(0x16, ProtocolVersion.MINECRAFT_1_14),
    new(0x17, ProtocolVersion.MINECRAFT_1_15),
    new(0x16, ProtocolVersion.MINECRAFT_1_16),
    new(0x15, ProtocolVersion.MINECRAFT_1_16_2),
    new(0x16, ProtocolVersion.MINECRAFT_1_17),
    new(0x13, ProtocolVersion.MINECRAFT_1_19),
    new(0x12, ProtocolVersion.MINECRAFT_1_19_3),
    new(0x14, ProtocolVersion.MINECRAFT_1_19_4),
    new(0x15, ProtocolVersion.MINECRAFT_1_20_2),

    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.Latest)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Clientbound, "minecraft:container_set_slot"), i))
  ];

  public int ContainerId { get; set; }
  public int StateId { get; set; }
  public int Slot { get; set; }
  public ItemStack? ItemStack { get; set; }

  // Only the container id is decoded to tell backend inventory updates apart, the original packet is forwarded as is.
  // No upgrade transformations are registered, so the container id keeps the backend layout.
  public static SetContainerSlotClientboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    return new SetContainerSlotClientboundPacket
    {
      ContainerId = protocolVersion >= ProtocolVersion.MINECRAFT_1_21_2 ? buffer.ReadVarInt() : (sbyte) buffer.ReadUnsignedByte()
    };
  }

  public void Encode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    buffer.WriteVarInt(ContainerId);
    buffer.WriteVarInt(StateId);
    buffer.WriteUnsignedShort((ushort) Slot);
    buffer.WriteItemStack(ItemStack);
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
  }
}
