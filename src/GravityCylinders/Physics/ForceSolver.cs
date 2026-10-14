namespace GravityCylinders.Physics;

/// <summary>
/// Gravitational forces between the cylinders for a given model and observer frame.
/// <para>
/// Per element: the pull per proper metre is f = (2Gλᵢλⱼ/d)·K(w), with K = γ²(1 + w²) in GR and 1 otherwise. In
/// frame k each element feels f/γᵢₖ (time dilation), and per metre of frame k that is f again.
/// </para>
/// <para>
/// Totals: which parts of two sliding cylinders overlap "at one moment" depends on the frame, so totals are summed over
/// i's elements as they lie in frame k (length Lᵢ/γᵢₖ) in the boosted field of source j, Φⱼ(γⱼₖx). Substituting
/// x′ = γⱼₖx turns this into the exact closed form K·F_seg(λᵢ, γⱼₖLᵢ/γᵢₖ, λⱼ, Lⱼ, d)/γⱼₖ, whose long-cylinder limit is
/// f × min(Lᵢ/γᵢₖ, Lⱼ/γⱼₖ).
/// </para>
/// </summary>
public static class ForceSolver
{
    /// <summary>A net force smaller than this fraction of the summed pair magnitudes is rounding noise and becomes 0.</summary>
    public const double NetSnapFraction = 1e-12;

    public static SceneForces Solve(IReadOnlyList<Cylinder> cylinders, PhysicsModel model, FrameChoice frame)
    {
        var transform = PhysicsModels.Frames(model);
        int frameIndex = FrameChoices.CylinderIndex(frame);
        double frameBeta = frameIndex >= 0 ? cylinders[frameIndex].Beta : 0;

        var betas = cylinders.Select(c => transform.VelocityInFrame(c.Beta, frameBeta)).ToArray();
        var gammas = betas.Select(transform.Gamma).ToArray();

        var results = new List<CylinderForces>(cylinders.Count);
        for (int i = 0; i < cylinders.Count; i++)
        {
            var on = cylinders[i];
            var pairs = new List<PairForce>(cylinders.Count - 1);
            for (int j = 0; j < cylinders.Count; j++)
            {
                if (j == i) continue;
                var source = cylinders[j];
                double d = Math.Abs(source.Y - on.Y);
                double sign = Math.Sign(source.Y - on.Y);
                double coupling = PhysicsModels.Coupling(model, PhysicsModels.RelativeSpeed(model, on.Beta, source.Beta));
                double newtonian = sign * 2 * Phys.G * on.Lambda * source.Lambda / d;
                double lengthOn = on.Length / gammas[i], lengthSource = source.Length / gammas[j];
                pairs.Add(new PairForce(
                    Source: j,
                    NewtonianPerLength: newtonian,
                    Coupling: coupling,
                    PerProperLengthInFrame: newtonian * coupling / gammas[i],
                    PerFrameLength: newtonian * coupling,
                    OverlapInFrame: Math.Min(lengthOn, lengthSource),
                    TotalInFrame: sign * coupling
                        * FiniteSegment.Force(on.Lambda, gammas[j] * lengthOn, source.Lambda, source.Length, d) / gammas[j]));
            }
            results.Add(new CylinderForces(i, betas[i], gammas[i], on.Length / gammas[i], pairs,
                Snap(pairs.Select(p => p.PerFrameLength)), Snap(pairs.Select(p => p.TotalInFrame))));
        }

        double weakField = cylinders.Max(c => Phys.G * c.Lambda / (Phys.C * Phys.C));
        return new SceneForces(model, frame, frameBeta, results, weakField);
    }

    private static double Snap(IEnumerable<double> parts)
    {
        double sum = 0, magnitude = 0;
        foreach (double p in parts)
        {
            sum += p;
            magnitude += Math.Abs(p);
        }
        return Math.Abs(sum) < NetSnapFraction * magnitude ? 0 : sum;
    }
}
