namespace GravityCylinders.Physics;

/// <summary>
/// A long, non-rotating cylinder of mass lying along x at transverse position <see cref="Y"/> (z = 0).
/// <see cref="Lambda"/> is the proper (rest-frame) mass per metre, <see cref="Length"/> the proper length, and
/// <see cref="Beta"/> the lab-frame speed along x as a fraction of c.
/// </summary>
public sealed record Cylinder(double Lambda, double Length, double Radius, double Y, double Beta);
