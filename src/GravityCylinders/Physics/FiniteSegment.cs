namespace GravityCylinders.Physics;

/// <summary>Newtonian gravity of thin, uniform, finite line segments lying along x and centred on x = 0.</summary>
public static class FiniteSegment
{
    /// <summary>
    /// Magnitude (N) of the transverse force between two parallel, centred segments at separation <paramref name="d"/>:
    /// (2Gλaλb/d)·[√(((A+B)/2)² + d²) − √(((A−B)/2)² + d²)], evaluated in the cancellation-free form
    /// (2Gλaλb/d)·AB / [√(((A+B)/2)² + d²) + √(((A−B)/2)² + d²)].
    /// </summary>
    public static double Force(double lambdaOn, double lengthOn, double lambdaSource, double lengthSource, double d)
    {
        double a = lengthOn, b = lengthSource;
        double sum = (a + b) / 2, diff = (a - b) / 2;
        double denominator = Math.Sqrt(sum * sum + d * d) + Math.Sqrt(diff * diff + d * d);
        return 2 * Phys.G * lambdaOn * lambdaSource / d * (a * b / denominator);
    }

    /// <summary>Potential (m²/s²) at axial position x and distance ρ from the axis of a segment of length B.</summary>
    public static double Potential(double lambda, double length, double x, double rho) =>
        -Phys.G * lambda * (Math.Asinh((x + length / 2) / rho) - Math.Asinh((x - length / 2) / rho));

    /// <summary>∂Φ/∂ρ (positive; the attraction toward the axis is −∂Φ/∂ρ).</summary>
    public static double RadialDerivative(double lambda, double length, double x, double rho)
    {
        double ap = x + length / 2, am = x - length / 2;
        return Phys.G * lambda / rho * (ap / Math.Sqrt(ap * ap + rho * rho) - am / Math.Sqrt(am * am + rho * rho));
    }
}
