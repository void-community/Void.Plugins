using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Proxy.Api.Network;

namespace Menus.Protocol.Packets.Clientbound;

// Only observed to cancel it, the original packet is forwarded as is
public class SetPlayerInventorySlotClientboundPacket : IMinecraftClientboundPacket<SetPlayerInventorySlotClientboundPacket>
{
  // Added in 1.21.2
  public static readonly MinecraftPacketIdMapping[] Mappings = [
    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21_2, ProtocolVersion.Latest)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Clientbound, "minecraft:set_player_inventory"), i))
  ];

  public static SetPlayerInventorySlotClientboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    return new SetPlayerInventorySlotClientboundPacket();
  }

  public void Encode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    throw new NotSupportedException("Set player inventory slot packet is never sent by the proxy.");
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
  }
}
