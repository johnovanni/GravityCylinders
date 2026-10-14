using GravityCylinders.Physics;

namespace GravityCylinders.Presentation;

public enum BadgeLevel { Green, Amber, Red }

/// <summary>Text for the overlay in the 3D view and the explanations under the force table.</summary>
public static class HudFormatter
{
    public const double AmberThreshold = 1e-3;
    public const double RedThreshold = 1e-2;

    public const string CheckNotApplicable = "Direct metric calculation: n/a (GR only)";
    public const string CheckComputing = "Direct metric calculation: computing…";

    public const string SimultaneityNote =
        "Forces per metre are the same in every frame, but totals are not: the cylinders slide past each other, and " +
        "observers in different frames disagree about which parts overlap \"at the same moment\" (relativity of simultaneity).";

    public const string GalileanNote =
        "Galilean frames: lengths, clocks and forces — per metre and in total — are the same in every frame.";

    public static string SimultaneityNoteFor(PhysicsModel model) =>
        model == PhysicsModel.Newtonian ? GalileanNote : SimultaneityNote;

    public static (string Text, BadgeLevel Level) Badge(double weakFieldParameter)
    {
        string value = $"Gλ/c² = {weakFieldParameter:0.0e0}";
        if (weakFieldParameter < AmberThreshold) return ($"{value} (weak field OK)", BadgeLevel.Green);
        if (weakFieldParameter < RedThreshold) return ($"{value} (near weak-field limit)", BadgeLevel.Amber);
        return ($"{value} (outside weak-field model)", BadgeLevel.Red);
    }

    public static string Frame(SceneForces f)
    {
        string frame = f.Frame == FrameChoice.Lab
            ? "Lab"
            : $"{FrameChoices.DisplayName(f.Frame)} (β = {Signed(f.FrameBeta)})";
        return $"Frame: {frame}   Model: {PhysicsModels.DisplayName(f.Model)}";
    }

    /// <summary>
    /// The direct metric result for one cylinder and its difference from the solver, relative to
    /// <paramref name="scale"/> (the summed pair magnitudes, so a net that cancels to zero still gets a sensible Δ).
    /// </summary>
    public static string Check(string cylinderName, double solver, double direct, double scale)
    {
        double delta = scale > 0 ? Math.Abs(direct - solver) / scale * 100 : 0;
        return $"{cylinderName}: direct metric calculation {Phys.Eng(direct, "N")} (Δ = {delta:0.00}%)";
    }

    public static string ModelHelp(PhysicsModel model) => model switch
    {
        PhysicsModel.Newtonian =>
            "Newtonian: rest mass attracts rest mass, 2Gλ₁λ₂/d per metre, whatever the speeds. Frames are Galilean: " +
            "speeds simply subtract, nothing contracts and every observer measures the same force.",
        PhysicsModel.SpecialRelativity =>
            "Special relativity: Newton's pull in each cylinder's own rest frame, combined with relativistic kinematics — " +
            "velocity addition, length contraction and time dilation (a force seen from another frame is divided by γ). " +
            "This is not a self-consistent theory of gravity; it isolates what relativistic kinematics alone does.",
        _ =>
            "General relativity (weak field): gravity couples to energy and momentum, not just rest mass. In a cylinder's " +
            "rest frame the pull of a neighbour moving at relative speed w is multiplied by γ²(1 + w²): co-moving cylinders " +
            "feel exactly Newton's force, and the pull grows steeply as w approaches c. Frames work as in special relativity.",
    };

    /// <summary>
    /// A speed with a true minus sign, e.g. −0.6. Speeds of 0.99c and above get enough digits that a massive body is
    /// never shown as moving at exactly c.
    /// </summary>
    public static string Signed(double value)
    {
        double magnitude = Math.Abs(value);
        string text = value.ToString(magnitude >= 0.99 && magnitude < 1 ? "0.######" : "0.###");
        if (magnitude < 1 && text.TrimStart('-') == "1") text = value.ToString("0.############");
        return text.Replace('-', '−');
    }
}
