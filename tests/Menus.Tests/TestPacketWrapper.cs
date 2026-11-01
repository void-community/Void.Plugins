using System.Diagnostics.CodeAnalysis;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network.Registries.Transformations.Mappings;
using Void.Minecraft.Network.Registries.Transformations.Properties;

namespace Menus.Tests;

// Mirrors the proxy wrapper: properties are read from the packet until a transformation wrote some,
// and values written by one transformation are read by the next one
internal class TestPacketWrapper(byte[] bytes) : IMinecraftBinaryPacketWrapper
{
  private readonly MemoryStream stream = new(bytes);
  private readonly List<IPacketProperty> read = [];
  private readonly List<IPacketProperty> write = [];

  public TPropertyValue Read<TPropertyValue>() where TPropertyValue : IPacketProperty<TPropertyValue>
  {
    if (read.Count is 0)
    {
      var buffer = new MinecraftBuffer(stream);
      return TPropertyValue.Read(ref buffer);
    }

    var property = read[0];
    read.RemoveAt(0);
    return property.As<TPropertyValue>();
  }

  public void Write<TPropertyValue>(TPropertyValue value) where TPropertyValue : IPacketProperty<TPropertyValue>
  {
    write.Add(value);
  }

  public TPropertyValue Passthrough<TPropertyValue>() where TPropertyValue : IPacketProperty<TPropertyValue>
  {
    var property = Read<TPropertyValue>();
    write.Add(property);
    return property;
  }

  public void Reset()
  {
    read.InsertRange(0, write);
    write.Clear();
  }

  public void WriteProcessedValues(ref MinecraftBuffer buffer)
  {
    foreach (var property in read)
      property.Write(ref buffer);

    if (read.Count is 0)
      buffer.Write(stream);
  }

  public bool TryGet<TPropertyValue>(int index, [MaybeNullWhen(false)] out TPropertyValue value) where TPropertyValue : IPacketProperty<TPropertyValue> => throw new NotSupportedException();

  public TPropertyValue Get<TPropertyValue>(int index) where TPropertyValue : IPacketProperty<TPropertyValue> => throw new NotSupportedException();

  public bool TrySet<TPropertyValue>(int index, TPropertyValue value) where TPropertyValue : IPacketProperty<TPropertyValue> => throw new NotSupportedException();

  public void Set<TPropertyValue>(int index, TPropertyValue value) where TPropertyValue : IPacketProperty<TPropertyValue> => throw new NotSupportedException();
}
