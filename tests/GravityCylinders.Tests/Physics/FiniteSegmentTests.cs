using GravityCylinders.Physics;
using static GravityCylinders.Tests.TestHelpers;

namespace GravityCylinders.Tests.Physics;

public class FiniteSegmentTests
{
    /// <summary>Transverse force between two centred parallel unit-density segments by a midpoint double sum.</summary>
    private static double NumericSegmentForce(double a, double b, double d, int n)
    {
        double sum = 0, da = a / n, db = b / n;
        for (int i = 0; i < n; i++)
        {
            double x = -a / 2 + (i + 0.5) * da;
            for (int j = 0; j < n; j++)
            {
                double s = x - (-b / 2 + (j + 0.5) * db);
                sum += d / Math.Pow(s * s + d * d, 1.5) * da * db;
            }
        }
        return Phys.G * sum;
    }

    private static double FiniteDiffRho(double lambda, double length, double x, double rho, double h = 1e-5) =>
        (FiniteSegment.Potential(lambda, length, x, rho + h) - FiniteSegment.Potential(lambda, length, x, rho - h)) / (2 * h);

    [Fact]
    public void Force_LongSegmentsTendToLineFormula() =>
        AssertClose(2 * Phys.G * 1e4, FiniteSegment.Force(1, 1e4, 1, 1e4, 1), 2e-4);

    [Fact]
    public void Force_ShortSegmentsMatchNumericDoubleIntegral() =>
        AssertClose(NumericSegmentForce(2, 3, 1, 400), FiniteSegment.Force(1, 2, 1, 3, 1), 1e-4);

    [Fact]
    public void Force_IsSymmetricUnderSwap() =>
        Assert.Equal(FiniteSegment.Force(2, 600, 3, 60, 3), FiniteSegment.Force(3, 60, 2, 600, 3), 12);

    [Fact]
    public void Force_UnequalLongLengthsUseShorterOverlap() =>
        AssertClose(2 * Phys.G * 6 * 60 / 3, FiniteSegment.Force(2, 6000, 3, 60, 3), 0.06);

    [Fact]
    public void RadialDerivative_MatchesPotentialFiniteDifference() =>
        AssertClose(FiniteDiffRho(1, 5, 0.3, 1.7), FiniteSegment.RadialDerivative(1, 5, 0.3, 1.7), 1e-6);

    [Fact]
    public void RadialDerivative_LongSegmentCentreIsLineField() =>
        AssertClose(2 * Phys.G / 2.0, FiniteSegment.RadialDerivative(1, 1e6, 0, 2), 1e-9);

    [Fact]
    public void Potential_IsAccurateFarBeyondTheEnd()
    {
        // Far along the axis the segment looks like a point mass: Φ ≈ −G λ B / x.
        AssertClose(-Phys.G * 1 * 2 / 1e4, FiniteSegment.Potential(1, 2, 1e4, 1), 1e-6);
    }
}
