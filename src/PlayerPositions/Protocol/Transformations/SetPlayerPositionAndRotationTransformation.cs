using Microsoft.Extensions.Logging;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Network.Registries.Transformations.Properties;

namespace PlayerPositions.Protocol.Transformations;

public class SetPlayerPositionAndRotationTransformation(ILogger<SetPlayerPositionAndRotationTransformation> logger)
{
  public void UpgradeTo_1_8(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace($"Upgrade to 1.8 {fromProtocolVersion} -> {toProtocolVersion}");

    wrapper.Passthrough<DoubleProperty>(); // X
    wrapper.Passthrough<DoubleProperty>(); // Feet Y
    wrapper.Read<DoubleProperty>(); // Head Y
    wrapper.Passthrough<DoubleProperty>(); // Z
    wrapper.Passthrough<FloatProperty>(); // Yaw
    wrapper.Passthrough<FloatProperty>(); // Pitch
    wrapper.Passthrough<BoolProperty>(); // On ground
  }

  public void UpgradeTo_1_21_2(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace($"Upgrade to 1.21.2 {fromProtocolVersion} -> {toProtocolVersion}");

    wrapper.Passthrough<DoubleProperty>(); // X
    wrapper.Passthrough<DoubleProperty>(); // Y
    wrapper.Passthrough<DoubleProperty>(); // Z
    wrapper.Passthrough<FloatProperty>(); // Yaw
    wrapper.Passthrough<FloatProperty>(); // Pitch

    var onGround = wrapper.Read<BoolProperty>();
    wrapper.Write(ByteProperty.FromPrimitive((byte) (onGround.AsPrimitive ? 0x01 : 0x00))); // Flags
  }

  public void DowngradeTo_1_21(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace($"Downgrade to 1.21 {fromProtocolVersion} -> {toProtocolVersion}");

    wrapper.Passthrough<DoubleProperty>(); // X
    wrapper.Passthrough<DoubleProperty>(); // Y
    wrapper.Passthrough<DoubleProperty>(); // Z
    wrapper.Passthrough<FloatProperty>(); // Yaw
    wrapper.Passthrough<FloatProperty>(); // Pitch

    var flags = wrapper.Read<ByteProperty>();
    wrapper.Write(BoolProperty.FromPrimitive((flags.AsPrimitive & 0x01) != 0)); // On ground
  }

  public void DowngradeTo_1_7_6(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace($"Downgrade to 1.7.6 {fromProtocolVersion} -> {toProtocolVersion}");

    wrapper.Passthrough<DoubleProperty>(); // X

    var y = wrapper.Read<DoubleProperty>();
    wrapper.Write(y); // Feet Y
    wrapper.Write(DoubleProperty.FromPrimitive(y.AsPrimitive + 1.62)); // Head Y

    wrapper.Passthrough<DoubleProperty>(); // Z
    wrapper.Passthrough<FloatProperty>(); // Yaw
    wrapper.Passthrough<FloatProperty>(); // Pitch
    wrapper.Passthrough<BoolProperty>(); // On ground
  }
}
