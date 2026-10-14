using GravityCylinders.Physics;
using GravityCylinders.Presentation;

namespace GravityCylinders.Tests.Presentation;

public class PresentationTests : InvariantCultureTest
{
    private static Cylinder[] Scene(double b0, double b1, double b2) =>
    [
        new(1e12, 3e5, 0.1, -3, b0),
        new(1e12, 3e5, 0.1, 0, b1),
        new(1e12, 3e5, 0.1, 3, b2),
    ];

    [Theory]
    [InlineData(9.9e-4, BadgeLevel.Green, "Gλ/c² = 9.9e-4 (weak field OK)")]
    [InlineData(1e-3, BadgeLevel.Amber, "Gλ/c² = 1.0e-3 (near weak-field limit)")]
    [InlineData(9.9e-3, BadgeLevel.Amber, "Gλ/c² = 9.9e-3 (near weak-field limit)")]
    [InlineData(1e-2, BadgeLevel.Red, "Gλ/c² = 1.0e-2 (outside weak-field model)")]
    public void Badge_ThresholdsAndText(double parameter, BadgeLevel level, string text)
    {
        var badge = HudFormatter.Badge(parameter);
        Assert.Equal(level, badge.Level);
        Assert.Equal(text, badge.Text);
    }

    [Fact]
    public void Frame_NamesFrameSpeedAndModel()
    {
        var f = ForceSolver.Solve(Scene(0.3, -0.6, 0), PhysicsModel.GeneralRelativity, FrameChoice.Cylinder2);
        Assert.Equal("Frame: Cylinder 2 (β = −0.6)   Model: General relativity", HudFormatter.Frame(f));
    }

    [Fact]
    public void Frame_LabHasNoSpeed()
    {
        var f = ForceSolver.Solve(Scene(0.3, -0.6, 0), PhysicsModel.Newtonian, FrameChoice.Lab);
        Assert.Equal("Frame: Lab   Model: Newtonian", HudFormatter.Frame(f));
    }

    [Fact]
    public void Check_ShowsDirectValueAndRelativeDifference() =>
        Assert.Equal("Cylinder 1: direct metric calculation 1.23 TN (Δ = 0.04%)",
            HudFormatter.Check("Cylinder 1", 1.23e12, 1.23e12 * 1.0004, 1.23e12));

    [Fact]
    public void Check_ZeroNetUsesPairScale() =>
        Assert.Equal("Cylinder 2: direct metric calculation 2 kN (Δ = 0.00%)",
            HudFormatter.Check("Cylinder 2", 0, 2e3, 1e12));

    [Fact]
    public void CheckNotApplicable_Text() =>
        Assert.Equal("Direct metric calculation: n/a (GR only)", HudFormatter.CheckNotApplicable);

    [Fact]
    public void ModelHelp_SaysWhatEachModelIs()
    {
        Assert.Contains("not a self-consistent theory of gravity", HudFormatter.ModelHelp(PhysicsModel.SpecialRelativity));
        Assert.Contains("γ²(1 + w²)", HudFormatter.ModelHelp(PhysicsModel.GeneralRelativity));
        Assert.Contains("Galilean", HudFormatter.ModelHelp(PhysicsModel.Newtonian));
    }

    [Fact]
    public void SimultaneityNote_MentionsPerMetreAgreement() =>
        Assert.Contains("per metre", HudFormatter.SimultaneityNote);

    [Fact]
    public void Build_ThreeRowsTwoPairsWithCoupling()
    {
        Cylinder[] scene = [new(1e12, 3e5, 0.1, -3, 0.6), new(1e12, 3e5, 0.1, 0, 0), new(1e12, 3e5, 0.1, 1e9, 0)];
        var rows = ForceTableBuilder.Build(ForceSolver.Solve(scene, PhysicsModel.GeneralRelativity, FrameChoice.Lab));
        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.Equal(2, r.Pairs.Count));
        Assert.Equal("Cylinder 1", rows[0].Name);
        Assert.Equal("β = 0.6", rows[0].Speed);
        Assert.Equal("γ = 1.25", rows[0].Gamma);
        Assert.Equal("240 km", rows[0].Length);
        var fromSecond = rows[0].Pairs[0];
        Assert.Equal("from Cylinder 2", fromSecond.Source);
        Assert.Equal("× 2.125", fromSecond.Coupling);
    }

    [Fact]
    public void Build_ZeroNetShowsZero()
    {
        var rows = ForceTableBuilder.Build(ForceSolver.Solve(Scene(0.6, 0, -0.6), PhysicsModel.GeneralRelativity, FrameChoice.Lab));
        Assert.Equal("0 N", rows[1].NetTotal);
        Assert.Equal("0 N/m", rows[1].NetPerMetre);
    }

    [Theory]
    [InlineData(0.5, "0.5")]
    [InlineData(-0.6, "−0.6")]
    [InlineData(0.999949, "0.999949")]
    [InlineData(-0.99995, "−0.99995")]
    [InlineData(0.998686, "0.998686")]
    [InlineData(1.98, "1.98")]
    public void Signed_NeverRoundsAMassiveBodyUpToC(double beta, string expected) =>
        Assert.Equal(expected, HudFormatter.Signed(beta));

    [Theory]
    [InlineData(1.0, "× 1")]
    [InlineData(2.125, "× 2.125")]
    [InlineData(1.00020002, "× (1 + 2e-4)")]
    [InlineData(1.0000002, "× (1 + 2e-7)")]
    public void CouplingText_ShowsSmallGrExcess(double coupling, string expected) =>
        Assert.Equal(expected, ForceTableBuilder.CouplingText(coupling));

    [Fact]
    public void Build_ShowsPerFrameMetreSoTheBreakdownCloses()
    {
        // Final review minor: per proper metre × overlap ≠ total when γ ≠ 1; per frame metre × overlap = total.
        Cylinder[] scene = [new(1e12, 3e5, 0.1, -3, 0.6), new(1e12, 3e5, 0.1, 0, 0), new(1e12, 3e5, 0.1, 1e9, 0)];
        var forces = ForceSolver.Solve(scene, PhysicsModel.GeneralRelativity, FrameChoice.Cylinder2);
        var row = ForceTableBuilder.Build(forces)[0].Pairs[0];
        Assert.Equal(Phys.Eng(forces.Cylinders[0].Pairs[0].PerFrameLength, "N/m"), row.PerFrameMetre);
        Assert.Contains("per frame metre × overlap", ForceTableBuilder.Footnote);
    }
}
