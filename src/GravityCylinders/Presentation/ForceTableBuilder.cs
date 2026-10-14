using GravityCylinders.Physics;

namespace GravityCylinders.Presentation;

/// <summary>One step-by-step breakdown of the force on a cylinder from one source.</summary>
public sealed record ForceRow(string Cylinder, string Source, string NewtonianPerMetre, string Coupling,
    string PerElement, string Overlap, string Total);

public sealed record CylinderRow(string Name, string Speed, string Gamma, string Length, string NetPerMetre,
    string NetTotal, IReadOnlyList<ForceRow> Pairs);

/// <summary>Turns solver results into the text of the force table.</summary>
public static class ForceTableBuilder
{
    public static IReadOnlyList<CylinderRow> Build(SceneForces forces) =>
        forces.Cylinders.Select(c =>
        {
            string name = CylinderName(c.Index);
            var pairs = c.Pairs.Select(p => new ForceRow(
                name,
                $"from {CylinderName(p.Source)}",
                Phys.Eng(p.NewtonianPerLength, "N/m"),
                $"× {p.Coupling:0.###}",
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
