using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Nbt;
using Void.Minecraft.Network;

namespace Menus.Minecraft.Components.Item;

public class CustomDataItemComponent : IItemComponent<CustomDataItemComponent>
{
  private static readonly Dictionary<ProtocolVersion, int> Mappings =
    new Dictionary<ProtocolVersion, int> { { ProtocolVersion.MINECRAFT_1_20_5, 0x00 } }
      .Concat(
        ProtocolVersion
          .Range(ProtocolVersion.MINECRAFT_1_21, ProtocolVersion.Latest)
          .Select(i => new KeyValuePair<ProtocolVersion, int>(i, MinecraftDataComponentTypeRegistry.GetId(i, "minecraft:custom_data")))
      )
      .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

  public static int GetId(ProtocolVersion protocolVersion) => Mappings[protocolVersion];

  public required NbtTag Value { get; set; }

  public static CustomDataItemComponent Read(ref MinecraftBuffer buffer)
  {
    return new CustomDataItemComponent
    {
      // Components use network NBT, which has no root tag name
      Value = buffer.ReadTag(readName: false)
    };
  }

  public void Write(ref MinecraftBuffer buffer)
  {
    buffer.WriteTag(Value, writeName: false);
  }
}
