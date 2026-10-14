namespace GravityCylinders.Physics;

/// <summary>Special-relativistic kinematics along x and the weak-field GR coupling factor. Speeds are β = v/c.</summary>
public static class Relativity
{
    /// <summary>Velocity of something moving at <paramref name="beta"/> (lab) as seen from a frame moving at <paramref name="frameBeta"/>.</summary>
    public static double InFrame(double beta, double frameBeta) => (beta - frameBeta) / (1 - beta * frameBeta);

    /// <summary>Speed of one body in the rest frame of the other.</summary>
    public static double RelativeSpeed(double a, double b) => Math.Abs(InFrame(a, b));

    /// <summary>
    /// Weak-field GR factor 2(u·U)² − 1 = γ²(1 + w²) by which a source's pull on a body moving at relative speed w
    /// exceeds the Newtonian pull (gravity couples to energy and momentum, not just rest mass).
    /// </summary>
    public static double CouplingGR(double w) => (1 + w * w) / (1 - w * w);
}
