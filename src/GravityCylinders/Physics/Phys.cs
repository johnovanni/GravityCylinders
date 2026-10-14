using System.Globalization;

namespace GravityCylinders.Physics;

/// <summary>Physical constants (SI). One scene unit is one metre.</summary>
public static class Phys
{
    public const double C = 299_792_458.0;   // m/s
    public const double G = 6.67430e-11;     // m^3 / (kg s^2)

    public static double Gamma(double beta) => 1.0 / Math.Sqrt(1.0 - beta * beta);

    private static readonly string[] Prefixes = ["y", "z", "a", "f", "p", "n", "µ", "m", "", "k", "M", "G", "T", "P", "E", "Z", "Y"];

    /// <summary>
    /// Formats a value with an SI prefix, e.g. 1.44e9 N -> "1.44 GN". A value that rounds up to 1000 moves to the next
    /// prefix ("999.7 N" -> "1 kN"); values beyond the prefixes (y … Y) use scientific notation ("8.05e43 N").
    /// </summary>
    public static string Eng(double value, string unit, int digits = 3)
    {
        if (double.IsNaN(value)) return "—";
        if (value == 0) return $"0 {unit}";
        string fmt = "G" + digits;
        int exp3 = (int)Math.Floor(Math.Log10(Math.Abs(value)) / 3.0);
        if (Math.Abs(RoundTo(value / Math.Pow(1000, exp3), digits)) >= 1000) exp3++;
        if (exp3 < -8 || exp3 > 8) return $"{Scientific(value, digits)} {unit}";
        return $"{(value / Math.Pow(1000, exp3)).ToString(fmt)} {Prefixes[exp3 + 8]}{unit}";
    }

    private static string Scientific(double value, int digits)
    {
        int exp = (int)Math.Floor(Math.Log10(Math.Abs(value)));
        if (Math.Abs(RoundTo(value / Math.Pow(10, exp), digits)) >= 10) exp++;
        return $"{(value / Math.Pow(10, exp)).ToString("G" + digits)}e{exp}";
    }

    private static double RoundTo(double value, int significantDigits) =>
        double.Parse(value.ToString("G" + significantDigits, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
