using Menus.Extensions;
using Menus.Minecraft;
using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace Menus.Protocol.Packets.Clientbound;

public class SetContainerContentClientboundPacket : IMinecraftClientboundPacket<SetContainerContentClientboundPacket>
{
  public static readonly MinecraftPacketIdMapping[] Mappings = [
    new(0x30, ProtocolVersion.MINECRAFT_1_7_2),
    new(0x14, ProtocolVersion.MINECRAFT_1_9),
    new(0x15, ProtocolVersion.MINECRAFT_1_13),
    new(0x14, ProtocolVersion.MINECRAFT_1_14),
    new(0x15, ProtocolVersion.MINECRAFT_1_15),
    new(0x14, ProtocolVersion.MINECRAFT_1_16),
    new(0x13, ProtocolVersion.MINECRAFT_1_16_2),
    new(0x14, ProtocolVersion.MINECRAFT_1_17),
    new(0x11, ProtocolVersion.MINECRAFT_1_19),
    new(0x10, ProtocolVersion.MINECRAFT_1_19_3),
    new(0x12, ProtocolVersion.MINECRAFT_1_19_4),
    new(0x13, ProtocolVersion.MINECRAFT_1_20_2),

    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.Latest)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Clientbound, "minecraft:container_set_content"), i))
  ];

  public int ContainerId { get; set; }
  public int StateId { get; set; }
  public List<ItemStack?> ItemStacks { get; set; } = [];
  public ItemStack? CarriedItemStack { get; set; }

  // Only the container id is decoded to tell backend inventory updates apart, the original packet is forwarded as is.
  // No upgrade transformations are registered, so the container id keeps the backend layout.
  public static SetContainerContentClientboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    // Upgrade transformations bring older layouts to the latest one
    return new SetContainerContentClientboundPacket
    {
      ContainerId = buffer.ReadVarInt()
    };
  }

  public void Encode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    buffer.WriteVarInt(ContainerId);
    buffer.WriteVarInt(StateId);
    buffer.WriteVarInt(ItemStacks.Count);

    foreach (var itemStack in ItemStacks)
      buffer.WriteItemStack(itemStack);

    buffer.WriteItemStack(CarriedItemStack);
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
  }
}
