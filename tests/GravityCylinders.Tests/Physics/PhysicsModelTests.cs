using GravityCylinders.Physics;
using static GravityCylinders.Tests.TestHelpers;

namespace GravityCylinders.Tests.Physics;

public class PhysicsModelTests
{
    [Fact]
    public void DisplayNames_AreVerbatim()
    {
        Assert.Equal("Newtonian", PhysicsModels.DisplayName(PhysicsModel.Newtonian));
        Assert.Equal("Special relativity", PhysicsModels.DisplayName(PhysicsModel.SpecialRelativity));
        Assert.Equal("General relativity", PhysicsModels.DisplayName(PhysicsModel.GeneralRelativity));
    }

    [Fact]
    public void Coupling_OnlyGrDependsOnSpeed()
    {
        Assert.Equal(1.0, PhysicsModels.Coupling(PhysicsModel.Newtonian, 0.6));
        Assert.Equal(1.0, PhysicsModels.Coupling(PhysicsModel.SpecialRelativity, 0.6));
        AssertClose(2.125, PhysicsModels.Coupling(PhysicsModel.GeneralRelativity, 0.6));
    }

    [Fact]
    public void Galilean_SubtractsAndNeverContracts()
    {
        AssertClose(1.2, FrameTransform.Galilean.VelocityInFrame(0.6, -0.6));
        Assert.Equal(1.0, FrameTransform.Galilean.Gamma(1.2));
    }

    [Fact]
    public void Galilean_HandlesSpeedsAboveOne()
    {
        AssertClose(1.98, FrameTransform.Galilean.VelocityInFrame(0.99, -0.99));
        Assert.Equal(1.0, FrameTransform.Galilean.Gamma(1.98));
    }

    [Fact]
    public void Lorentz_MatchesRelativity()
    {
        AssertClose(15.0 / 17, FrameTransform.Lorentz.VelocityInFrame(0.6, -0.6));
        AssertClose(17.0 / 8, FrameTransform.Lorentz.Gamma(15.0 / 17));
    }

    [Fact]
    public void Frames_NewtonianIsGalileanOthersLorentz()
    {
        Assert.Same(FrameTransform.Galilean, PhysicsModels.Frames(PhysicsModel.Newtonian));
        Assert.Same(FrameTransform.Lorentz, PhysicsModels.Frames(PhysicsModel.SpecialRelativity));
        Assert.Same(FrameTransform.Lorentz, PhysicsModels.Frames(PhysicsModel.GeneralRelativity));
    }

    [Fact]
    public void RelativeSpeed_NewtonianIsDifference()
    {
        AssertClose(1.2, PhysicsModels.RelativeSpeed(PhysicsModel.Newtonian, 0.6, -0.6));
        AssertClose(15.0 / 17, PhysicsModels.RelativeSpeed(PhysicsModel.SpecialRelativity, 0.6, -0.6));
        AssertClose(15.0 / 17, PhysicsModels.RelativeSpeed(PhysicsModel.GeneralRelativity, 0.6, -0.6));
    }
}
