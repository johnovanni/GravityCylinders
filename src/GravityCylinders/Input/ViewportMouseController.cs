using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using GravityCylinders.Rendering;

namespace GravityCylinders.Input;

/// <summary>Mouse handling for the 3D view: left-drag rotates, right/middle-drag pans and the wheel zooms.</summary>
public sealed class ViewportMouseController
{
    private enum DragMode { None, Rotate, Pan }

    private readonly FrameworkElement _host;
    private readonly OrbitCamera _orbit;
    private readonly PerspectiveCamera _camera;
    private DragMode _drag;
    private Point _last;

    public ViewportMouseController(FrameworkElement host, OrbitCamera orbit, PerspectiveCamera camera)
    {
        _host = host;
        _orbit = orbit;
        _camera = camera;
        host.MouseDown += OnMouseDown;
        host.MouseMove += OnMouseMove;
        host.MouseUp += OnMouseUp;
        host.MouseWheel += (_, e) => _orbit.Zoom(e.Delta);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _host.Focus();
        _last = e.GetPosition(_host);
        _drag = e.ChangedButton switch
        {
            MouseButton.Left => DragMode.Rotate,
            MouseButton.Right or MouseButton.Middle => DragMode.Pan,
            _ => DragMode.None,
        };
        if (_drag != DragMode.None) _host.CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_drag == DragMode.None) return;
        var p = e.GetPosition(_host);
        double dx = p.X - _last.X, dy = p.Y - _last.Y;
        _last = p;

        if (_drag == DragMode.Rotate) _orbit.Rotate(dx, dy);
        else _orbit.Pan(dx, dy, _host.ActualWidth, _camera.FieldOfView);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _drag = DragMode.None;
        _host.ReleaseMouseCapture();
    }
}
