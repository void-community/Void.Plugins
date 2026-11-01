using Microsoft.Extensions.Logging;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Network.Registries.Transformations.Properties;

namespace Menus.Protocol.Transformations;

public class CloseContainerTransformation(ILogger<CloseContainerTransformation> logger)
{
  public void DowngradeTo_1_21(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Downgrade to 1.21 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    var containerId = wrapper.Read<VarIntProperty>();
    wrapper.Write(ByteProperty.FromPrimitive((byte) containerId.AsPrimitive));
  }

  public void UpgradeTo_1_21_2(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Upgrade to 1.21.2 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    var containerId = wrapper.Read<ByteProperty>();
    wrapper.Write(VarIntProperty.FromPrimitive(containerId.AsPrimitive));
  }
}
