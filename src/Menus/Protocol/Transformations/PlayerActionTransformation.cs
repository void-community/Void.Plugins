using Microsoft.Extensions.Logging;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Network.Registries.Transformations.Properties;

namespace Menus.Protocol.Transformations;

public class PlayerActionTransformation(ILogger<PlayerActionTransformation> logger)
{
  // 26.3 inserted changing the destroy direction here and shifted the statuses after it
  private const int ChangeDestroyDirectionStatus = 1;

  public void UpgradeTo_1_9(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Upgrade to 1.9 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    var status = wrapper.Read<ByteProperty>();
    wrapper.Write(VarIntProperty.FromPrimitive(status.AsPrimitive));

    wrapper.Passthrough<BinaryProperty>(); // Position, face and sequence
  }

  public void UpgradeTo_26_3(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace("Upgrade to 26.3 {FromProtocolVersion} -> {ToProtocolVersion}", fromProtocolVersion, toProtocolVersion);

    var status = wrapper.Read<VarIntProperty>().AsPrimitive;
    wrapper.Write(VarIntProperty.FromPrimitive(status >= ChangeDestroyDirectionStatus ? status + 1 : status));

    wrapper.Passthrough<BinaryProperty>(); // Position, face and sequence
  }
}
