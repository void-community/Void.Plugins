using System.Collections.Concurrent;
using System.Reflection;
using Menus.Minecraft;
using Menus.Minecraft.Components.Item;
using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;

namespace Menus.Extensions;

public static class MinecraftBufferExtensions
{
  private static readonly ConcurrentDictionary<Type, Func<ProtocolVersion, int>> ComponentIdGetters = new();

  public static void WriteItemStack(this ref MinecraftBuffer buffer, ItemStack? itemStack)
  {
    if (itemStack == null || itemStack.Count == 0)
    {
      buffer.WriteVarInt(0);
      return;
    }

    var itemId = MinecraftItemRegistry.GetId(ProtocolVersion.Latest, itemStack.Identifier);

    buffer.WriteVarInt(itemStack.Count);
    buffer.WriteVarInt(itemId);

    buffer.WriteVarInt(itemStack.Components.Count); // componentsToAddCount
    buffer.WriteVarInt(0); // componentsToRemoveCount

    foreach (var component in itemStack.Components)
    {
      buffer.WriteVarInt(GetComponentId(component, ProtocolVersion.Latest));
      component.Write(ref buffer);
    }
  }

  // GetId is a static interface member, so it is looked up once per component type
  private static int GetComponentId(IItemComponent component, ProtocolVersion protocolVersion)
  {
    var getId = ComponentIdGetters.GetOrAdd(component.GetType(), type =>
      type.GetMethod(nameof(IItemComponent<>.GetId), BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Func<ProtocolVersion, int>>()
      ?? throw new InvalidOperationException($"{type} has no static {nameof(IItemComponent<>.GetId)} method."));

    return getId(protocolVersion);
  }
}
