namespace Menus.Api;

public enum ClickType
{
  LeftMouseButton,
  RightMouseButton,
  ShiftLeftMouseButton,
  ShiftRightMouseButton,
  /// <summary>
  /// Only sent by clients in creative mode.
  /// </summary>
  MiddleMouseButton,
  NumberKey,
  SwapOffhand,
  Drop,
  DropStack,
  /// <summary>
  /// Sent after the single clicks of the same double click.
  /// </summary>
  DoubleClick,
}
