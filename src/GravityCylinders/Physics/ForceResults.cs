namespace GravityCylinders.Physics;

/// <summary>Observer frames: the lab, or the rest frame of one of the cylinders.</summary>
public enum FrameChoice { Lab, Cylinder1, Cylinder2, Cylinder3 }

public static class FrameChoices
{
    public static string DisplayName(FrameChoice frame) => frame switch
    {
        FrameChoice.Lab => "Lab",
        FrameChoice.Cylinder1 => "Cylinder 1",
        FrameChoice.Cylinder2 => "Cylinder 2",
        _ => "Cylinder 3",
    };

    /// <summary>Index of the cylinder whose rest frame this is, or −1 for the lab.</summary>
    public static int CylinderIndex(FrameChoice frame) => (int)frame - 1;
}

/// <summary>
/// Force on one cylinder from one other cylinder, as seen in the chosen frame. Forces are y-components, so their
/// sign points toward the source.
/// </summary>
/// <param name="Source">Index of the cylinder doing the pulling.</param>
/// <param name="NewtonianPerLength">2Gλᵢλⱼ/d (N/m) — the Newtonian pull per metre.</param>
/// <param name="Coupling">Model factor multiplying it (γ²(1 + w²) in GR, otherwise 1).</param>
/// <param name="PerProperLengthInFrame">Force per proper metre of the cylinder, per unit of this frame's time (÷ γ).</param>
/// <param name="PerFrameLength">Force per metre as measured in this frame — the same in every frame.</param>
/// <param name="OverlapInFrame">Length over which the two cylinders overlap in this frame (m).</param>
/// <param name="TotalInFrame">Total force on the overlapping section, in this frame (N).</param>
public sealed record PairForce(int Source, double NewtonianPerLength, double Coupling, double PerProperLengthInFrame,
    double PerFrameLength, double OverlapInFrame, double TotalInFrame);

public sealed record CylinderForces(int Index, double BetaInFrame, double Gamma, double LengthInFrame,
    IReadOnlyList<PairForce> Pairs, double NetPerFrameLength, double NetTotal);

/// <param name="WeakFieldParameter">Largest Gλ/c² in the scene; the weak-field model needs it ≪ 1.</param>
public sealed record SceneForces(PhysicsModel Model, FrameChoice Frame, double FrameBeta,
    IReadOnlyList<CylinderForces> Cylinders, double WeakFieldParameter);
