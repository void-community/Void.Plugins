using Void.Data.Api.Minecraft;
using Void.Minecraft.Components.Text;
using Void.Minecraft.Nbt.Tags;
using Void.Minecraft.Profiles;

namespace Menus.Api;

public record MenuItem(Identifier Identifier, string? LeftClickCommand = null, string? RightClickCommand = null, int Count = 1, Component? Title = null, List<Component>? Lore = null, GameProfile? Profile = null)
{
  /// <summary>
  /// Called for every click on the item, the commands only run for <see cref="ClickType.LeftMouseButton"/> and <see cref="ClickType.RightMouseButton"/>.
  /// A double click also arrives as the single clicks before it.
  /// </summary>
  public Func<MenuClick, CancellationToken, ValueTask>? OnClick { get; init; }

  public int? Damage { get; init; }

  public NbtCompound? CustomData { get; init; }
}
