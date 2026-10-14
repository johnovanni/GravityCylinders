using GravityCylinders.Physics;

namespace GravityCylinders.Tests.Physics;

public class PhysTests : InvariantCultureTest
{
    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(0.6, 1.25)]
    [InlineData(0.8, 5.0 / 3.0)]
    public void Gamma_MatchesLorentzFactor(double beta, double expected) =>
        TestHelpers.AssertClose(expected, Phys.Gamma(beta));

    [Theory]
    [InlineData(1.44e9, "N", "1.44 GN")]
    [InlineData(0.001, "N/m", "1 mN/m")]
    [InlineData(-2.5e-12, "N", "-2.5 pN")]
    [InlineData(0.0, "N", "0 N")]
    [InlineData(double.NaN, "N", "—")]
    public void Eng_UsesSiPrefixes(double value, string unit, string expected) =>
        Assert.Equal(expected, Phys.Eng(value, unit));

    [Fact]
    public void Eng_ClampsToLargestPrefix() =>
        Assert.EndsWith("YN", Phys.Eng(1e30, "N"));

    [Fact]
    public void GravitationalConstant_IsCodata2018() => Assert.Equal(6.67430e-11, Phys.G);
}
