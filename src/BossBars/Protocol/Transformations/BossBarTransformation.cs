using Microsoft.Extensions.Logging;
using Void.Minecraft.Components.Text.Serializers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Network.Registries.Transformations.Properties;

namespace BossBars.Protocol.Transformations;

public class BossBarTransformation(ILogger<BossBarTransformation> logger)
{
  public void UpgradeTo_1_20_3(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace($"Upgrade to 1.20.3 {fromProtocolVersion} -> {toProtocolVersion}");

    wrapper.Passthrough<UuidProperty>(); // Boss bar id
    var action = wrapper.Passthrough<VarIntProperty>();

    // Add and update title actions start with the title
    if (action.AsPrimitive is 0x00 or 0x03)
    {
      var title = wrapper.Read<StringProperty>();
      var component = ComponentJsonSerializer.Deserialize(title.AsPrimitive);
      wrapper.Write(NbtProperty.FromNbtTag(ComponentNbtSerializer.Serialize(component)));
    }

    wrapper.Passthrough<BinaryProperty>(); // Remaining action fields
  }

  public void DowngradeTo_1_20_2(IMinecraftBinaryPacketWrapper wrapper,
    ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion)
  {
    logger.LogTrace($"Downgrade to 1.20.2 {fromProtocolVersion} -> {toProtocolVersion}");

    wrapper.Passthrough<UuidProperty>(); // Boss bar id
    var action = wrapper.Passthrough<VarIntProperty>();

    // Add and update title actions start with the title
    if (action.AsPrimitive is 0x00 or 0x03)
    {
      var title = wrapper.Read<NbtProperty>();
      var component = ComponentNbtSerializer.Deserialize(title.AsNbtTag);
      wrapper.Write(StringProperty.FromPrimitive(ComponentJsonSerializer.Serialize(component).ToJsonString()));
    }

    wrapper.Passthrough<BinaryProperty>(); // Remaining action fields
  }
}
