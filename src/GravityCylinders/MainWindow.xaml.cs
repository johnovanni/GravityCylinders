using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using GravityCylinders.Input;
using GravityCylinders.Physics;
using GravityCylinders.Presentation;
using GravityCylinders.Rendering;
using GravityCylinders.Visualization;

namespace GravityCylinders;

/// <summary>Wiring and the per-frame loop only; all state lives in <see cref="MainViewModel"/>.</summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private readonly OrbitCamera _orbit = new();
    private readonly FrameTimer _timer = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly RingMarkerRenderer _rings = new();
    private readonly ModelVisual3D _cylinderVisual = new(), _ringVisual = new(), _arrowVisual = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        var lights = new Model3DGroup();
        SceneStyle.AddLights(lights);
        Viewport.Children.Add(new ModelVisual3D { Content = lights });
        Viewport.Children.Add(_cylinderVisual);
        Viewport.Children.Add(_ringVisual);
        Viewport.Children.Add(_arrowVisual);

        _ = new ViewportMouseController(ViewportHost, _orbit, Camera);
        _vm.ForcesChanged += (_, _) => RebuildScene();
        _vm.PropertyChanged += OnViewModelChanged;
        RebuildScene();
        CompositionTarget.Rendering += OnRendering;
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.ArrowScale) or nameof(MainViewModel.ArrowLengthShowsStrength))
            RebuildArrows();
    }

    private void RebuildScene()
    {
        var cylinders = _vm.CurrentCylinders();
        _cylinderVisual.Content = CylinderRenderer.Build(_vm.Forces, cylinders);
        RebuildArrows();
        Badge.Foreground = new SolidColorBrush(_vm.BadgeLevel switch
        {
            BadgeLevel.Green => Color.FromRgb(0x6E, 0xE0, 0x8A),
            BadgeLevel.Amber => Color.FromRgb(0xFF, 0xB0, 0x20),
            _ => Color.FromRgb(0xFF, 0x50, 0x50),
        });
    }

    private void RebuildArrows() =>
        _arrowVisual.Content = ForceArrowRenderer.Build(_vm.Forces, _vm.CurrentCylinders(), _vm.ArrowLengthShowsStrength, _vm.ArrowScale);

    private void OnRendering(object? sender, EventArgs e)
    {
        double dt = _timer.Tick(_clock.Elapsed.TotalSeconds);
        _rings.Advance(_vm.Forces, dt, _vm.TimeScale);
        _ringVisual.Content = _rings.Build(_vm.Forces, _vm.CurrentCylinders());
        _orbit.Apply(Camera);
    }
}

/// <summary>Shows the display name of a <see cref="PhysicsModel"/> or <see cref="FrameChoice"/>.</summary>
public sealed class DisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        PhysicsModel m => PhysicsModels.DisplayName(m),
        FrameChoice f => FrameChoices.DisplayName(f),
        _ => value?.ToString() ?? "",
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
