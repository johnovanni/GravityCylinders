namespace GravityCylinders.Physics;

/// <summary>
/// Independent check of the GR forces: builds the weak-field metric of every moving source directly in the observer's
/// frame, differentiates it numerically and applies the geodesic equation to each element of a cylinder. It shares no
/// formulas with <see cref="ForceSolver"/> beyond the Newtonian potential of a segment in its own rest frame.
/// </summary>
public static class WeakFieldMetricCheck
{
    private static readonly double[] Eta = [-1, 1, 1, 1];

    /// <summary>
    /// h_μν = −(2Φ/c²)(η_μν + 2u_μu_ν) of <paramref name="source"/> at (x⁰ = ct, x, y, z) in a frame moving at
    /// <paramref name="frameBeta"/>, with Φ taken from the source's rest-frame geometry at x′ = γ(x − βx⁰).
    /// </summary>
    public static double[,] Metric(Cylinder source, double frameBeta, double x0, double x, double y, double z)
    {
        double beta = Relativity.InFrame(source.Beta, frameBeta);
        double gamma = Phys.Gamma(beta);
        double rho = Math.Sqrt((y - source.Y) * (y - source.Y) + z * z);
        double phi = FiniteSegment.Potential(source.Lambda, source.Length, gamma * (x - beta * x0), rho);

        double[] uLower = [-gamma, gamma * beta, 0, 0];
        var h = new double[4, 4];
        double scale = -2 * phi / (Phys.C * Phys.C);
        for (int m = 0; m < 4; m++)
            for (int n = 0; n < 4; n++)
                h[m, n] = scale * ((m == n ? Eta[m] : 0) + 2 * uLower[m] * uLower[n]);
        return h;
    }

    /// <summary>Net y-force (N) on cylinder <paramref name="on"/> in the frame moving at <paramref name="frameBeta"/>.</summary>
    public static double NetForceInFrame(IReadOnlyList<Cylinder> cylinders, int on, double frameBeta, int samples = 4000)
    {
        var body = cylinders[on];
        double beta = Relativity.InFrame(body.Beta, frameBeta);
        double gamma = Phys.Gamma(beta);
        double[] u = [gamma, gamma * beta, 0, 0];

        double minSeparation = double.MaxValue;
        for (int j = 0; j < cylinders.Count; j++)
            if (j != on) minSeparation = Math.Min(minSeparation, Math.Abs(cylinders[j].Y - body.Y));
        double eps = 1e-4 * minSeparation;

        double length = body.Length / gamma, dx = length / samples, total = 0;
        for (int s = 0; s < samples; s++)
        {
            double[] position = [0, -length / 2 + (s + 0.5) * dx, body.Y, 0];
            double acceleration = 0;
            for (int j = 0; j < cylinders.Count; j++)
                if (j != on) acceleration += GeodesicAccelerationY(cylinders[j], frameBeta, position, u, eps);
            // Mass per metre of this frame is γλ; dp/dt = (dp/dτ)/γ, so the γ's cancel.
            total += body.Lambda * acceleration * dx;
        }
        return total;
    }

    /// <summary>d²y/dτ² = −c² Γʸ_αβ Uᵅ Uᵝ with Γʸ_αβ Uᵅ Uᵝ = Σ (∂_α h_yβ − ½ ∂_y h_αβ) Uᵅ Uᵝ (η^yy = 1).</summary>
    private static double GeodesicAccelerationY(Cylinder source, double frameBeta, double[] p, double[] u, double eps)
    {
        const int Y = 2;
        var dh = new double[4][,];
        for (int a = 0; a < 4; a++)
        {
            double[] plus = (double[])p.Clone(), minus = (double[])p.Clone();
            plus[a] += eps;
            minus[a] -= eps;
            var hp = Metric(source, frameBeta, plus[0], plus[1], plus[2], plus[3]);
            var hm = Metric(source, frameBeta, minus[0], minus[1], minus[2], minus[3]);
            dh[a] = new double[4, 4];
            for (int m = 0; m < 4; m++)
                for (int n = 0; n < 4; n++)
                    dh[a][m, n] = (hp[m, n] - hm[m, n]) / (2 * eps);
        }

        double gammaUU = 0;
        for (int a = 0; a < 4; a++)
            for (int b = 0; b < 4; b++)
                gammaUU += (dh[a][Y, b] - 0.5 * dh[Y][a, b]) * u[a] * u[b];
        return -Phys.C * Phys.C * gammaUU;
    }
}
