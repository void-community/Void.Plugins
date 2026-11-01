using Microsoft.Extensions.Logging;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Network.Registries.Transformations.Properties;

namespace Menus.Protocol.Transformations;

public class PlaceRecipeTransformation(ILogger<PlaceRecipeTransformation> logger)
{
  public void UpgradeTo_1_21_2(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Upgrade to 1.21.2 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    var containerId = wrapper.Read<ByteProperty>();
    wrapper.Write(VarIntProperty.FromPrimitive((sbyte)containerId.AsPrimitive));

    wrapper.Passthrough<BinaryProperty>(); // Recipe and make all
  }
}
