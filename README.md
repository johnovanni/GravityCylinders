# Gravity Cylinders (WPF, .NET 10)

Interactive 3D view of the gravitational force between three long, parallel cylinders of mass moving along their axes,
under Newtonian, special-relativistic and weak-field general-relativistic models, seen from the lab or any cylinder's
frame.

```
dotnet run --project src/GravityCylinders
dotnet test
```

## Layout
```
src/GravityCylinders/
  Physics/         Phys, Cylinder, Relativity, FiniteSegment, PhysicsModel, FrameTransform, ForceSolver,
                   WeakFieldMetricCheck
  Visualization/   ArrowScaling, FrameTimer                         — no WPF UI
  Presentation/    ForceTableBuilder, HudFormatter
  Rendering/       MeshBuilder, OrbitCamera, SceneStyle, CylinderRenderer, RingMarkerRenderer, ForceArrowRenderer
  Input/           ViewportMouseController (rotate / pan / zoom)
  MainViewModel    UI state, presets;  MainWindow: wiring + per-frame loop only
tests/GravityCylinders.Tests/   xUnit tests
```

## Controls
- **Left-drag** rotate · **Right/middle-drag** pan · **Wheel** zoom
