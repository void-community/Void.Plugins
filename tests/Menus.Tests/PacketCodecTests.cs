using Menus.Extensions;
using Menus.Minecraft;
using Menus.Minecraft.Components.Item;
using Menus.Protocol.Packets.Clientbound;
using Menus.Protocol.Packets.Serverbound;
using Void.Data.Api.Minecraft;
using Void.Minecraft.Buffers;
using Void.Minecraft.Nbt.Tags;
using Void.Minecraft.Network;

namespace Menus.Tests;

public class PacketCodecTests
{
  [Fact]
  public void EmptyItemStackIsWrittenAsZeroCount()
  {
    var bytes = new byte[8];
    var buffer = new MinecraftBuffer(bytes.AsSpan());

    buffer.WriteItemStack(null);
    buffer.WriteItemStack(new ItemStack { Identifier = "minecraft:stone", Count = 0 });

    Assert.Equal(2, buffer.Position);
    Assert.Equal([0x00, 0x00], bytes[..2]);
  }

  [Fact]
  public void ItemStackWritesCountIdAndComponents()
  {
    var bytes = new byte[64];
    var buffer = new MinecraftBuffer(bytes.AsSpan());

    buffer.WriteItemStack(new ItemStack
    {
      Identifier = "minecraft:stone",
      Count = 3,
      Components = [new DamageItemComponent { Value = 7 }]
    });

    var read = new MinecraftBuffer(bytes.AsSpan());
    Assert.Equal(3, read.ReadVarInt());
    Assert.Equal(MinecraftItemRegistry.GetId(ProtocolVersion.Latest, "minecraft:stone"), read.ReadVarInt());
    Assert.Equal(1, read.ReadVarInt()); // Components to add
    Assert.Equal(0, read.ReadVarInt()); // Components to remove
    Assert.Equal(DamageItemComponent.GetId(ProtocolVersion.Latest), read.ReadVarInt());
    Assert.Equal(7, read.ReadVarInt());
    Assert.Equal(buffer.Position, read.Position);
  }

  [Fact]
  public void CustomDataUsesNetworkNbtWithoutRootName()
  {
    var bytes = new byte[32];
    var buffer = new MinecraftBuffer(bytes.AsSpan());

    new CustomDataItemComponent { Value = new NbtCompound { ["a"] = new NbtByte(1) } }.Write(ref buffer);

    // Compound, then straight to the byte tag "a" = 1, then end
    Assert.Equal([0x0A, 0x01, 0x00, 0x01, (byte) 'a', 0x01, 0x00], bytes[..(int) buffer.Position]);

    var read = new MinecraftBuffer(bytes.AsSpan());
    Assert.IsType<NbtCompound>(CustomDataItemComponent.Read(ref read).Value);
    Assert.Equal(buffer.Position, read.Position);
  }

  [Fact]
  public void SetContainerContentWritesItemsAndCarriedItem()
  {
    var bytes = new byte[64];
    var buffer = new MinecraftBuffer(bytes.AsSpan());

    new SetContainerContentClientboundPacket
    {
      ContainerId = 127,
      StateId = 5,
      ItemStacks = [null, null, null]
    }.Encode(ref buffer, ProtocolVersion.Latest);

    // Container id, state id, count, 3 empty items, empty carried item
    Assert.Equal([127, 5, 3, 0, 0, 0, 0], bytes[..(int) buffer.Position]);
  }

  [Fact]
  public void ClickContainerDecodesSlotOutsideTheWindowAsNegative()
  {
    // Container 127, state id 0, slot -999, button 0, mode 0, no changed slots, empty carried item
    byte[] bytes = [0x7F, 0x00, 0xFC, 0x19, 0x00, 0x00, 0x00, 0x00];
    var buffer = new MinecraftBuffer(bytes.AsSpan());

    var packet = ClickContainerServerboundPacket.Decode(ref buffer, ProtocolVersion.Latest);

    Assert.Equal(127, packet.ContainerId);
    Assert.Equal(-999, packet.Slot);
    Assert.Equal([0x00, 0x00], packet.SkippedBytes);
  }

  [Fact]
  public void ClickContainerEncodesNegativeSlot()
  {
    var bytes = new byte[16];
    var buffer = new MinecraftBuffer(bytes.AsSpan());

    new ClickContainerServerboundPacket { ContainerId = 0, StateId = 0, Slot = -999, Button = 0, Mode = 0, SkippedBytes = [] }
      .Encode(ref buffer, ProtocolVersion.Latest);

    Assert.Equal([0x00, 0x00, 0xFC, 0x19, 0x00, 0x00], bytes[..(int) buffer.Position]);
  }

  [Fact]
  public void SwingArmIsReplacedByPunchIn26_3()
  {
    Assert.DoesNotContain(SwingArmServerboundPacket.Mappings, mapping => mapping.ProtocolVersion >= ProtocolVersion.MINECRAFT_26_3);
    Assert.Equal(ProtocolVersion.MINECRAFT_26_3, PunchServerboundPacket.Mappings[0].ProtocolVersion);
    Assert.All(PunchServerboundPacket.Mappings, mapping => Assert.True(mapping.Id >= 0));
  }

  [Fact]
  public void HeldSlotAndSwingMappingsResolveForEveryVersion()
  {
    // Registry lookups return -1 for versions without the packet
    Assert.All(SwingArmServerboundPacket.Mappings, mapping => Assert.True(mapping.Id >= 0));
    Assert.All(SetHeldItemServerboundPacket.Mappings, mapping => Assert.True(mapping.Id >= 0));
    Assert.All(SetHeldItemClientboundPacket.Mappings, mapping => Assert.True(mapping.Id >= 0));
  }
}
