using GravityCylinders.Physics;
using static GravityCylinders.Tests.TestHelpers;

namespace GravityCylinders.Tests.Physics;

public class WeakFieldMetricCheckTests
{
    private static Cylinder[] Scene(double b0, double b1, double b2) =>
    [
        new(1e12, 3000, 0.1, -3, b0),
        new(1e12, 3000, 0.1, 0, b1),
        new(1e12, 3000, 0.1, 3, b2),
    ];

    [Fact]
    public void Metric_StaticSourceIsIsotropicNewtonian()
    {
        var source = new Cylinder(1e12, 50, 0.1, 0, 0);
        var h = WeakFieldMetricCheck.Metric(source, 0, 0, 1.5, 2, 0);
        double expected = -2 * FiniteSegment.Potential(1e12, 50, 1.5, 2) / (Phys.C * Phys.C);
        AssertClose(expected, h[0, 0], 1e-12);
        AssertClose(expected, h[1, 1], 1e-12);
        AssertClose(expected, h[2, 2], 1e-12);
        AssertClose(expected, h[3, 3], 1e-12);
        for (int a = 0; a < 4; a++)
            for (int b = 0; b < 4; b++)
                if (a != b) Assert.Equal(0.0, h[a, b]);
    }

    [Fact]
    public void Metric_MovingSourceHasMomentumTerms()
    {
        var source = new Cylinder(1e12, 50, 0.1, 0, 0.6);
        var h = WeakFieldMetricCheck.Metric(source, 0, 0, 0, 2, 0);
        Assert.True(h[0, 1] != 0);
        AssertClose(h[0, 1], h[1, 0], 1e-15);
    }

    [Fact]
    public void Check_AgreesWithSolver_AllFrames()
    {
        var scene = Scene(0.6, 0, -0.5);
        foreach (var frame in Enum.GetValues<FrameChoice>())
        {
            var solved = ForceSolver.Solve(scene, PhysicsModel.GeneralRelativity, frame);
            for (int i = 0; i < scene.Length; i++)
            {
                double direct = WeakFieldMetricCheck.NetForceInFrame(scene, i, solved.FrameBeta);
                AssertClose(solved.Cylinders[i].NetTotal, direct, 2e-3);
            }
        }
    }

    [Fact]
    public void Check_AtRest_IsNewtonian()
    {
        var scene = Scene(0, 0, 0);
        var solved = ForceSolver.Solve(scene, PhysicsModel.Newtonian, FrameChoice.Lab);
        AssertClose(solved.Cylinders[0].NetTotal, WeakFieldMetricCheck.NetForceInFrame(scene, 0, 0), 2e-3);
    }
}
