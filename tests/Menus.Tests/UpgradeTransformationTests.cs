using Menus.Protocol.Packets.Clientbound;
using Menus.Protocol.Packets.Serverbound;
using Menus.Protocol.Transformations;
using Microsoft.Extensions.Logging.Abstractions;
using Void.Minecraft.Buffers;
using Void.Minecraft.Network;
using Void.Minecraft.Network.Messages.Packets;
using Void.Minecraft.Network.Registries.Transformations.Mappings;

namespace Menus.Tests;

// Received packets are upgraded to the latest layout, then decoded as the latest version
public class UpgradeTransformationTests
{
  private delegate void Step(IMinecraftBinaryPacketWrapper wrapper, ProtocolVersion fromProtocolVersion, ProtocolVersion toProtocolVersion);

  private static readonly PlayerActionTransformation PlayerAction = new(NullLogger<PlayerActionTransformation>.Instance);
  private static readonly SwingArmTransformation SwingArm = new(NullLogger<SwingArmTransformation>.Instance);
  private static readonly SetHeldItemTransformation SetHeldItem = new(NullLogger<SetHeldItemTransformation>.Instance);
  private static readonly PlaceRecipeTransformation PlaceRecipe = new(NullLogger<PlaceRecipeTransformation>.Instance);
  private static readonly SetContainerPropertyTransformation SetContainerProperty = new(NullLogger<SetContainerPropertyTransformation>.Instance);
  private static readonly ItemStackTransformation ItemStack = new(NullLogger<ItemStackTransformation>.Instance);
  private static readonly SetContainerSlotTransformation SetContainerSlot = new(NullLogger<SetContainerSlotTransformation>.Instance, ItemStack);
  private static readonly SetContainerContentTransformation SetContainerContent = new(NullLogger<SetContainerContentTransformation>.Instance, ItemStack);

  private static byte[] Upgrade(byte[] bytes, params Step[] steps)
  {
    var wrapper = new TestPacketWrapper(bytes);

    foreach (var step in steps)
    {
      step(wrapper, ProtocolVersion.Oldest, ProtocolVersion.Latest);
      wrapper.Reset();
    }

    var stream = new MemoryStream();
    var buffer = new MinecraftBuffer(stream);
    wrapper.WriteProcessedValues(ref buffer);
    return stream.ToArray();
  }

  private static T Decode<T>(byte[] bytes) where T : IMinecraftPacket<T>
  {
    var buffer = new MinecraftBuffer(bytes.AsSpan());
    return T.Decode(ref buffer, ProtocolVersion.Latest);
  }

  [Fact]
  public void PlayerActionFrom1_7KeepsTheRestOfThePacket()
  {
    // Drop item, x, y, z and face
    byte[] bytes = [0x04, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0xFF];

    var upgraded = Upgrade(bytes, PlayerAction.UpgradeTo_1_9, PlayerAction.UpgradeTo_26_3);

    Assert.Equal([0x05, .. bytes[1..]], upgraded);
    Assert.Equal(PlayerActionServerboundPacket.DropItemStatus, Decode<PlayerActionServerboundPacket>(upgraded).Status);
  }

  [Theory]
  [InlineData(0x03, PlayerActionServerboundPacket.DropItemStackStatus)]
  [InlineData(0x04, PlayerActionServerboundPacket.DropItemStatus)]
  public void PlayerActionFrom1_8(byte status, int expected)
  {
    // Status, position and face
    byte[] bytes = [status, 0, 0, 0, 0, 0, 0, 0, 0, 0x00];

    var packet = Decode<PlayerActionServerboundPacket>(Upgrade(bytes, PlayerAction.UpgradeTo_1_9, PlayerAction.UpgradeTo_26_3));

    Assert.Equal(expected, packet.Status);
  }

  [Theory]
  [InlineData(0x00, 0)] // Start destroying a block
  [InlineData(0x01, 2)] // Abort destroying a block
  [InlineData(0x03, PlayerActionServerboundPacket.DropItemStackStatus)]
  [InlineData(0x04, PlayerActionServerboundPacket.DropItemStatus)]
  [InlineData(0x06, PlayerActionServerboundPacket.SwapItemInHandStatus)]
  public void PlayerActionFrom26_2(byte status, int expected)
  {
    // Status, position, face and sequence
    byte[] bytes = [status, 0, 0, 0, 0, 0, 0, 0, 0, 0x00, 0x07];

    var upgraded = Upgrade(bytes, PlayerAction.UpgradeTo_26_3);

    Assert.Equal(bytes[1..], upgraded[1..]);
    Assert.Equal(expected, Decode<PlayerActionServerboundPacket>(upgraded).Status);
  }

  [Theory]
  [InlineData(0x01)] // Change destroy direction
  [InlineData(0x05)] // Drop item
  [InlineData(0x07)] // Swap item in hand
  public void PlayerActionFrom26_3IsDecodedAsIs(byte status)
  {
    Assert.Equal(status, Decode<PlayerActionServerboundPacket>([status, 0, 0, 0, 0, 0, 0, 0, 0, 0x00, 0x07]).Status);
  }

  [Fact]
  public void SwingArmFrom1_7IsAMainHandSwing()
  {
    // Entity id and the swing animation
    var upgraded = Upgrade([0x00, 0x00, 0x00, 0x2A, 0x01], SwingArm.UpgradeTo_1_8, SwingArm.UpgradeTo_1_9);

    Assert.Equal([0x00], upgraded);
  }

  [Fact]
  public void SwingArmFrom1_8IsAMainHandSwing()
  {
    var packet = Decode<SwingArmServerboundPacket>(Upgrade([], SwingArm.UpgradeTo_1_9));

    Assert.Equal(SwingArmServerboundPacket.MainHand, packet.Hand);
  }

  [Fact]
  public void SwingArmFrom1_9KeepsTheHand()
  {
    Assert.Equal(1, Decode<SwingArmServerboundPacket>([0x01]).Hand);
  }

  [Fact]
  public void SetHeldItemFromByteSlot()
  {
    var packet = Decode<SetHeldItemClientboundPacket>(Upgrade([0x03], SetHeldItem.UpgradeTo_1_21_4));

    Assert.Equal(3, packet.Slot);
  }

  [Theory]
  [InlineData(0xFE, -2)] // Player inventory regardless of the open container
  [InlineData(0xFF, -1)] // Cursor
  [InlineData(0x05, 5)]
  public void SetContainerSlotFromSignedByteContainerId(byte containerId, int expected)
  {
    // Container id, state id, slot and an empty item
    var upgraded = Upgrade([containerId, 0x01, 0x00, 0x24, 0x00], SetContainerSlot.UpgradeTo_1_21_2);

    Assert.Equal([0x01, 0x00, 0x24, 0x00], upgraded[^4..]);
    Assert.Equal(expected, Decode<SetContainerSlotClientboundPacket>(upgraded).ContainerId);
  }

  [Fact]
  public void SetContainerSlotFromVarIntContainerId()
  {
    Assert.Equal(-2, Decode<SetContainerSlotClientboundPacket>([0xFE, 0xFF, 0xFF, 0xFF, 0x0F]).ContainerId);
  }

  [Fact]
  public void SetContainerContentFromByteContainerId()
  {
    // Container id, state id, no items and an empty carried item
    var packet = Decode<SetContainerContentClientboundPacket>(Upgrade([0x64, 0x01, 0x00, 0x00], SetContainerContent.UpgradeTo_1_21_2));

    Assert.Equal(100, packet.ContainerId);
  }

  [Fact]
  public void SetContainerPropertyFromByteContainerId()
  {
    var packet = Decode<SetContainerPropertyClientboundPacket>(Upgrade([0x05, 0x00, 0x01, 0x00, 0x02], SetContainerProperty.UpgradeTo_1_21_2));

    Assert.Equal(5, packet.ContainerId);
    Assert.Equal(1, packet.Property);
    Assert.Equal(2, packet.Value);
  }

  [Fact]
  public void PlaceRecipeFromSignedByteContainerId()
  {
    // Container id, then the recipe and make all
    var packet = Decode<PlaceRecipeServerboundPacket>(Upgrade([0xFE, 0x03, 0x01], PlaceRecipe.UpgradeTo_1_21_2));

    Assert.Equal(-2, packet.ContainerId);
  }
}
