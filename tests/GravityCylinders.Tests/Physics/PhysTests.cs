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

    [Theory]
    [InlineData(999.7, "1 kN")]
    [InlineData(999.4, "999 N")]
    [InlineData(-999_700.0, "-1 MN")]
    [InlineData(0.0009997, "1 mN")]
    public void Eng_RollsOverToTheNextPrefix(double value, string expected) =>
        Assert.Equal(expected, Phys.Eng(value, "N"));

    [Theory]
    [InlineData(8.05e43, "8.05e43 N")]
    [InlineData(1e30, "1e30 N")]
    [InlineData(-2e-30, "-2e-30 N")]
    public void Eng_BeyondThePrefixesUsesScientificNotation(double value, string expected) =>
        Assert.Equal(expected, Phys.Eng(value, "N"));

    [Fact]
    public void GravitationalConstant_IsCodata2018() => Assert.Equal(6.67430e-11, Phys.G);
}
