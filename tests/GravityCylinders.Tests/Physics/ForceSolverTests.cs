using GravityCylinders.Physics;
using static GravityCylinders.Tests.TestHelpers;

namespace GravityCylinders.Tests.Physics;

public class ForceSolverTests
{
    private const double Lambda = 1e12, Length = 3e5, D = 3;
    private static readonly double LinePerMetre = 2 * Phys.G * Lambda * Lambda / D;

    private static readonly PhysicsModel[] AllModels = Enum.GetValues<PhysicsModel>();
    private static readonly FrameChoice[] AllFrames = Enum.GetValues<FrameChoice>();

    private static Cylinder Cyl(double beta, double y, double lambda = Lambda, double length = Length, double radius = 0.1) =>
        new(lambda, length, radius, y, beta);

    private static Cylinder[] Scene(double b0, double b1, double b2) => [Cyl(b0, -3), Cyl(b1, 0), Cyl(b2, 3)];

    private static PairForce Pair(SceneForces f, int on, int source) => f.Cylinders[on].Pairs.Single(p => p.Source == source);

    [Fact]
    public void AllAtRest_AllModelsAndFramesAgree()
    {
        double expected = FiniteSegment.Force(Lambda, Length, Lambda, Length, D);
        foreach (var model in AllModels)
            foreach (var frame in AllFrames)
            {
                var pair = Pair(ForceSolver.Solve(Scene(0, 0, 0), model, frame), 0, 1);
                AssertClose(expected, pair.TotalInFrame, 1e-12);
            }
    }

    [Fact]
    public void CoMovingGr_PerLengthIsNewtonian()
    {
        var f = ForceSolver.Solve(Scene(0.5, 0.5, 0.5), PhysicsModel.GeneralRelativity, FrameChoice.Lab);
        AssertClose(LinePerMetre, Pair(f, 0, 1).PerFrameLength, 1e-12);
    }

    [Fact]
    public void GrCoupling_InEachFrame()
    {
        Cylinder[] scene = [Cyl(0.6, -3), Cyl(0, 0), Cyl(0, 1e9)];
        foreach (var frame in AllFrames)
            AssertClose(2.125 * LinePerMetre, Pair(ForceSolver.Solve(scene, PhysicsModel.GeneralRelativity, frame), 0, 1).PerFrameLength, 1e-12);

        var inRestFrameOfSource = ForceSolver.Solve(scene, PhysicsModel.GeneralRelativity, FrameChoice.Cylinder2);
        AssertClose(2.125 * LinePerMetre / 1.25, Pair(inRestFrameOfSource, 0, 1).PerProperLengthInFrame, 1e-12);

        var inOwnRestFrame = ForceSolver.Solve(scene, PhysicsModel.GeneralRelativity, FrameChoice.Cylinder1);
        var pair = Pair(inOwnRestFrame, 0, 1);
        AssertClose(0.8 * Length, pair.OverlapInFrame, 1e-12);
        AssertClose(2.125 * LinePerMetre * 0.8 * Length, pair.TotalInFrame, 1e-4);
    }

    [Fact]
    public void SrRestForceIsNewtonianPerLength()
    {
        Cylinder[] scene = [Cyl(0.6, -3), Cyl(0, 0), Cyl(0, 1e9)];
        var f = ForceSolver.Solve(scene, PhysicsModel.SpecialRelativity, FrameChoice.Cylinder1);
        AssertClose(LinePerMetre, Pair(f, 0, 1).PerFrameLength, 1e-12);
    }

    [Fact]
    public void EqualLengths_RestFrameTotalsAreEqualAndOpposite()
    {
        Cylinder[] scene = [Cyl(0.6, -3, lambda: 1e12), Cyl(-0.3, 0, lambda: 4e12), Cyl(0, 1e9)];
        double onFirst = Pair(ForceSolver.Solve(scene, PhysicsModel.GeneralRelativity, FrameChoice.Cylinder1), 0, 1).TotalInFrame;
        double onSecond = Pair(ForceSolver.Solve(scene, PhysicsModel.GeneralRelativity, FrameChoice.Cylinder2), 1, 0).TotalInFrame;
        Assert.True(onFirst > 0);
        AssertClose(-onFirst, onSecond, 1e-12);
    }

    [Fact]
    public void MiddleSymmetric_NetIsExactlyZero()
    {
        var middle = ForceSolver.Solve(Scene(0.6, 0, -0.6), PhysicsModel.GeneralRelativity, FrameChoice.Lab).Cylinders[1];
        Assert.Equal(0.0, middle.NetTotal);
        Assert.Equal(0.0, middle.NetPerFrameLength);
    }

    [Fact]
    public void MiddleAsymmetric_GrNetNonzeroNewtonianZero()
    {
        var gr = ForceSolver.Solve(Scene(0.3, 0, -0.8), PhysicsModel.GeneralRelativity, FrameChoice.Lab).Cylinders[1];
        Assert.True(gr.NetTotal > 0);
        Assert.True(gr.NetPerFrameLength > 0);

        var newton = ForceSolver.Solve(Scene(0.3, 0, -0.8), PhysicsModel.Newtonian, FrameChoice.Lab).Cylinders[1];
        Assert.Equal(0.0, newton.NetTotal);
    }

    [Fact]
    public void ExtremeCounterMotion_StaysFinite()
    {
        foreach (var model in AllModels)
            foreach (var frame in AllFrames)
                foreach (var c in ForceSolver.Solve(Scene(0.99, 0, -0.99), model, frame).Cylinders)
                {
                    Assert.True(double.IsFinite(c.NetTotal) && double.IsFinite(c.NetPerFrameLength));
                    Assert.True(double.IsFinite(c.Gamma) && double.IsFinite(c.LengthInFrame) && double.IsFinite(c.BetaInFrame));
                    Assert.All(c.Pairs, p => Assert.True(double.IsFinite(p.TotalInFrame) && double.IsFinite(p.PerProperLengthInFrame)));
                }
    }

    [Fact]
    public void FrameBeta_IsSelectedCylindersLabSpeed()
    {
        var f = ForceSolver.Solve(Scene(0.3, 0.5, -0.8), PhysicsModel.SpecialRelativity, FrameChoice.Cylinder3);
        Assert.Equal(-0.8, f.FrameBeta);
        Assert.Equal(0.0, f.Cylinders[2].BetaInFrame, 12);
        Assert.Equal(0.0, ForceSolver.Solve(Scene(0.3, 0.5, -0.8), PhysicsModel.SpecialRelativity, FrameChoice.Lab).FrameBeta);
    }

    [Fact]
    public void WeakFieldParameter_IsMaxGLambdaOverC2()
    {
        Cylinder[] scene = [Cyl(0, -3, lambda: 1e12), Cyl(0, 0, lambda: 5e12), Cyl(0, 3, lambda: 2e12)];
        AssertClose(Phys.G * 5e12 / (Phys.C * Phys.C), ForceSolver.Solve(scene, PhysicsModel.Newtonian, FrameChoice.Lab).WeakFieldParameter);
    }
}
