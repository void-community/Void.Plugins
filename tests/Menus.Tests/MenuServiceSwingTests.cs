using System.Reflection;
using Void.Minecraft.Network.Messages.Packets;
using Menus.Api;
using Menus.Protocol.Packets.Clientbound;
using Menus.Protocol.Packets.Serverbound;
using Menus.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Void.Proxy.Api.Commands;
using Void.Proxy.Api.Events.Network;
using Void.Proxy.Api.Events.Services;
using Void.Proxy.Api.Links;
using Void.Proxy.Api.Network;
using Void.Proxy.Api.Network.Messages;
using Void.Proxy.Api.Players;
using Void.Proxy.Api.Players.Contexts;

namespace Menus.Tests;

public class MenuServiceSwingTests
{
  private static readonly MethodInfo OnMessageReceived = typeof(MenuService).GetMethod("OnMessageReceived", BindingFlags.Instance | BindingFlags.NonPublic)!;

  private static readonly Menu Inventory = new("Inventory", "minecraft:inventory", 9, "", new()
  {
    [36] = new MenuItem("minecraft:compass", "/first"),
    [40] = new MenuItem("minecraft:clock", "/fifth", "/fifth-right")
  });

  private readonly IPlayer player = Substitute.For<IPlayer>();
  private readonly ICommandService commands = Substitute.For<ICommandService>();
  private readonly MenuService menuService;

  public MenuServiceSwingTests()
  {
    var playerContext = Substitute.For<IPlayerContext>();
    playerContext.Player.Returns(player);

    menuService = new MenuService(NullLogger<MenuService>.Instance, playerContext, commands, Substitute.For<ILinkService>(), Substitute.For<IEventService>());
  }

  private async Task<MessageReceivedEvent> ReceiveAsync(INetworkMessage message)
  {
    var serverbound = message is IMinecraftServerboundPacket;
    var @event = new MessageReceivedEvent(
      serverbound ? Side.Client : Side.Server,
      serverbound ? Side.Client : Side.Server,
      serverbound ? Side.Server : Side.Client,
      serverbound ? Direction.Serverbound : Direction.Clientbound,
      message,
      Substitute.For<ILink>(),
      player);

    await (ValueTask) OnMessageReceived.Invoke(menuService, [@event, CancellationToken.None])!;
    return @event;
  }

  [Fact]
  public async Task SwingClicksTheFirstHotbarSlotByDefault()
  {
    await menuService.SetInventoryMenuAsync(Inventory, CancellationToken.None);

    var @event = await ReceiveAsync(new SwingArmServerboundPacket { Hand = SwingArmServerboundPacket.MainHand });

    Assert.True(@event.Result);
    await commands.Received(1).ExecuteAsync(player, "/first", Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task PunchClicksTheSlotSelectedByThePlayer()
  {
    await menuService.SetInventoryMenuAsync(Inventory, CancellationToken.None);
    await ReceiveAsync(new SetHeldItemServerboundPacket { Slot = 4 });

    var @event = await ReceiveAsync(new PunchServerboundPacket());

    Assert.True(@event.Result);
    await commands.Received(1).ExecuteAsync(player, "/fifth", Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SwingClicksTheSlotSelectedByTheBackend()
  {
    await ReceiveAsync(new SetHeldItemClientboundPacket { Slot = 4 });
    await menuService.SetInventoryMenuAsync(Inventory, CancellationToken.None);

    await ReceiveAsync(new SwingArmServerboundPacket { Hand = SwingArmServerboundPacket.MainHand });

    await commands.Received(1).ExecuteAsync(player, "/fifth", Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SwingWithAnEmptyHandReachesTheBackend()
  {
    await menuService.SetInventoryMenuAsync(Inventory, CancellationToken.None);
    await ReceiveAsync(new SetHeldItemServerboundPacket { Slot = 2 });

    var @event = await ReceiveAsync(new SwingArmServerboundPacket { Hand = SwingArmServerboundPacket.MainHand });

    Assert.False(@event.Result);
    await commands.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default!, default);
  }

  [Fact]
  public async Task OffHandSwingIsNotAClick()
  {
    await menuService.SetInventoryMenuAsync(Inventory, CancellationToken.None);

    var @event = await ReceiveAsync(new SwingArmServerboundPacket { Hand = 1 });

    Assert.False(@event.Result);
    await commands.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default!, default);
  }

  [Fact]
  public async Task SwingIsIgnoredWhileTheBackendOwnsTheInventory()
  {
    var @event = await ReceiveAsync(new SwingArmServerboundPacket { Hand = SwingArmServerboundPacket.MainHand });

    Assert.False(@event.Result);
    await commands.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default!, default);
  }

  [Fact]
  public async Task OutOfRangeHeldSlotIsIgnored()
  {
    await menuService.SetInventoryMenuAsync(Inventory, CancellationToken.None);
    await ReceiveAsync(new SetHeldItemServerboundPacket { Slot = 4 });
    await ReceiveAsync(new SetHeldItemServerboundPacket { Slot = 9 });

    await ReceiveAsync(new PunchServerboundPacket());

    await commands.Received(1).ExecuteAsync(player, "/fifth", Arg.Any<CancellationToken>());
  }

  public static TheoryData<INetworkMessage> SwingingActions => new()
  {
    new UseItemOnServerboundPacket(),
    new UseItemServerboundPacket(),
    new PlayerActionServerboundPacket { Status = PlayerActionServerboundPacket.DropItemStatus },
    new PlayerActionServerboundPacket { Status = PlayerActionServerboundPacket.DropItemStackStatus }
  };

  [Theory]
  [MemberData(nameof(SwingingActions))]
  public async Task SwingRightAfterUsingOrDroppingIsNotAClick(INetworkMessage action)
  {
    // Received before owning the inventory, which would resend it to the client
    await ReceiveAsync(action);
    await menuService.SetInventoryMenuAsync(Inventory, CancellationToken.None);

    var @event = await ReceiveAsync(new SwingArmServerboundPacket { Hand = SwingArmServerboundPacket.MainHand });

    Assert.False(@event.Result);
    await commands.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default!, default);
  }

  [Fact]
  public async Task LaterSwingAfterUsingIsAClick()
  {
    await ReceiveAsync(new UseItemOnServerboundPacket());
    await menuService.SetInventoryMenuAsync(Inventory, CancellationToken.None);

    await Task.Delay(150, TestContext.Current.CancellationToken);
    await ReceiveAsync(new SwingArmServerboundPacket { Hand = SwingArmServerboundPacket.MainHand });

    await commands.Received(1).ExecuteAsync(player, "/first", Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task PunchRightAfterUsingIsAClick()
  {
    // 26.3 sends the punch only for left clicks
    await ReceiveAsync(new UseItemOnServerboundPacket());
    await menuService.SetInventoryMenuAsync(Inventory, CancellationToken.None);

    await ReceiveAsync(new PunchServerboundPacket());

    await commands.Received(1).ExecuteAsync(player, "/first", Arg.Any<CancellationToken>());
  }
}
