using GravityCylinders.Physics;
using static GravityCylinders.Tests.TestHelpers;

namespace GravityCylinders.Tests.Physics;

public class RelativityTests
{
    [Fact]
    public void InFrame_AddsVelocitiesRelativistically() => AssertClose(15.0 / 17, Relativity.InFrame(0.6, -0.6));

    [Fact]
    public void InFrame_GammaOfComposedSpeed() => AssertClose(17.0 / 8, Phys.Gamma(Relativity.InFrame(0.6, -0.6)));

    [Fact]
    public void RelativeSpeed_IsSymmetricAndPositive()
    {
        AssertClose(15.0 / 17, Relativity.RelativeSpeed(-0.6, 0.6));
        AssertClose(15.0 / 17, Relativity.RelativeSpeed(0.6, -0.6));
    }

    [Fact]
    public void CouplingGR_AtSixTenths() => AssertClose(2.125, Relativity.CouplingGR(0.6));

    [Fact]
    public void CouplingGR_SmallSpeedExcessIsTwoWSquared() => AssertClose(2e-4, Relativity.CouplingGR(0.01) - 1, 1e-3);

    [Fact]
    public void CouplingGR_AtRestIsOne() => Assert.Equal(1.0, Relativity.CouplingGR(0));
}
