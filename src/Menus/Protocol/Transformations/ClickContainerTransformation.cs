using Microsoft.Extensions.Logging;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Network.Registries.Transformations.Properties;

namespace Menus.Protocol.Transformations;

public class ClickContainerTransformation(ILogger<ClickContainerTransformation> logger)
{
  public void UpgradeTo_1_9(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Upgrade to 1.9 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<ByteProperty>(); // Container id
    wrapper.Passthrough<ShortProperty>(); // Slot
    wrapper.Passthrough<ByteProperty>(); // Button
    wrapper.Passthrough<ShortProperty>(); // Action number

    var mode = wrapper.Read<ByteProperty>();
    wrapper.Write(VarIntProperty.FromPrimitive(mode.AsPrimitive)); // Mode

    wrapper.Passthrough<BinaryProperty>(); // Skipped bytes
  }

  public void UpgradeTo_1_17(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Upgrade to 1.17 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<ByteProperty>(); // Container id
    wrapper.Passthrough<ShortProperty>(); // Slot
    wrapper.Passthrough<ByteProperty>(); // Button
    wrapper.Read<ShortProperty>(); // Action number
    wrapper.Passthrough<VarIntProperty>(); // Mode
    wrapper.Passthrough<BinaryProperty>(); // Skipped bytes
  }

  public void UpgradeTo_1_17_1(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Upgrade to 1.17.1 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    wrapper.Passthrough<ByteProperty>(); // Container id
    wrapper.Write(VarIntProperty.FromPrimitive(0)); // State id
    wrapper.Passthrough<ShortProperty>(); // Slot
    wrapper.Passthrough<ByteProperty>(); // Button
    wrapper.Passthrough<VarIntProperty>(); // Mode
    wrapper.Passthrough<BinaryProperty>(); // Skipped bytes
  }

  public void UpgradeTo_1_21_2(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Upgrade to 1.21.2 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    var containerId = wrapper.Read<ByteProperty>();
    wrapper.Write(VarIntProperty.FromPrimitive(containerId.AsPrimitive));

    wrapper.Passthrough<VarIntProperty>(); // State id
    wrapper.Passthrough<ShortProperty>(); // Slot
    wrapper.Passthrough<ByteProperty>(); // Button
    wrapper.Passthrough<VarIntProperty>(); // Mode
    wrapper.Passthrough<BinaryProperty>(); // Skipped bytes
  }
}
