using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.PacketId.Mappings;
using Void.Minecraft.Profiles;
using Void.Proxy.Api.Network;

namespace TabLists.Protocol.Packets.Clientbound;

public class PlayerInfoRemoveClientboundPacket : IMinecraftClientboundPacket<PlayerInfoRemoveClientboundPacket>
{
  public static readonly MinecraftPacketIdMapping[] Mappings = [
    new(0x35, ProtocolVersion.MINECRAFT_1_19_3),
    new(0x39, ProtocolVersion.MINECRAFT_1_19_4),
    new(0x3B, ProtocolVersion.MINECRAFT_1_20_2),
    new(0x3D, ProtocolVersion.MINECRAFT_1_20_5),

    .. ProtocolVersion
      .Range(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.Latest)
      .Select(i => new MinecraftPacketIdMapping(
        MinecraftPacketRegistry.GetId(i, Phase.Play, Direction.Clientbound, "minecraft:player_info_remove"), i))
  ];

  public List<Uuid> PlayerUuids { get; set; } = new ();

  public static PlayerInfoRemoveClientboundPacket Decode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    var count = buffer.ReadVarInt();
    var uuids = new List<Uuid>(count);

    for (var i = 0; i < count; i++)
      uuids.Add(buffer.ReadUuid());

    return new PlayerInfoRemoveClientboundPacket() { PlayerUuids = uuids };
  }

  public void Encode(ref MinecraftBuffer buffer, ProtocolVersion protocolVersion)
  {
    buffer.WriteVarInt(PlayerUuids.Count);

    foreach (var uuid in PlayerUuids)
      buffer.WriteUuid(uuid);
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
  }
}
