using Menus.Api;
using Menus.Events;
using Menus.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Void.Proxy.Api.Commands;
using Void.Proxy.Api.Events.Services;
using Void.Proxy.Api.Links;
using Void.Proxy.Api.Players;
using Void.Proxy.Api.Players.Contexts;

namespace Menus.Tests;

public class MenuServiceClickTests
{
  private readonly IPlayer player = Substitute.For<IPlayer>();
  private readonly ICommandService commands = Substitute.For<ICommandService>();
  private readonly IEventService events = Substitute.For<IEventService>();
  private readonly MenuService menuService;

  public MenuServiceClickTests()
  {
    var playerContext = Substitute.For<IPlayerContext>();
    playerContext.Player.Returns(player);

    menuService = new MenuService(NullLogger<MenuService>.Instance, playerContext, commands, Substitute.For<ILinkService>(), events);
  }

  [Theory]
  [InlineData(ClickType.LeftMouseButton, "/left")]
  [InlineData(ClickType.RightMouseButton, "/right")]
  public async Task PlainClicksRunTheirCommand(ClickType clickType, string command)
  {
    var menu = new Menu("Test", "minecraft:generic_9x1", 9, "Test", new() { [4] = new MenuItem("minecraft:stone", "/left", "/right") });

    await menuService.ClickSlotAsync(menu, 4, clickType, CancellationToken.None);

    await commands.Received(1).ExecuteAsync(player, command, Arg.Any<CancellationToken>());
  }

  [Theory]
  [InlineData(ClickType.ShiftLeftMouseButton)]
  [InlineData(ClickType.MiddleMouseButton)]
  [InlineData(ClickType.DoubleClick)]
  public async Task OtherClicksOnlyReachTheCallback(ClickType clickType)
  {
    MenuClick? received = null;
    var item = new MenuItem("minecraft:stone", "/left", "/right") { OnClick = (click, _) => { received = click; return ValueTask.CompletedTask; } };
    var menu = new Menu("Test", "minecraft:generic_9x1", 9, "Test", new() { [2] = item });

    await menuService.ClickSlotAsync(menu, 2, clickType, CancellationToken.None);

    await commands.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default!, default);
    Assert.NotNull(received);
    Assert.Same(player, received.Player);
    Assert.Same(menu, received.Menu);
    Assert.Same(item, received.Item);
    Assert.Equal(2, received.Slot);
    Assert.Equal(clickType, received.Type);
    Assert.Null(received.HotbarSlot);
  }

  [Fact]
  public async Task ClickRaisesEventBeforeTheCallback()
  {
    var order = new List<string>();
    events.When(service => service.ThrowAsync(Arg.Any<MenuClickedEvent>(), Arg.Any<CancellationToken>())).Do(_ => order.Add("event"));
    var item = new MenuItem("minecraft:stone") { OnClick = (_, _) => { order.Add("callback"); return ValueTask.CompletedTask; } };
    var menu = new Menu("Test", "minecraft:generic_9x1", 9, "Test", new() { [0] = item });

    await menuService.ClickSlotAsync(menu, 0, ClickType.LeftMouseButton, CancellationToken.None);

    Assert.Equal(["event", "callback"], order);
  }

  [Fact]
  public async Task FailingCallbackStillRunsTheCommand()
  {
    var item = new MenuItem("minecraft:stone", "/left") { OnClick = (_, _) => throw new InvalidOperationException("broken handler") };
    var menu = new Menu("Test", "minecraft:generic_9x1", 9, "Test", new() { [0] = item });

    await menuService.ClickSlotAsync(menu, 0, ClickType.LeftMouseButton, CancellationToken.None);

    await commands.Received(1).ExecuteAsync(player, "/left", Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task FailingEventHandlerDoesNotStopTheClick()
  {
    events.ThrowAsync(Arg.Any<MenuClickedEvent>(), Arg.Any<CancellationToken>()).Returns(_ => throw new InvalidOperationException("broken listener"));
    var menu = new Menu("Test", "minecraft:generic_9x1", 9, "Test", new() { [0] = new MenuItem("minecraft:stone", "/left") });

    await menuService.ClickSlotAsync(menu, 0, ClickType.LeftMouseButton, CancellationToken.None);

    await commands.Received(1).ExecuteAsync(player, "/left", Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task EmptySlotDoesNothing()
  {
    var menu = new Menu("Test", "minecraft:generic_9x1", 9, "Test", new());

    await menuService.ClickSlotAsync(menu, 3, ClickType.LeftMouseButton, CancellationToken.None);

    await commands.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default!, default);
    await events.DidNotReceiveWithAnyArgs().ThrowAsync(default(MenuClickedEvent)!, default);
  }

  [Fact]
  public async Task PlayerInventoryMenuSlotsAreNotRemapped()
  {
    // Window 0 numbering: 36 is the first hotbar slot
    var inventory = new Menu("Inventory", "minecraft:inventory", 9, "", new() { [36] = new MenuItem("minecraft:compass", "/hotbar") });

    await menuService.ClickSlotAsync(inventory, 36, ClickType.LeftMouseButton, CancellationToken.None);

    await commands.Received(1).ExecuteAsync(player, "/hotbar", Arg.Any<CancellationToken>());
  }
}
