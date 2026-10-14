using GravityCylinders.Physics;
using GravityCylinders.Presentation;

namespace GravityCylinders.Tests;

public class MainViewModelTests : InvariantCultureTest
{
    [Fact]
    public void Defaults_ProduceForces()
    {
        var vm = new MainViewModel();
        Assert.Equal(3, vm.Cylinders.Count);
        Assert.Equal(3, vm.Rows.Count);
        Assert.Equal(BadgeLevel.Green, vm.BadgeLevel);
        Assert.Equal(PhysicsModel.GeneralRelativity, vm.SelectedModel);
        Assert.Equal(FrameChoice.Lab, vm.SelectedFrame);
        Assert.Equal([-3.0, 0.0, 3.0], vm.Cylinders.Select(c => c.Y));
        Assert.All(vm.Cylinders, c => Assert.Equal((1e12, 60.0, 0.4), (c.Lambda, c.Length, c.Radius)));
    }

    [Fact]
    public void ChangingModel_ChangesForces()
    {
        var vm = new MainViewModel { PresetIndex = 1 };
        double gr = vm.Forces.Cylinders[0].NetPerFrameLength;
        int raised = 0;
        vm.ForcesChanged += (_, _) => raised++;
        vm.SelectedModel = PhysicsModel.Newtonian;
        Assert.NotEqual(gr, vm.Forces.Cylinders[0].NetPerFrameLength);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void ChangingFrame_ChangesBetaInFrame()
    {
        var vm = new MainViewModel { PresetIndex = 1 };   // Middle counter-moving
        vm.SelectedFrame = FrameChoice.Cylinder2;
        Assert.Equal(0.0, vm.Forces.Cylinders[1].BetaInFrame, 12);
        Assert.StartsWith("Frame: Cylinder 2", vm.FrameText);
    }

    [Fact]
    public void MiddleCannotLeave()
    {
        var vm = new MainViewModel();
        var before = vm.Forces;
        vm.Cylinders[1].Y = 5;
        Assert.Equal(0.0, vm.Cylinders[1].Y);
        Assert.Equal("Cylinder 2 must stay between 1 and 3", vm.ValidationMessage);
        Assert.Same(before, vm.Forces);
    }

    [Fact]
    public void OverlapRejected()
    {
        var vm = new MainViewModel();
        vm.Cylinders[0].Radius = 3.5;
        Assert.Equal(0.4, vm.Cylinders[0].Radius);
        Assert.Equal("Cylinders 1 and 2 would overlap", vm.ValidationMessage);
    }

    [Fact]
    public void ValidChangeClearsMessage()
    {
        var vm = new MainViewModel();
        vm.Cylinders[1].Y = 5;
        vm.Cylinders[1].Y = 0.5;
        Assert.Equal("", vm.ValidationMessage);
        Assert.Equal(0.5, vm.Cylinders[1].Y);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    public void NonPositiveLambdaRejected(double lambda)
    {
        var vm = new MainViewModel();
        var before = vm.Forces;
        vm.Cylinders[0].Lambda = lambda;
        Assert.Equal(1e12, vm.Cylinders[0].Lambda);
        Assert.NotEqual("", vm.ValidationMessage);
        Assert.Same(before, vm.Forces);
    }

    [Fact]
    public void BetaClamped()
    {
        var vm = new MainViewModel();
        vm.Cylinders[0].Beta = 1.5;
        Assert.Equal(0.99, vm.Cylinders[0].Beta);
        vm.Cylinders[0].Beta = -3;
        Assert.Equal(-0.99, vm.Cylinders[0].Beta);
    }

    [Fact]
    public void PresetKeepsModelAndFrame()
    {
        var vm = new MainViewModel { SelectedModel = PhysicsModel.SpecialRelativity, SelectedFrame = FrameChoice.Cylinder3 };
        vm.PresetIndex = 2;
        Assert.Equal(PhysicsModel.SpecialRelativity, vm.SelectedModel);
        Assert.Equal(FrameChoice.Cylinder3, vm.SelectedFrame);
        Assert.Equal([0.95, -0.95, 0.95], vm.Cylinders.Select(c => c.Beta));
    }

    [Fact]
    public void Presets_AreVerbatim() =>
        Assert.Equal(["All co-moving (0.5c)", "Middle counter-moving", "Near-light counter-motion", "Outer at rest, middle moving"],
            new MainViewModel().Presets);

    [Fact]
    public async Task CheckOnlyInGr()
    {
        var vm = new MainViewModel();
        await vm.CheckCompletion;
        Assert.Contains("direct metric calculation", vm.CheckText);
        vm.SelectedModel = PhysicsModel.Newtonian;
        Assert.Equal(HudFormatter.CheckNotApplicable, vm.CheckText);
    }

    [Fact]
    public void ModelHelpFollowsModel()
    {
        var vm = new MainViewModel { SelectedModel = PhysicsModel.SpecialRelativity };
        Assert.Equal(HudFormatter.ModelHelp(PhysicsModel.SpecialRelativity), vm.ModelHelpText);
    }

    [Fact]
    public void SimultaneityNote_OnlyClaimsFrameDependenceWhenRelativistic()
    {
        var vm = new MainViewModel();
        Assert.Equal(HudFormatter.SimultaneityNote, vm.SimultaneityNote);
        vm.SelectedModel = PhysicsModel.Newtonian;
        Assert.DoesNotContain("simultaneity", vm.SimultaneityNote);
        Assert.Contains("same in every frame", vm.SimultaneityNote);
    }

    [Fact]
    public async Task MetricCheck_RunsOffTheInputThreadAndKeepsOnlyTheLatestResult()
    {
        // Final review Important #1: the ~40 ms direct check must not block input changes (slider drags).
        using var gate = new ManualResetEventSlim(false);
        int calls = 0;
        var vm = new MainViewModel((cylinders, on, frameBeta) =>
        {
            if (Interlocked.Increment(ref calls) <= 3) gate.Wait();   // the first (default-scene) check blocks
            return WeakFieldMetricCheck.NetForceInFrame(cylinders, on, frameBeta, 200);
        });
        var firstRun = vm.CheckCompletion;
        vm.SelectedFrame = FrameChoice.Cylinder1;           // returns although the first check is still blocked
        Assert.Equal(HudFormatter.CheckComputing, vm.CheckText);
        gate.Set();
        await firstRun;
        await vm.CheckCompletion;
        Assert.Contains("direct metric calculation", vm.CheckText);
        Assert.Equal(FrameChoice.Cylinder1, vm.Forces.Frame);
        Assert.Equal(6, calls);                             // both runs completed; the stale one was discarded
    }

    [Fact]
    public void LambdaBeyondWeakFieldRejected()
    {
        var vm = new MainViewModel();
        vm.Cylinders[0].Lambda = 2e27;   // Gλ/c² ≈ 1.5
        Assert.Equal(1e12, vm.Cylinders[0].Lambda);
        Assert.Equal("λ too large: Gλ/c² must stay below 1", vm.ValidationMessage);

        vm.Cylinders[0].Lambda = 1e26;   // Gλ/c² ≈ 0.07: allowed, flagged red
        Assert.Equal(1e26, vm.Cylinders[0].Lambda);
        Assert.Equal(BadgeLevel.Red, vm.BadgeLevel);
    }
}
