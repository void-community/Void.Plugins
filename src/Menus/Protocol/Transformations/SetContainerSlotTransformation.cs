using Microsoft.Extensions.Logging;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Network.Registries.Transformations.Properties;

namespace Menus.Protocol.Transformations;

public class SetContainerSlotTransformation(ILogger<SetContainerSlotTransformation> logger, ItemStackTransformation itemStackTransformation)
{
  public void Passthrough_1_7_2_plus(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Passthrough 1.7.2+ {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<ByteProperty>(); // Container id
    wrapper.Passthrough<ShortProperty>(); // Slot id

    itemStackTransformation.Downgrade(wrapper, fromProtocolVersion, toProtocolVersion);
  }

  public void DowngradeTo_1_17(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Downgrade to 1.17 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<ByteProperty>(); // Container id
    wrapper.Read<VarIntProperty>(); // skip state id
    wrapper.Passthrough<ShortProperty>(); // Slot id

    itemStackTransformation.Downgrade(wrapper, fromProtocolVersion, toProtocolVersion);
  }

  public void Passthrough_1_17_1_plus(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Passthrough 1.17.1+ {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<ByteProperty>(); // Container id
    wrapper.Passthrough<VarIntProperty>(); // State id
    wrapper.Passthrough<ShortProperty>(); // Slot id

    itemStackTransformation.Downgrade(wrapper, fromProtocolVersion, toProtocolVersion);
  }

  public void DowngradeTo_1_20_3(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Downgrade to 1.20.3 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<ByteProperty>(); // Container id
    wrapper.Passthrough<VarIntProperty>(); // State id
    wrapper.Passthrough<ShortProperty>(); // Slot id

    itemStackTransformation.Downgrade(wrapper, fromProtocolVersion, toProtocolVersion);
  }

  public void Passthrough_1_20_5_plus(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Passthrough 1.20.5+ {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<ByteProperty>(); // Container id
    wrapper.Passthrough<VarIntProperty>(); // State id
    wrapper.Passthrough<ShortProperty>(); // Slot id

    itemStackTransformation.Downgrade(wrapper, fromProtocolVersion, toProtocolVersion);
  }

  public void DowngradeTo_1_21_2(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Downgrade to 1.21.2 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<VarIntProperty>(); // Container id
    wrapper.Passthrough<VarIntProperty>(); // State id
    wrapper.Passthrough<ShortProperty>(); // Slot id

    itemStackTransformation.Downgrade(wrapper, fromProtocolVersion, toProtocolVersion);
  }

  public void DowngradeTo_1_21(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Downgrade to 1.21 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    var containerId = wrapper.Read<VarIntProperty>();
    wrapper.Write(ByteProperty.FromPrimitive((byte)containerId.AsPrimitive));

    wrapper.Passthrough<VarIntProperty>(); // State id
    wrapper.Passthrough<ShortProperty>(); // Slot id

    itemStackTransformation.Downgrade(wrapper, fromProtocolVersion, toProtocolVersion);
  }

  public void Passthrough_1_21_4_plus(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Passthrough 1.21.4+ {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<VarIntProperty>(); // Container id
    wrapper.Passthrough<VarIntProperty>(); // State id
    wrapper.Passthrough<ShortProperty>(); // Slot id

    itemStackTransformation.Downgrade(wrapper, fromProtocolVersion, toProtocolVersion);
  }
}
