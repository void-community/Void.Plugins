using Menus.Api;

namespace Menus.Tests;

public class MenuExtensionsTests
{
  private static readonly MenuItem Stone = new("minecraft:stone");
  private static readonly MenuItem Glass = new("minecraft:glass_pane");

  private static Menu CreateMenu(Dictionary<int, MenuItem>? items = null) => new("Test", "minecraft:generic_9x1", 9, "Test", items ?? []);

  [Fact]
  public void FillSetsOnlyEmptySlotsAndKeepsTheOriginal()
  {
    var menu = CreateMenu(new() { [0] = Stone });

    var filled = menu.Fill(Glass);

    Assert.Equal(9, filled.ItemsMap.Count);
    Assert.Same(Stone, filled.ItemsMap[0]);
    Assert.All(Enumerable.Range(1, 8), slot => Assert.Same(Glass, filled.ItemsMap[slot]));
    Assert.Single(menu.ItemsMap);
  }

  [Fact]
  public void FillCoversThePlayerInventoryOfInventoryMenus()
  {
    var inventory = new Menu("Inventory", "minecraft:inventory", 9, "", new() { [36] = Stone });

    var filled = inventory.Fill(Glass);

    // Crafting and armor slots, main inventory, hotbar and offhand
    Assert.Equal(46, filled.ItemsMap.Count);
    Assert.Same(Stone, filled.ItemsMap[36]);
    Assert.Same(Glass, filled.ItemsMap[45]);
  }

  [Fact]
  public void WithPagePlacesItemsAndClearsLeftoverSlots()
  {
    var items = Enumerable.Range(0, 5).Select(i => new MenuItem("minecraft:stone", Count: i + 1)).ToList();
    int[] slots = [1, 2, 3];
    var menu = CreateMenu(new() { [0] = Glass, [3] = Glass });

    var first = menu.WithPage(items, slots, 0);
    var second = menu.WithPage(items, slots, 1);

    Assert.Equal([1, 2, 3], slots.Select(slot => first.ItemsMap[slot].Count));
    Assert.Equal(4, second.ItemsMap[1].Count);
    Assert.Equal(5, second.ItemsMap[2].Count);
    Assert.False(second.ItemsMap.ContainsKey(3));
    Assert.Same(Glass, second.ItemsMap[0]);
    Assert.Same(Glass, menu.ItemsMap[3]);
  }

  [Theory]
  [InlineData(0, 7, 1)]
  [InlineData(7, 7, 1)]
  [InlineData(8, 7, 2)]
  [InlineData(21, 7, 3)]
  public void PageCountRoundsUp(int itemCount, int slotsPerPage, int pageCount)
  {
    Assert.Equal(pageCount, MenuExtensions.GetPageCount(itemCount, slotsPerPage));
  }
}
