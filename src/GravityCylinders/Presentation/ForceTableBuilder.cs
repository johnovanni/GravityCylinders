using GravityCylinders.Physics;

namespace GravityCylinders.Presentation;

/// <summary>One step-by-step breakdown of the force on a cylinder from one source.</summary>
public sealed record ForceRow(string Cylinder, string Source, string NewtonianPerMetre, string Coupling,
    string PerFrameMetre, string PerElement, string Overlap, string Total);

public sealed record CylinderRow(string Name, string Speed, string Gamma, string Length, string NetPerMetre,
    string NetTotal, IReadOnlyList<ForceRow> Pairs);

/// <summary>Turns solver results into the text of the force table.</summary>
public static class ForceTableBuilder
{
    public const string Footnote =
        "Per proper metre (÷γ) is the force on each metre of the cylinder's own length, per unit of this frame's time " +
        "(time dilation). In this frame the cylinder is γ times shorter, so those metres are γ times denser and the " +
        "total is per frame metre × overlap.";

    /// <summary>The model factor; a GR excess too small for three decimals is shown explicitly, e.g. "× (1 + 2e-4)".</summary>
    public static string CouplingText(double coupling)
    {
        double excess = coupling - 1;
        return excess != 0 && Math.Abs(excess) < 1e-3 ? $"× (1 + {excess:0.##e0})" : $"× {coupling:0.###}";
    }

    public static IReadOnlyList<CylinderRow> Build(SceneForces forces) =>
        forces.Cylinders.Select(c =>
        {
            string name = CylinderName(c.Index);
            var pairs = c.Pairs.Select(p => new ForceRow(
                name,
                $"from {CylinderName(p.Source)}",
                Phys.Eng(p.NewtonianPerLength, "N/m"),
                CouplingText(p.Coupling),
                Phys.Eng(p.PerFrameLength, "N/m"),
                Phys.Eng(p.PerProperLengthInFrame, "N/m"),
                Phys.Eng(p.OverlapInFrame, "m"),
                Phys.Eng(p.TotalInFrame, "N"))).ToList();
            return new CylinderRow(
                name,
                $"β = {HudFormatter.Signed(c.BetaInFrame)}",
                $"γ = {c.Gamma:0.###}",
                Phys.Eng(c.LengthInFrame, "m"),
                Phys.Eng(c.NetPerFrameLength, "N/m"),
                Phys.Eng(c.NetTotal, "N"),
                pairs);
        }).ToList();

    public static string CylinderName(int index) => $"Cylinder {index + 1}";
}
