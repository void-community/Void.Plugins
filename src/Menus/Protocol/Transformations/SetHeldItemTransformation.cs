using Microsoft.Extensions.Logging;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Network.Registries.Transformations.Properties;

namespace Menus.Protocol.Transformations;

public class SetHeldItemTransformation(ILogger<SetHeldItemTransformation> logger)
{
  public void UpgradeTo_1_21_4(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Upgrade to 1.21.4 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    var slot = wrapper.Read<ByteProperty>();
    wrapper.Write(VarIntProperty.FromPrimitive(slot.AsPrimitive));
  }
}
