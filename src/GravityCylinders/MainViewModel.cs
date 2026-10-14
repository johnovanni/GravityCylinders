using System.Collections.ObjectModel;
using GravityCylinders.Physics;
using GravityCylinders.Presentation;

namespace GravityCylinders;

public sealed class MainViewModel : ObservableObject
{
    private static readonly (string Name, double[] Betas)[] PresetDefinitions =
    [
        ("All co-moving (0.5c)", [0.5, 0.5, 0.5]),
        ("Middle counter-moving", [0.5, -0.5, 0.5]),
        ("Near-light counter-motion", [0.95, -0.95, 0.95]),
        ("Outer at rest, middle moving", [0, 0.8, 0]),
    ];

    /// <summary>Elements per cylinder in the direct metric check; enough for 0.1% at the default L/d = 20.</summary>
    public const int CheckSamples = 1000;

    private PhysicsModel _model = PhysicsModel.GeneralRelativity;
    private FrameChoice _frame = FrameChoice.Lab;
    private int _presetIndex = -1;
    private bool _arrowLengthShowsStrength = true;
    private double _arrowScale = 1, _timeScale = 1;
    private string _validationMessage = "";
    private bool _suspendRecalculation;
    private readonly Func<IReadOnlyList<Cylinder>, int, double, double> _directCheck;
    private readonly SynchronizationContext? _ui;
    private int _checkVersion;

    public MainViewModel()
        : this((cylinders, on, frameBeta) => WeakFieldMetricCheck.NetForceInFrame(cylinders, on, frameBeta, CheckSamples))
    {
    }

    /// <param name="directCheck">Net force on cylinder `on` in the frame moving at `frameBeta`, computed directly from the
    /// metric. It runs on a background thread because it takes tens of milliseconds.</param>
    public MainViewModel(Func<IReadOnlyList<Cylinder>, int, double, double> directCheck)
    {
        _directCheck = directCheck;
        _ui = SynchronizationContext.Current;
        double[] ys = [-3, 0, 3];
        for (int i = 0; i < 3; i++)
        {
            var c = new CylinderViewModel(i, new Cylinder(1e12, 60, 0.4, ys[i], 0), Validate);
            c.Rejected += (_, message) => ValidationMessage = message;
            c.Changed += (_, _) =>
            {
                ValidationMessage = "";
                if (!_suspendRecalculation && _presetIndex != -1)
                {
                    // A manual edit means the scene no longer matches the preset; clearing it lets it be re-applied.
                    _presetIndex = -1;
                    OnPropertyChanged(nameof(PresetIndex));
                }
                Recalculate();
            };
            Cylinders.Add(c);
        }
        Recalculate();
    }

    public event EventHandler? ForcesChanged;

    public ObservableCollection<CylinderViewModel> Cylinders { get; } = [];

    public PhysicsModel[] Models { get; } = Enum.GetValues<PhysicsModel>();
    public FrameChoice[] Frames { get; } = Enum.GetValues<FrameChoice>();
    public string[] Presets { get; } = PresetDefinitions.Select(p => p.Name).ToArray();

    public PhysicsModel SelectedModel
    {
        get => _model;
        set { if (Set(ref _model, value)) Recalculate(); }
    }

    public FrameChoice SelectedFrame
    {
        get => _frame;
        set { if (Set(ref _frame, value)) Recalculate(); }
    }

    public int PresetIndex
    {
        get => _presetIndex;
        set
        {
            if (!Set(ref _presetIndex, value) || value < 0) return;
            _suspendRecalculation = true;
            for (int i = 0; i < Cylinders.Count; i++) Cylinders[i].Beta = PresetDefinitions[value].Betas[i];
            _suspendRecalculation = false;
            Recalculate();
        }
    }

    public bool ArrowLengthShowsStrength { get => _arrowLengthShowsStrength; set => Set(ref _arrowLengthShowsStrength, value); }
    public double ArrowScale { get => _arrowScale; set => Set(ref _arrowScale, value); }
    public double TimeScale { get => _timeScale; set => Set(ref _timeScale, value); }

    public SceneForces Forces { get; private set; } = null!;
    public IReadOnlyList<CylinderRow> Rows { get; private set; } = [];
    public string FrameText { get; private set; } = "";
    public string BadgeText { get; private set; } = "";
    public BadgeLevel BadgeLevel { get; private set; }
    public string CheckText { get; private set; } = "";

    /// <summary>Completes when the latest background metric check has finished (for tests and diagnostics).</summary>
    public Task CheckCompletion { get; private set; } = Task.CompletedTask;
    public string ModelHelpText => HudFormatter.ModelHelp(_model);
    public string TableFootnote => ForceTableBuilder.Footnote;
    public string SimultaneityNote => HudFormatter.SimultaneityNoteFor(_model);

    public string ValidationMessage { get => _validationMessage; private set => Set(ref _validationMessage, value); }

    public IReadOnlyList<Cylinder> CurrentCylinders() => Cylinders.Select(c => c.ToCylinder()).ToList();

    private void Recalculate()
    {
        if (_suspendRecalculation) return;
        var cylinders = CurrentCylinders();
        Forces = ForceSolver.Solve(cylinders, _model, _frame);
        Rows = ForceTableBuilder.Build(Forces);
        FrameText = HudFormatter.Frame(Forces);
        (BadgeText, BadgeLevel) = HudFormatter.Badge(Forces.WeakFieldParameter);
        StartCheck(cylinders);

        foreach (var name in new[] { nameof(Forces), nameof(Rows), nameof(FrameText), nameof(BadgeText), nameof(BadgeLevel),
                     nameof(CheckText), nameof(ModelHelpText), nameof(SimultaneityNote) })
            OnPropertyChanged(name);
        ForcesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Runs the direct metric check in the background; a result is shown only if no newer input arrived.</summary>
    private void StartCheck(IReadOnlyList<Cylinder> cylinders)
    {
        int version = Interlocked.Increment(ref _checkVersion);
        if (_model != PhysicsModel.GeneralRelativity)
        {
            CheckText = HudFormatter.CheckNotApplicable;
            CheckCompletion = Task.CompletedTask;
            return;
        }
        CheckText = HudFormatter.CheckComputing;
        var forces = Forces;
        CheckCompletion = Task.Run(() => CheckLines(cylinders, forces)).ContinueWith(t =>
        {
            if (!t.IsCompletedSuccessfully) return;
            void Publish()
            {
                if (version != Volatile.Read(ref _checkVersion)) return;
                CheckText = t.Result;
                OnPropertyChanged(nameof(CheckText));
            }
            if (_ui != null) _ui.Post(_ => Publish(), null);
            else Publish();
        }, TaskScheduler.Default);
    }

    private string CheckLines(IReadOnlyList<Cylinder> cylinders, SceneForces forces) =>
        string.Join(Environment.NewLine, forces.Cylinders.Select(c => HudFormatter.Check(
            ForceTableBuilder.CylinderName(c.Index),
            c.NetTotal,
            _directCheck(cylinders, c.Index, forces.FrameBeta),
            c.Pairs.Sum(p => Math.Abs(p.TotalInFrame)))));

    /// <summary>Beyond Gλ/c² = 1 a linearised metric is meaningless, so such densities are refused outright.</summary>
    public const double MaxWeakFieldParameter = 1;

    /// <summary>
    /// Refuses densities far outside the weak-field model, keeps cylinder 2 between 1 and 3 and stops neighbouring
    /// cylinders from overlapping.
    /// </summary>
    private string? Validate(CylinderViewModel changed, Cylinder proposed)
    {
        if (Phys.G * proposed.Lambda / (Phys.C * Phys.C) >= MaxWeakFieldParameter)
            return "λ too large: Gλ/c² must stay below 1";
        var all = Cylinders.Select(c => c == changed ? proposed : c.ToCylinder()).ToList();
        if (!(all[0].Y < all[1].Y && all[1].Y < all[2].Y))
            return changed.Index switch
            {
                0 => "Cylinder 1 must stay below cylinder 2",
                1 => "Cylinder 2 must stay between 1 and 3",
                _ => "Cylinder 3 must stay above cylinder 2",
            };
        for (int i = 0; i < 2; i++)
            if (all[i + 1].Y - all[i].Y <= all[i].Radius + all[i + 1].Radius)
                return $"Cylinders {i + 1} and {i + 2} would overlap";
        return null;
    }
}
