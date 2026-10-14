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
- **Model** list: Newtonian / Special relativity / General relativity. **Frame** list: Lab / Cylinder 1 / 2 / 3.
- Per cylinder: mass per metre λ, proper length, radius, y position (cylinder 2 always stays in the middle) and lab speed β.
- Ring markers slide with each cylinder at its speed in the current frame, so the chosen cylinder's rings stand still.

## Physics (SI, weak field: Gλ/c² ≪ 1)
- Newtonian: pull per metre 2Gλᵢλⱼ/d, Galilean frames; totals use the exact finite-segment formula.
- Special relativity: Newton's pull in each cylinder's rest frame plus relativistic kinematics (velocity addition,
  contraction, time dilation). Not a self-consistent theory of gravity — it isolates what kinematics alone does.
- General relativity (weak field, exact in speed): in a cylinder's rest frame the pull of a neighbour moving at relative
  speed w is multiplied by γ²(1 + w²) — from h_μν = −(2Φ/c²)(η_μν + 2u_μu_ν) and the geodesic equation. Co-moving
  cylinders feel exactly Newton's force.
- Frames: each element's force in frame k is its rest-frame force ÷ γ (time dilation); per metre of frame k it is the
  same in every frame. Totals use the overlap in frame k, so they differ between frames (relativity of simultaneity).
- Check: in GR the app also builds each moving source's metric directly in the chosen frame, differentiates it
  numerically and integrates the geodesic force — the HUD shows the agreement.
