using Microsoft.Extensions.Logging;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Network.Registries.Transformations.Properties;

namespace Menus.Protocol.Transformations;

public class SetContainerPropertyTransformation(ILogger<SetContainerPropertyTransformation> logger)
{
  public void DowngradeTo_1_21(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    if (fromProtocolVersion < toProtocolVersion)
      return;

    logger.LogTrace("Downgrade to 1.21 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    var containerId = wrapper.Read<VarIntProperty>();
    wrapper.Write(ByteProperty.FromPrimitive((byte)containerId.AsPrimitive));

    wrapper.Passthrough<ShortProperty>(); // Property id
    wrapper.Passthrough<ShortProperty>(); // Value
  }
}
