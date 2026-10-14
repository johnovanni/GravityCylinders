using System.Globalization;

namespace GravityCylinders.Tests;

internal static class TestHelpers
{
    public static void AssertClose(double expected, double actual, double relativeTolerance = 1e-9, double absoluteTolerance = 1e-12)
    {
        double diff = Math.Abs(expected - actual);
        double allowed = Math.Max(absoluteTolerance, relativeTolerance * Math.Abs(expected));
        Assert.True(diff <= allowed, $"Expected {expected:R} but got {actual:R} (diff {diff:R}, allowed {allowed:R})");
    }

    public static void AssertClose(Vector3D expected, Vector3D actual, double absoluteTolerance = 1e-9)
    {
        Assert.True((expected - actual).Length <= absoluteTolerance, $"Expected {expected} but got {actual}");
    }

    public static void AssertClose(Point3D expected, Point3D actual, double absoluteTolerance = 1e-9)
    {
        Assert.True((expected - actual).Length <= absoluteTolerance, $"Expected {expected} but got {actual}");
    }
}

/// <summary>Runs a test body with the invariant culture so number formatting is predictable.</summary>
public abstract class InvariantCultureTest : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

    protected InvariantCultureTest() => CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        GC.SuppressFinalize(this);
    }
}
