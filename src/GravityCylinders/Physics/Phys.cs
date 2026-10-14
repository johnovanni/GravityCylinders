namespace GravityCylinders.Physics;

/// <summary>Physical constants (SI). One scene unit is one metre.</summary>
public static class Phys
{
    public const double C = 299_792_458.0;   // m/s
    public const double G = 6.67430e-11;     // m^3 / (kg s^2)

    public static double Gamma(double beta) => 1.0 / Math.Sqrt(1.0 - beta * beta);

    private static readonly string[] Prefixes = ["y", "z", "a", "f", "p", "n", "µ", "m", "", "k", "M", "G", "T", "P", "E", "Z", "Y"];

    /// <summary>Formats a value with an SI prefix, e.g. 1.44e9 N -> "1.44 GN".</summary>
    public static string Eng(double value, string unit, int digits = 3)
    {
        if (double.IsNaN(value)) return "—";
        if (value == 0) return $"0 {unit}";
        int exp3 = (int)Math.Floor(Math.Log10(Math.Abs(value)) / 3.0);
        exp3 = Math.Clamp(exp3, -8, 8);
        double scaled = value / Math.Pow(1000, exp3);
        string fmt = "G" + digits;
        return $"{scaled.ToString(fmt)} {Prefixes[exp3 + 8]}{unit}";
    }
}
