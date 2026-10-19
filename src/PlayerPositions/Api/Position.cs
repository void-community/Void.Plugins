namespace PlayerPositions.Api;

public class Position
{
  public double X { get; internal set; }
  public double Y { get; internal set; }
  public double Z { get; internal set; }
  public float Yaw { get; internal set; }
  public float Pitch { get; internal set; }
  public int Flags { get; internal set; }

  public bool IsOnGround()
  {
    return (Flags & 0x01) != 0;
  }

  public bool IsPushingAgainstWall()
  {
    return (Flags & 0x02) != 0;
  }
}
