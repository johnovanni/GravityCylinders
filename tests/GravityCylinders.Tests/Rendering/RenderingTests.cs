using GravityCylinders.Physics;
using GravityCylinders.Rendering;
using GravityCylinders.Visualization;
using static GravityCylinders.Tests.TestHelpers;

namespace GravityCylinders.Tests.Rendering;

public class RenderingTests
{
    private static Cylinder[] Scene(double b0, double b1, double b2) =>
    [
        new(1e12, 60, 0.4, -3, b0),
        new(1e12, 60, 0.4, 0, b1),
        new(1e12, 60, 0.4, 3, b2),
    ];

    private static Rect3D Bounds(Model3D model) => model.Bounds;

    [Fact]
    public void Disc_IsFlatFanAtCentre()
    {
        var mb = new MeshBuilder();
        mb.AddDisc(new Point3D(2, 0, 0), new Vector3D(1, 0, 0), 0.5, 12);
        var mesh = mb.ToMesh();
        Assert.Equal(13, mesh.Positions.Count);
        Assert.Equal(12 * 3, mesh.TriangleIndices.Count);
        Assert.All(mesh.Positions, p => Assert.Equal(2.0, p.X, 9));
    }

    [Fact]
    public void CylinderLength_FollowsFrame()
    {
        var scene = Scene(0.6, 0, 0);
        var group = CylinderRenderer.Build(ForceSolver.Solve(scene, PhysicsModel.GeneralRelativity, FrameChoice.Lab), scene);
        Assert.Equal(3, group.Children.Count);
        AssertClose(48, Bounds(group.Children[0]).SizeX);
        AssertClose(60, Bounds(group.Children[1]).SizeX);
        AssertClose(-3, Bounds(group.Children[0]).Y + Bounds(group.Children[0]).SizeY / 2);
    }

    [Fact]
    public void Newtonian_NoContraction()
    {
        var scene = Scene(0.6, 0, 0);
        var group = CylinderRenderer.Build(ForceSolver.Solve(scene, PhysicsModel.Newtonian, FrameChoice.Lab), scene);
        AssertClose(60, Bounds(group.Children[0]).SizeX);
    }

    [Fact]
    public void ArrowPointsTowardSource()
    {
        var f = ForceSolver.Solve(Scene(0, 0, 0), PhysicsModel.GeneralRelativity, FrameChoice.Lab);
        Assert.True(ForceArrowRenderer.NetVector(f, 0, true, 1).Y > 0);
        Assert.True(ForceArrowRenderer.NetVector(f, 2, true, 1).Y < 0);
        Assert.True(ForceArrowRenderer.PairVector(f, 1, 0, true, 1).Y < 0);
    }

    [Fact]
    public void ZeroNet_NoNetArrow()
    {
        var f = ForceSolver.Solve(Scene(0.6, 0, -0.6), PhysicsModel.GeneralRelativity, FrameChoice.Lab);
        Assert.Equal(0.0, ForceArrowRenderer.NetVector(f, 1, true, 1).Length);
        Assert.True(ForceArrowRenderer.PairVector(f, 1, 0, true, 1).Length > 0);
    }

    [Fact]
    public void Normalized_LargestArrowIsFullLength()
    {
        var f = ForceSolver.Solve(Scene(0.3, 0, -0.8), PhysicsModel.GeneralRelativity, FrameChoice.Lab);
        double full = ForceArrowRenderer.CellSize * ArrowScaling.CellFill;
        Assert.All(new[] { 0, 2 }, i => AssertClose(full, ForceArrowRenderer.NetVector(f, i, false, 1).Length));
    }

    [Fact]
    public void ExtremeForces_ArrowsCapped()
    {
        var f = ForceSolver.Solve(Scene(0.99, -0.99, 0.99), PhysicsModel.GeneralRelativity, FrameChoice.Lab);
        double cap = ForceArrowRenderer.CellSize * ArrowScaling.CellFill * ArrowScaling.MaxRelativeLength * 2 + 1e-9;
        for (int i = 0; i < 3; i++)
        {
            Assert.True(ForceArrowRenderer.NetVector(f, i, true, 2).Length <= cap);
            foreach (var p in f.Cylinders[i].Pairs)
                Assert.True(ForceArrowRenderer.PairVector(f, i, p.Source, true, 2).Length <= cap);
        }
    }

    [Fact]
    public void ArrowGroup_HasModelsForEachCylinder()
    {
        var scene = Scene(0, 0, 0);
        var group = ForceArrowRenderer.Build(ForceSolver.Solve(scene, PhysicsModel.Newtonian, FrameChoice.Lab), scene, true, 1);
        Assert.NotEmpty(group.Children);
    }

    [Fact]
    public void Rings_StandStillForSelectedFrame()
    {
        var scene = Scene(0.5, -0.5, 0.2);
        var f = ForceSolver.Solve(scene, PhysicsModel.SpecialRelativity, FrameChoice.Cylinder1);
        var rings = new RingMarkerRenderer();
        double[] before = rings.Phases.ToArray();
        rings.Advance(f, 0.1, 1);   // 1 s would move cylinder 2 exactly 4 spacings and wrap to the same phase
        Assert.Equal(before[0], rings.Phases[0]);
        Assert.NotEqual(before[1], rings.Phases[1]);
        Assert.NotEqual(before[2], rings.Phases[2]);
    }

    [Fact]
    public void Rings_StayWithinDrawnLength()
    {
        var scene = Scene(0.8, 0, 0);
        var f = ForceSolver.Solve(scene, PhysicsModel.GeneralRelativity, FrameChoice.Lab);
        var rings = new RingMarkerRenderer();
        rings.Advance(f, 0.37, 1);
        var group = rings.Build(f, scene);
        var bounds = group.Bounds;
        Assert.True(bounds.X >= -30.0 - 1e-9 && bounds.X + bounds.SizeX <= 30.0 + 1e-9);
    }

    [Theory]
    [InlineData(PhysicsModel.Newtonian)]
    [InlineData(PhysicsModel.GeneralRelativity)]
    public void Rings_MoveAtBetaTimesSceneSpeed(PhysicsModel model)
    {
        // Final review Important #2: drawn ring speed must be β·SceneSpeedAtC in the frame, not β·S/γ.
        var scene = Scene(0.95, 0, 0);
        var f = ForceSolver.Solve(scene, model, FrameChoice.Lab);
        var rings = new RingMarkerRenderer();
        double before = rings.RingPositions(f, 0).First(x => x > 0);
        double dt = 0.01;
        rings.Advance(f, dt, 1);
        double after = rings.RingPositions(f, 0).First(x => x > before);
        AssertClose(0.95 * RingMarkerRenderer.SceneSpeedAtC * dt, after - before, 1e-9);
    }

    [Fact]
    public void PairArrows_StayOnShortCylinders()
    {
        Cylinder[] scene = [new(1e12, 5, 0.4, -3, 0), new(1e12, 60, 0.4, 0, 0), new(1e12, 60, 0.4, 3, 0)];
        var f = ForceSolver.Solve(scene, PhysicsModel.GeneralRelativity, FrameChoice.Lab);
        Assert.Equal(-2.0, ForceArrowRenderer.PairArrowX(f, 0, 0), 12);   // 0.4 × 5 m
        Assert.Equal(2.0, ForceArrowRenderer.PairArrowX(f, 0, 1), 12);
        Assert.Equal(-ForceArrowRenderer.PairOffsetX, ForceArrowRenderer.PairArrowX(f, 1, 0), 12);
    }
}
