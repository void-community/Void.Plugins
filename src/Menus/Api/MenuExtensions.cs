namespace Menus.Api;

public static class MenuExtensions
{
  /// <summary>
  /// Returns a copy of the menu with every empty slot set to <paramref name="filler"/>.
  /// For player inventory menus this includes the main inventory, hotbar and offhand.
  /// </summary>
  public static Menu Fill(this Menu menu, MenuItem filler)
  {
    var itemsMap = new Dictionary<int, MenuItem>(menu.ItemsMap);

    // Inventory menu sizes only count the crafting and armor slots before the main inventory
    var size = menu.Type == "minecraft:inventory" ? menu.Size + 37 : menu.Size;

    for (var slot = 0; slot < size; slot++)
      itemsMap.TryAdd(slot, filler);

    return menu with { ItemsMap = itemsMap };
  }

  /// <summary>
  /// Returns a copy of the menu with one page of <paramref name="items"/> placed into <paramref name="slots"/>, in order.
  /// Slots left over on the last page are cleared.
  /// </summary>
  public static Menu WithPage(this Menu menu, IReadOnlyList<MenuItem> items, IReadOnlyList<int> slots, int page)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(page);

    var itemsMap = new Dictionary<int, MenuItem>(menu.ItemsMap);

    for (var i = 0; i < slots.Count; i++)
    {
      var index = page * slots.Count + i;

      if (index < items.Count)
        itemsMap[slots[i]] = items[index];
      else
        itemsMap.Remove(slots[i]);
    }

    return menu with { ItemsMap = itemsMap };
  }

  public static int GetPageCount(int itemCount, int slotsPerPage)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(slotsPerPage);

    return Math.Max(1, (itemCount + slotsPerPage - 1) / slotsPerPage);
  }
}
