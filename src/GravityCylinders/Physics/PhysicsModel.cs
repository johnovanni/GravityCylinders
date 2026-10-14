namespace GravityCylinders.Physics;

/// <summary>
/// The three descriptions of the same scene. Each layer adds to the previous one: Newtonian gravity with Galilean
/// frames; then relativistic kinematics (velocity addition, contraction, time dilation) with Newtonian coupling; then
/// weak-field GR, where the pull also depends on the relative speed through γ²(1 + w²).
/// </summary>
public enum PhysicsModel { Newtonian, SpecialRelativity, GeneralRelativity }

public static class PhysicsModels
{
    public static string DisplayName(PhysicsModel model) => model switch
    {
        PhysicsModel.Newtonian => "Newtonian",
        PhysicsModel.SpecialRelativity => "Special relativity",
        _ => "General relativity",
    };

    /// <summary>Factor multiplying the Newtonian pull per unit proper length, for relative speed w.</summary>
    public static double Coupling(PhysicsModel model, double w) =>
        model == PhysicsModel.GeneralRelativity ? Relativity.CouplingGR(w) : 1.0;

    public static double RelativeSpeed(PhysicsModel model, double betaA, double betaB) =>
        model == PhysicsModel.Newtonian ? Math.Abs(betaA - betaB) : Relativity.RelativeSpeed(betaA, betaB);

    public static FrameTransform Frames(PhysicsModel model) =>
        model == PhysicsModel.Newtonian ? FrameTransform.Galilean : FrameTransform.Lorentz;
}
