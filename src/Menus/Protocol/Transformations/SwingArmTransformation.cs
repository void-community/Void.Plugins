using Microsoft.Extensions.Logging;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Network.Registries.Transformations.Properties;

namespace Menus.Protocol.Transformations;

public class SwingArmTransformation(ILogger<SwingArmTransformation> logger)
{
  public void UpgradeTo_1_8(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Upgrade to 1.8 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    // 1.7 clients only send the swing animation
    wrapper.Read<IntProperty>(); // Entity id
    wrapper.Read<ByteProperty>(); // Animation
  }

  public void UpgradeTo_1_9(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Upgrade to 1.9 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    // The off hand was added in 1.9
    wrapper.Write(VarIntProperty.FromPrimitive(0)); // Main hand
  }
}
