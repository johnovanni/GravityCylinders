using System.Globalization;
using System.Runtime.CompilerServices;
using GravityCylinders.Physics;

namespace GravityCylinders;

/// <summary>
/// Editable inputs for one cylinder. Each change is offered to a validator first; a rejected value is reverted
/// (the binding is refreshed so the text box shows the old value again) and <see cref="Rejected"/> is raised.
/// Text boxes bind to the <c>…Text</c> properties so that text that is not a number is rejected with a message too.
/// </summary>
public sealed class CylinderViewModel : ObservableObject
{
    public const double MaxBeta = 0.99;
    private const string NotANumber = "Please enter a number";

    private readonly Func<CylinderViewModel, Cylinder, string?> _validate;
    private double _lambda, _length, _radius, _y, _beta;

    public CylinderViewModel(int index, Cylinder initial, Func<CylinderViewModel, Cylinder, string?> validate)
    {
        Index = index;
        _validate = validate;
        (_lambda, _length, _radius, _y, _beta) = (initial.Lambda, initial.Length, initial.Radius, initial.Y, initial.Beta);
    }

    public event EventHandler<string>? Rejected;

    /// <summary>Raised after a value has been accepted (not when a rejected value is reverted).</summary>
    public event EventHandler? Changed;

    public int Index { get; }
    public string Name => $"Cylinder {Index + 1}";

    public double Lambda { get => _lambda; set => TrySet(ref _lambda, value, ToCylinder() with { Lambda = value }, "Mass per metre must be positive"); }
    public double Length { get => _length; set => TrySet(ref _length, value, ToCylinder() with { Length = value }, "Length must be positive"); }
    public double Radius { get => _radius; set => TrySet(ref _radius, value, ToCylinder() with { Radius = value }, "Radius must be positive"); }
    public double Y { get => _y; set => TrySet(ref _y, value, ToCylinder() with { Y = value }, null); }

    public double Beta
    {
        get => _beta;
        set
        {
            if (!double.IsFinite(value)) { Reject(nameof(Beta), NotANumber); return; }
            double clamped = Math.Clamp(value, -MaxBeta, MaxBeta);
            TrySet(ref _beta, clamped, ToCylinder() with { Beta = clamped }, null);
            if (clamped != value) RaiseValueChanged(nameof(Beta));
        }
    }

    public string LambdaText { get => _lambda.ToString("G4"); set => SetFromText(value, v => Lambda = v); }
    public string LengthText { get => _length.ToString("G6"); set => SetFromText(value, v => Length = v); }
    public string RadiusText { get => _radius.ToString("G6"); set => SetFromText(value, v => Radius = v); }
    public string YText { get => _y.ToString("G6"); set => SetFromText(value, v => Y = v); }
    public string BetaText { get => _beta.ToString("0.######"); set => SetFromText(value, v => Beta = v); }

    public Cylinder ToCylinder() => new(_lambda, _length, _radius, _y, _beta);

    private void SetFromText(string text, Action<double> apply, [CallerMemberName] string? textProperty = null)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double value)) apply(value);
        else Reject(textProperty![..^"Text".Length], NotANumber);
    }

    private void TrySet(ref double field, double value, Cylinder proposed, string? positiveMessage,
        [CallerMemberName] string? name = null)
    {
        string? error = !double.IsFinite(value) ? NotANumber
            : positiveMessage != null && value <= 0 ? positiveMessage
            : _validate(this, proposed);
        if (error != null)
        {
            Reject(name!, error);
            return;
        }
        if (Set(ref field, value, name))
        {
            OnPropertyChanged(name + "Text");
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Reject(string property, string message)
    {
        RaiseValueChanged(property);
        Rejected?.Invoke(this, message);
    }

    /// <summary>Refreshes both the number and its text so bound controls show the current value.</summary>
    private void RaiseValueChanged(string property)
    {
        OnPropertyChanged(property);
        OnPropertyChanged(property + "Text");
    }
}
