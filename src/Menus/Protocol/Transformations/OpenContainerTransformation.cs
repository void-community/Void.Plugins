using Microsoft.Extensions.Logging;
using Void.Data.Api.Minecraft;
using Void.Minecraft.Components.Text.Serializers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Network.Registries.Transformations.Properties;

namespace Menus.Protocol.Transformations;

public class OpenContainerTransformation(ILogger<OpenContainerTransformation> logger)
{
  public void Passthrough_1_7_2_plus(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Passthrough 1.7.2+ {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<ByteProperty>(); // Container id

    var typeId = wrapper.Read<ByteProperty>();
    var title = wrapper.Read<StringProperty>();
    var slotCount = wrapper.Read<ByteProperty>();

    var typeIdentifier = MinecraftMenuRegistry.GetIdentifier(fromProtocolVersion, typeId.AsPrimitive, slotCount.AsPrimitive);
    var updatedTypeId = MinecraftMenuRegistry.GetId(toProtocolVersion, typeIdentifier);
    var updatedSlotCount = MinecraftMenuRegistry.GetSlotCount(toProtocolVersion, typeIdentifier);
    wrapper.Write(ByteProperty.FromPrimitive((byte) updatedTypeId));

    wrapper.Write(title);
    wrapper.Write(ByteProperty.FromPrimitive((byte) updatedSlotCount));
    wrapper.Passthrough<BoolProperty>();
  }

  public void DowngradeTo_1_7_6(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Downgrade to 1.7.6 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<ByteProperty>();

    var typeId = wrapper.Read<StringProperty>();
    var title = wrapper.Read<StringProperty>();
    var slotCount = wrapper.Read<ByteProperty>();

    var typeIdentifier = MinecraftMenuRegistry.GetIdentifier(fromProtocolVersion, typeId.AsPrimitive, slotCount.AsPrimitive);
    var updatedTypeId = MinecraftMenuRegistry.GetId(toProtocolVersion, typeIdentifier);
    var updatedSlotCount = MinecraftMenuRegistry.GetSlotCount(toProtocolVersion, typeIdentifier);
    wrapper.Write(ByteProperty.FromPrimitive((byte) updatedTypeId));

    var component = ComponentJsonSerializer.Deserialize(title.AsPrimitive);
    wrapper.Write(StringProperty.FromPrimitive(component.AsText));

    wrapper.Write(ByteProperty.FromPrimitive((byte) updatedSlotCount));
    wrapper.Write(BoolProperty.FromPrimitive(true));
  }

  public void Passthrough_1_8_plus(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Passthrough 1.8+ {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<ByteProperty>(); // Container id

    var typeId = wrapper.Read<StringProperty>();
    var title = wrapper.Read<StringProperty>();
    var slotCount = wrapper.Read<ByteProperty>();

    var typeIdentifier = MinecraftMenuRegistry.GetIdentifier(fromProtocolVersion, typeId.AsPrimitive, slotCount.AsPrimitive);
    var updatedTypeId = MinecraftMenuRegistry.GetLegacyId(toProtocolVersion, typeIdentifier);
    var updatedSlotCount = MinecraftMenuRegistry.GetSlotCount(toProtocolVersion, typeIdentifier);
    wrapper.Write(StringProperty.FromPrimitive(updatedTypeId));

    wrapper.Write(title);
    wrapper.Write(ByteProperty.FromPrimitive((byte) updatedSlotCount));
  }

  public void DowngradeTo_1_13_2(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Downgrade to 1.13.2 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    var containerId = wrapper.Read<VarIntProperty>();
    wrapper.Write(ByteProperty.FromPrimitive((byte) containerId.AsPrimitive));

    var typeId = wrapper.Read<VarIntProperty>();
    var typeIdentifier = MinecraftMenuRegistry.GetIdentifier(fromProtocolVersion, typeId.AsPrimitive);
    var updatedTypeId = MinecraftMenuRegistry.GetLegacyId(toProtocolVersion, typeIdentifier);
    var slotCount = MinecraftMenuRegistry.GetSlotCount(toProtocolVersion, typeIdentifier);
    wrapper.Write(StringProperty.FromPrimitive(updatedTypeId));

    wrapper.Passthrough<StringProperty>(); // Title
    wrapper.Write(ByteProperty.FromPrimitive((byte) slotCount));
  }

  public void Passthrough_1_14_plus(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Passthrough 1.14+ {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<VarIntProperty>(); // Container id

    var typeId = wrapper.Read<VarIntProperty>();
    var typeIdentifier = MinecraftMenuRegistry.GetIdentifier(fromProtocolVersion, typeId.AsPrimitive);
    var updatedTypeId = MinecraftMenuRegistry.GetId(toProtocolVersion, typeIdentifier);
    wrapper.Write(VarIntProperty.FromPrimitive(updatedTypeId));

    wrapper.Passthrough<StringProperty>(); // Title
  }

  public void DowngradeTo_1_20_2(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Downgrade to 1.20.2 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<VarIntProperty>(); // Container id

    var typeId = wrapper.Read<VarIntProperty>();
    var typeIdentifier = MinecraftMenuRegistry.GetIdentifier(fromProtocolVersion, typeId.AsPrimitive);
    var updatedTypeId = MinecraftMenuRegistry.GetId(toProtocolVersion, typeIdentifier);
    wrapper.Write(VarIntProperty.FromPrimitive(updatedTypeId));

    var title = wrapper.Read<NbtProperty>();
    var component = ComponentNbtSerializer.Deserialize(title.AsNbtTag);
    var updatedTitle = ComponentJsonSerializer.Serialize(component);
    wrapper.Write(StringProperty.FromPrimitive(updatedTitle.ToJsonString()));
  }

  public void Passthrough_1_20_3_plus(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Passthrough 1.20.3+ {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<VarIntProperty>(); // Container id

    var typeId = wrapper.Read<VarIntProperty>();
    var typeIdentifier = MinecraftMenuRegistry.GetIdentifier(fromProtocolVersion, typeId.AsPrimitive);
    var updatedTypeId = MinecraftMenuRegistry.GetId(toProtocolVersion, typeIdentifier);
    wrapper.Write(VarIntProperty.FromPrimitive(updatedTypeId));

    wrapper.Passthrough<NbtProperty>(); // Title
  }
}
