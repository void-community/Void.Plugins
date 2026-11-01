using Microsoft.Extensions.Logging;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Network.Registries.Transformations.Properties;

namespace Menus.Protocol.Transformations;

public class SetContainerContentTransformation(ILogger<SetContainerContentTransformation> logger, ItemStackTransformation itemStackTransformation)
{
  public void Passthrough_1_7_2_plus(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Passthrough 1.7.2+ {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<ByteProperty>(); // Container id
    var count = wrapper.Passthrough<ShortProperty>(); // Count

    for (var i = 0; i < count.AsPrimitive; i++)
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

    var count = wrapper.Read<VarIntProperty>();
    wrapper.Write(ShortProperty.FromPrimitive((short)count.AsPrimitive));

    for (var i = 0; i < count.AsPrimitive; i++)
      itemStackTransformation.Downgrade(wrapper, fromProtocolVersion, toProtocolVersion);

    // skip carried item, 1.17 has no such field
    var isPresent = wrapper.Read<BoolProperty>();
    if (!isPresent.AsPrimitive)
      return;

    wrapper.Read<VarIntProperty>(); // Item id
    wrapper.Read<ByteProperty>(); // Count
    wrapper.Read<NamedNbtProperty>(); // Nbt
  }

  public void Passthrough_1_17_1_plus(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Passthrough 1.17.1+ {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<ByteProperty>(); // Container id
    PassthroughStateIdAndItems(wrapper, fromProtocolVersion, toProtocolVersion);
  }

  public void DowngradeTo_1_20_3(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Downgrade to 1.20.3 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<ByteProperty>(); // Container id
    PassthroughStateIdAndItems(wrapper, fromProtocolVersion, toProtocolVersion);
  }

  public void Passthrough_1_20_5_plus(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Passthrough 1.20.5+ {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<ByteProperty>(); // Container id
    PassthroughStateIdAndItems(wrapper, fromProtocolVersion, toProtocolVersion);
  }

  public void DowngradeTo_1_21_2(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Downgrade to 1.21.2 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<VarIntProperty>(); // Container id
    PassthroughStateIdAndItems(wrapper, fromProtocolVersion, toProtocolVersion);
  }

  public void DowngradeTo_1_21(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Downgrade to 1.21 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    var containerId = wrapper.Read<VarIntProperty>();
    wrapper.Write(ByteProperty.FromPrimitive((byte)containerId.AsPrimitive));

    PassthroughStateIdAndItems(wrapper, fromProtocolVersion, toProtocolVersion);
  }

  public void Passthrough_1_21_4_plus(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Passthrough 1.21.4+ {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<VarIntProperty>(); // Container id
    PassthroughStateIdAndItems(wrapper, fromProtocolVersion, toProtocolVersion);
  }

  private void PassthroughStateIdAndItems(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    wrapper.Passthrough<VarIntProperty>(); // State id
    var count = wrapper.Passthrough<VarIntProperty>(); // Count

    for (var i = 0; i < count.AsPrimitive; i++)
      itemStackTransformation.Downgrade(wrapper, fromProtocolVersion, toProtocolVersion);

    itemStackTransformation.Downgrade(wrapper, fromProtocolVersion, toProtocolVersion); // Carried item
  }
}
