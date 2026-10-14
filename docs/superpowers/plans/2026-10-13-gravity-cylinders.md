# Gravity Cylinders Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A WPF 3D app showing the gravitational force on three parallel, axially moving cylinders under Newtonian,
special-relativistic and weak-field GR models, viewed from the lab or any cylinder's frame.

**Architecture:** Pure physics (`Physics/`) computes per-pair forces from closed-form formulas; a solver assembles
per-cylinder results for a chosen model and frame; an independent finite-difference metric check verifies GR totals.
Presentation turns results into text; the view model owns inputs/validation; renderers and `MainWindow` only draw.
Camera, mesh, arrow-scaling and timer code is copied from `EMFieldVisualizer`.

**Tech Stack:** C# / .NET 10 (`net10.0-windows`), WPF 3D (`Viewport3D`), xUnit 2.9.3.

**Spec:** `docs/superpowers/specs/2026-10-13-gravity-cylinders-design.md` (read §1 before Tasks 2–6).

## Global Constraints

- Repo root `C:\Users\johna\source\repos\GravityCylinders`; projects `src/GravityCylinders` (namespace
  `GravityCylinders`) and `tests/GravityCylinders.Tests`; solution `GravityCylinders.slnx`.
- csproj settings and package versions exactly as in `EMFieldVisualizer` (WinExe, `net10.0-windows`, UseWPF, Nullable,
  ImplicitUsings; Microsoft.NET.Test.Sdk 17.14.1, xunit 2.9.3, xunit.runner.visualstudio 3.1.4).
- SI units throughout; 1 scene unit = 1 m. G = 6.67430e-11, c = 299 792 458 (`Phys.G`, `Phys.C`). Internally
  speeds are β = v/c.
- β inputs clamped to [−0.99, 0.99]; λ, L, R > 0; y₁ < y₂ < y₃; dᵢⱼ > Rᵢ + Rⱼ.
- Weak-field badge thresholds on max Gλ/c²: Green < 1e-3, Amber < 1e-2, Red otherwise.
- Force sign: y-component; the force on i from j points toward j.
- Model names shown in the listbox, verbatim: "Newtonian", "Special relativity", "General relativity". Frame names:
  "Lab", "Cylinder 1", "Cylinder 2", "Cylinder 3".
- No "gravito-electric"/"gravito-magnetic" wording anywhere in UI or code.
- Run all commands from the repo root. Commit after each task with the trailer
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. Extreme counter-motion (β = +0.99 vs −0.99, w ≈ 0.99995, K ≈ 2×10⁴): no NaN/∞, arrows still capped, text
   still formatted — test in Task 5.
2. Galilean frame speeds beyond 1 in Newtonian mode (β = 0.99 seen from a cylinder at −0.99 gives 1.98): must not
   throw, clamp or produce NaN — test in Task 3.
3. Middle cylinder cancellation: symmetric scenes must show net exactly 0 (not ±1e-3 N rounding noise) — snap
   rule and test in Task 5.
4. Cylinders of very different lengths (L₁ = 10 L₂): totals use the overlap, F_seg stays symmetric under swapping
   on/source — test in Task 2.
5. Invalid typed input (negative λ, y that breaks ordering, overlapping radii): value reverts, message shown, forces
   unchanged — test in Task 7.

---

### Task 1: Solution scaffold and copied infrastructure

**Files:**
- Create: `GravityCylinders.slnx`, `README.md`, `src/GravityCylinders/GravityCylinders.csproj`, `App.xaml(.cs)`,
  `MainWindow.xaml(.cs)` (empty dark window with a `Viewport3D`, title "Gravity Cylinders"), `ObservableObject.cs`,
  `Physics/Phys.cs`, `Visualization/ArrowScaling.cs`, `Visualization/FrameTimer.cs`, `Rendering/OrbitCamera.cs`,
  `Rendering/MeshBuilder.cs`, `Rendering/SceneStyle.cs`, `Input/ViewportMouseController.cs`
- Create tests: `tests/GravityCylinders.Tests/GravityCylinders.Tests.csproj`, `TestHelpers.cs`,
  `Physics/PhysTests.cs`, `Visualization/ArrowScalingTests.cs`, `Visualization/FrameTimerTests.cs`,
  `Rendering/OrbitCameraTests.cs`, `Rendering/MeshBuilderTests.cs`

**Interfaces:**
- Produces: everything the EM app exposes in these files, renamed to namespace `GravityCylinders.*`, with these
  changes:
  - `Phys`: keep `C`, `Gamma`, `Eng`; add `public const double G = 6.67430e-11;`; drop K, charge, MeV and
    `MetresPerUnit` (scene unit is 1 m).
  - `SceneStyle`: drop particle/probe/E/B members; add `Cylinder1/2/3` colors (`#FF8A3D`, `#4DA3FF`, `#B57BFF`),
    `ForceNet` (white), `RingMarker` (`#E8E8E8`), `ForMaterial(int index)`; keep `Axis`, `AddLights`, `Model`.
  - `ViewportMouseController(FrameworkElement host, OrbitCamera orbit, PerspectiveCamera camera)`: rotate / pan /
    zoom only; remove probe drag, `ProbeMin/Max`, `DragInViewPlane`.
  - `TestHelpers`: keep `AssertClose` overloads and `InvariantCultureTest`; drop simulation helpers.
  - `OrbitCamera.DefaultDistance = 90` (scene is ~60 m long).

- [ ] **Step 1:** Copy the files from `C:\Users\johna\source\repos\EMFieldVisualizer` with the changes above;
  copy their tests, deleting tests for removed members (`DragInViewPlane_*`, probe/particle style tests).
- [ ] **Step 2:** Run `dotnet build` → `Build succeeded`, 0 warnings from new code.
- [ ] **Step 3:** Run `dotnet test` → all copied tests pass.
- [ ] **Step 4:** Write `README.md` (title, run/test commands, layout block as in spec §3, controls line). Commit
  `Scaffold Gravity Cylinders solution with shared 3D infrastructure`.

### Task 2: Cylinder, relativity helpers, finite-segment gravity

**Files:**
- Create: `src/GravityCylinders/Physics/Cylinder.cs`, `Physics/Relativity.cs`, `Physics/FiniteSegment.cs`
- Test: `tests/GravityCylinders.Tests/Physics/RelativityTests.cs`, `Physics/FiniteSegmentTests.cs`

**Interfaces:**
- Produces:
  - `public sealed record Cylinder(double Lambda, double Length, double Radius, double Y, double Beta);`
  - `Relativity.InFrame(double beta, double frameBeta) => (β − β_f)/(1 − β β_f)`;
    `Relativity.RelativeSpeed(double a, double b) => |InFrame(a, b)|`; `Relativity.CouplingGR(double w) => γ²(1+w²)`.
  - `FiniteSegment.Force(double lambdaOn, double lengthOn, double lambdaSource, double lengthSource, double d)` →
    magnitude (N) of spec §1.1 `F_seg`.
  - `FiniteSegment.Potential(double lambda, double length, double x, double rho)` =
    −Gλ[asinh((x + B/2)/ρ) − asinh((x − B/2)/ρ)] (asinh form avoids cancellation far from the segment).
  - `FiniteSegment.RadialDerivative(double lambda, double length, double x, double rho)` = ∂Φ/∂ρ =
    (Gλ/ρ)[a₊/√(a₊²+ρ²) − a₋/√(a₋²+ρ²)], a± = x ± B/2 (positive: attraction is −∂Φ/∂ρ).

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public void InFrame_AddsVelocitiesRelativistically() => AssertClose(15.0 / 17, Relativity.InFrame(0.6, -0.6));
[Fact] public void InFrame_GammaOfComposedSpeed() => AssertClose(17.0 / 8, Phys.Gamma(Relativity.InFrame(0.6, -0.6)));
[Fact] public void CouplingGR_AtSixTenths() => AssertClose(2.125, Relativity.CouplingGR(0.6));
[Fact] public void CouplingGR_SmallSpeedExcessIsTwoWSquared() => AssertClose(2e-4, Relativity.CouplingGR(0.01) - 1, 1e-3);
[Fact] public void CouplingGR_AtRestIsOne() => Assert.Equal(1.0, Relativity.CouplingGR(0));

[Fact] public void Force_LongSegmentsTendToLineFormula() =>
    AssertClose(2 * Phys.G * 1e4, FiniteSegment.Force(1, 1e4, 1, 1e4, 1), 2e-4);
[Fact] public void Force_ShortSegmentsMatchNumericDoubleIntegral()   // A=2, B=3, d=1; 400×400 midpoint sum of G d/(s²+d²)^{3/2}
    => AssertClose(NumericSegmentForce(2, 3, 1, 400), FiniteSegment.Force(1, 2, 1, 3, 1), 1e-4);
[Fact] public void Force_IsSymmetricUnderSwap() =>                    // review focus 4: L1 = 10 L2
    Assert.Equal(FiniteSegment.Force(2, 600, 3, 60, 3), FiniteSegment.Force(3, 60, 2, 600, 3), 12);
[Fact] public void Force_UnequalLongLengthsUseShorterOverlap() =>
    AssertClose(2 * Phys.G * 6 * 60 / 3, FiniteSegment.Force(2, 6000, 3, 60, 3), 0.06);
[Fact] public void RadialDerivative_MatchesPotentialFiniteDifference()  // x = 0.3, rho = 1.7, L = 5, h = 1e-5
    => AssertClose(FiniteDiffRho(1, 5, 0.3, 1.7), FiniteSegment.RadialDerivative(1, 5, 0.3, 1.7), 1e-6);
[Fact] public void RadialDerivative_LongSegmentCentreIsLineField() =>
    AssertClose(2 * Phys.G / 2.0, FiniteSegment.RadialDerivative(1, 1e6, 0, 2), 1e-9);
```

- [ ] **Step 2:** `dotnet test --filter "FullyQualifiedName~Relativity|FullyQualifiedName~FiniteSegment"` → fails
  to compile (types missing).
- [ ] **Step 3:** Implement the three files per the Interfaces block.
- [ ] **Step 4:** Same command → all pass.
- [ ] **Step 5:** Commit `Add cylinder model, relativity helpers and finite-segment gravity`.

### Task 3: Force models and frame transforms

**Files:**
- Create: `src/GravityCylinders/Physics/PhysicsModel.cs`, `Physics/FrameTransform.cs`
- Test: `tests/GravityCylinders.Tests/Physics/PhysicsModelTests.cs`

**Interfaces:**
- Consumes: `Relativity`, `Phys.Gamma`.
- Produces:
  - `public enum PhysicsModel { Newtonian, SpecialRelativity, GeneralRelativity }`
  - `public static class PhysicsModels { string DisplayName(PhysicsModel m); double Coupling(PhysicsModel m, double w);
    double RelativeSpeed(PhysicsModel m, double betaA, double betaB); FrameTransform Frames(PhysicsModel m); }`
    — Coupling is 1 for Newtonian/SR, `CouplingGR(w)` for GR. RelativeSpeed is `|a − b|` for Newtonian, relativistic
    otherwise.
  - `public abstract class FrameTransform { abstract double VelocityInFrame(double beta, double frameBeta);
    abstract double Gamma(double betaInFrame); static FrameTransform Galilean, Lorentz; }` — Galilean: β − β_f and
    γ ≡ 1; Lorentz: `Relativity.InFrame` and `Phys.Gamma`.

- [ ] **Step 1: Write the failing tests**
  - `DisplayNames_AreVerbatim`: "Newtonian", "Special relativity", "General relativity".
  - `Coupling_OnlyGrDependsOnSpeed`: at w = 0.6 → 1, 1, 2.125.
  - `Galilean_SubtractsAndNeverContracts`: `VelocityInFrame(0.6, -0.6) == 1.2`, `Gamma(1.2) == 1`.
  - `Galilean_HandlesSpeedsAboveOne` (review focus 2): `VelocityInFrame(0.99, -0.99) == 1.98`, Gamma finite = 1.
  - `Lorentz_MatchesRelativity`: `VelocityInFrame(0.6, -0.6) == 15/17`, `Gamma(15/17) == 17/8`.
  - `RelativeSpeed_NewtonianIsDifference`: (0.6, −0.6) → 1.2 Newtonian, 15/17 for SR and GR.
- [ ] **Step 2:** `dotnet test --filter FullyQualifiedName~PhysicsModelTests` → compile failure.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Same command → pass.
- [ ] **Step 5:** Commit `Add physics models and frame transforms`.

### Task 4: Force solver

**Files:**
- Create: `src/GravityCylinders/Physics/ForceSolver.cs`, `Physics/ForceResults.cs`
- Test: `tests/GravityCylinders.Tests/Physics/ForceSolverTests.cs` (helper
  `static Cylinder Cyl(double beta, double y, double lambda = 1e12, double length = 3e5, double radius = 0.1)`)

**Interfaces:**
- Consumes: `Cylinder`, `PhysicsModels`, `FrameTransform`, `FiniteSegment.Force`.
- Produces:
  - `public enum FrameChoice { Lab, Cylinder1, Cylinder2, Cylinder3 }` (+ `FrameChoices.DisplayName`).
  - `public sealed record PairForce(int Source, double NewtonianPerLength, double Coupling, double PerProperLengthInFrame,
    double PerFrameLength, double OverlapInFrame, double TotalInFrame);` — all forces signed (toward source).
    NewtonianPerLength = ±2Gλᵢλⱼ/d; PerFrameLength = Newtonian × Coupling; PerProperLengthInFrame = that / γ_ik;
    TotalInFrame = ±Coupling·F_seg(λᵢ, Lᵢ/γ_ik, λⱼ, Lⱼ/γ_jk, d) (spec §1.4); OverlapInFrame = min(Lᵢ/γ_ik, Lⱼ/γ_jk).
  - `public sealed record CylinderForces(int Index, double BetaInFrame, double Gamma, double LengthInFrame,
    IReadOnlyList<PairForce> Pairs, double NetPerFrameLength, double NetTotal);`
  - `public sealed record SceneForces(PhysicsModel Model, FrameChoice Frame, double FrameBeta,
    IReadOnlyList<CylinderForces> Cylinders, double WeakFieldParameter);` — WeakFieldParameter = max Gλ/c².
  - `ForceSolver.Solve(IReadOnlyList<Cylinder> cylinders, PhysicsModel model, FrameChoice frame) → SceneForces`.
  - Net snapping: if |net| < 1e-12 × Σ|pair| it is set to exactly 0 (both net fields).

- [ ] **Step 1: Write the failing tests** (y = −3, 0, 3 unless stated; indices are 0-based, UI names 1-based)
  - `AllAtRest_AllModelsAndFramesAgree`: β = 0,0,0; for every model × frame, cylinder 0's pair from 1 has
    TotalInFrame equal (rel 1e-12) to `FiniteSegment.Force(1e12, 3e5, 1e12, 3e5, 3)` and positive (toward +y).
  - `CoMovingGr_PerLengthIsNewtonian`: β = 0.5 all, GR, Lab: PerFrameLength = 2G·1e24/3 (rel 1e-12).
  - `GrCoupling_InEachFrame`: cylinders 0 (β 0.6) and 1 (β 0, y 0), cylinder 2 far away (y = 1e9); GR:
    PerFrameLength = 2.125 · 2G·1e24/3 in every frame; frame Cylinder2(index 1): PerProperLengthInFrame of cylinder 0 =
    that / 1.25; frame Cylinder1: TotalInFrame ≈ 2.125·2G·1e24/3 · 0.8·3e5 (rel 1e-4).
  - `SrRestForceIsNewtonianPerLength`: same scene, SR, frame Cylinder1: PerFrameLength = 2G·1e24/3.
  - `EqualLengths_RestFrameTotalsAreEqualAndOpposite`: GR, λ₀ = 1e12, λ₁ = 4e12, β 0.6 / −0.3: pair(0←1).TotalInFrame
    in frame Cylinder1 = −pair(1←0).TotalInFrame in frame Cylinder2 (rel 1e-12).
  - `MiddleSymmetric_NetIsExactlyZero` (review focus 3): β = 0.6, 0, −0.6, GR, Lab → `NetTotal == 0` and
    `NetPerFrameLength == 0` for cylinder 1.
  - `MiddleAsymmetric_GrNetNonzeroNewtonianZero`: β = 0.3, 0, −0.8: GR net on index 1 ≠ 0 and is positive (toward index 2 at
    y = +3: larger w); Newtonian net on 1 == 0.
  - `ExtremeCounterMotion_StaysFinite` (review focus 1): β = 0.99, 0, −0.99, every model × frame: all numbers finite.
  - `WeakFieldParameter_IsMaxGLambdaOverC2`: λ = 1e12, 5e12, 2e12 → G·5e12/c².
- [ ] **Step 2:** `dotnet test --filter FullyQualifiedName~ForceSolverTests` → compile failure.
- [ ] **Step 3:** Implement. Frame beta: Lab → 0, CylinderN → that cylinder's lab β.
- [ ] **Step 4:** Same command → pass.
- [ ] **Step 5:** Commit `Add force solver for all models and frames`.

### Task 5: Independent weak-field metric check

**Files:**
- Create: `src/GravityCylinders/Physics/WeakFieldMetricCheck.cs`
- Test: `tests/GravityCylinders.Tests/Physics/WeakFieldMetricCheckTests.cs`

**Interfaces:**
- Consumes: `Cylinder`, `FiniteSegment.Potential`, `Relativity`, `Phys`.
- Produces: `WeakFieldMetricCheck.NetForceInFrame(IReadOnlyList<Cylinder> cylinders, int on, double frameBeta,
  int samples = 4000) → double` (signed y-force, N), and
  `WeakFieldMetricCheck.Metric(Cylinder source, double frameBeta, double x0, double x, double y, double z) → double[4,4]`
  (h_μν, coordinates x⁰ = ct, x, y, z).

Algorithm (spec §1.5; not determined by the signature):
```
h_μν = −(2Φ/c²)(η_μν + 2 u_μ u_ν),  η = diag(−1,1,1,1),  u^μ = (γ_s, γ_s β_s, 0, 0) with β_s = InFrame(β_source, frameBeta)
Φ = FiniteSegment.Potential(λ_s, L_s, γ_s (x − β_s x⁰), √((y − y_s)² + z²))
For each sample of cylinder `on` (midpoints of N equal slices of [−L/(2γ_on), L/(2γ_on)], y = y_on, z = 0, x⁰ = 0):
  U^μ = (γ_on, γ_on β_on, 0, 0)
  Γʸ_αβ Uᵅ Uᵝ = Σ_αβ (∂_α h_yβ − ½ ∂_y h_αβ) Uᵅ Uᵝ          (central differences, step ε = 1e-4 · min separation)
  a_y = −c² Γʸ_αβ Uᵅ Uᵝ,   summed over all sources ≠ on
  total += λ_on · a_y · Δx     (mass per frame length γλ, times 1/γ for dt_k)
```

- [ ] **Step 1: Write the failing tests**
  - `Metric_StaticSourceIsIsotropicNewtonian`: β = 0, frame 0: h₀₀ = h_xx = h_yy = −2Φ/c², off-diagonal 0.
  - `Check_AgreesWithSolver_AllFrames`: L = 3000, y = −3, 0, 3, β = 0.6, 0, −0.5, λ = 1e12: for each frame and each
    cylinder, `NetForceInFrame` matches `ForceSolver` GR `NetTotal` within 0.2%.
  - `Check_AtRest_IsNewtonian`: all β = 0: matches the Newtonian solver within 0.2%.
- [ ] **Step 2:** `dotnet test --filter FullyQualifiedName~WeakFieldMetricCheckTests` → compile failure.
- [ ] **Step 3:** Implement. If the solver check misses 0.2%, first refine `samples`/ε before suspecting physics; do
  not loosen the tolerance.
- [ ] **Step 4:** Same command → pass; test run under 5 s.
- [ ] **Step 5:** Commit `Add independent weak-field metric check`.

### Task 6: Presentation (force table, HUD, badge)

**Files:**
- Create: `src/GravityCylinders/Presentation/ForceTableBuilder.cs`, `Presentation/HudFormatter.cs`
- Test: `tests/GravityCylinders.Tests/Presentation/PresentationTests.cs` (derive from `InvariantCultureTest`)

**Interfaces:**
- Consumes: `SceneForces`, `PhysicsModel`, `FrameChoice`, `Phys.Eng`.
- Produces:
  - `public enum BadgeLevel { Green, Amber, Red }`; `HudFormatter.Badge(double weakFieldParameter) → (string Text, BadgeLevel Level)`,
    text `"Gλ/c² = 2.2e-16 (weak field OK)"` / `"(near weak-field limit)"` / `"(outside weak-field model)"`, mantissa
    format `0.0e0`.
  - `HudFormatter.Frame(SceneForces f) → string`, e.g. `"Frame: Cylinder 2 (β = −0.6)   Model: General relativity"`.
  - `HudFormatter.Check(double solver, double direct) → string`: `"Direct metric calculation: 1.23 TN (Δ = 0.04%)"`;
    `HudFormatter.CheckNotApplicable = "Direct metric calculation: n/a (GR only)"`.
  - `HudFormatter.ModelHelp(PhysicsModel)` → one paragraph per model; the SR text must contain "not a self-consistent
    theory of gravity"; the GR text mentions γ²(1 + w²).
  - `HudFormatter.SimultaneityNote` — one sentence: totals differ between frames because the overlapping sections are
    judged "at the same moment" differently; per-metre forces agree.
  - `public sealed record ForceRow(string Cylinder, string Source, string NewtonianPerMetre, string Coupling,
    string PerElement, string Overlap, string Total);` and `public sealed record CylinderRow(string Name, string Speed,
    string Gamma, string Length, string NetPerMetre, string NetTotal, IReadOnlyList<ForceRow> Pairs);`
    `ForceTableBuilder.Build(SceneForces) → IReadOnlyList<CylinderRow>`; forces via `Phys.Eng(v, "N/m"|"N")`,
    Coupling `"× 2.125"`, β signed with `−` sign and 3 decimals, γ `0.###`.
- [ ] **Step 1:** Write tests: badge thresholds at 9.9e-4 / 1e-3 / 9.9e-3 / 1e-2; Frame text for Lab and Cylinder 2;
  Check text and Δ%; SR help contains the phrase; Build returns 3 rows × 2 pairs with the GrCoupling scene from Task 4
  showing `"× 2.125"`; zero net shows `"0 N"`.
- [ ] **Step 2:** `dotnet test --filter FullyQualifiedName~PresentationTests` → compile failure.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Same command → pass.
- [ ] **Step 5:** Commit `Add force table and HUD text`.

### Task 7: View model (inputs, validation, presets)

**Files:**
- Create: `src/GravityCylinders/CylinderViewModel.cs`, `src/GravityCylinders/MainViewModel.cs`
- Test: `tests/GravityCylinders.Tests/MainViewModelTests.cs`

**Interfaces:**
- Consumes: `ForceSolver`, `WeakFieldMetricCheck`, `ForceTableBuilder`, `HudFormatter`.
- Produces:
  - `CylinderViewModel : ObservableObject` — `Index`, `Name` ("Cylinder 1"…), `Lambda`, `Length`, `Radius`, `Y`,
    `Beta` (setters clamp β to ±0.99; reject non-positive λ/L/R by reverting and raising `Rejected(string message)`),
    `ToCylinder()`.
  - `MainViewModel : ObservableObject` — `Cylinders` (3), `Models` (`PhysicsModel[]`), `SelectedModel`, `Frames`
    (`FrameChoice[]`), `SelectedFrame`, `Presets` (string[]), `PresetIndex`, `ArrowLengthShowsStrength` (default
    true), `ArrowScale` (1), `TimeScale` (1), `Forces` (`SceneForces`), `Rows`, `FrameText`, `BadgeText`,
    `BadgeLevel`, `CheckText`, `ModelHelpText`, `SimultaneityNote`, `ValidationMessage`, `ForcesChanged` event.
  - Recalculate on any input/listbox change; metric check only when GR, else `CheckNotApplicable`.
  - Ordering/overlap validation on Y or Radius change: if y₁ < y₂ < y₃ or dᵢⱼ > Rᵢ + Rⱼ would break, revert and set
    `ValidationMessage` ("Cylinder 2 must stay between 1 and 3" / "Cylinders 1 and 2 would overlap"); cleared on next
    valid change.
  - Defaults: λ = 1e12 kg/m, L = 60 m, R = 0.4 m, y = −3, 0, 3; GR; Lab.
  - Presets (verbatim, β₁/β₂/β₃): "All co-moving (0.5c)" 0.5/0.5/0.5; "Middle counter-moving" 0.5/−0.5/0.5;
    "Near-light counter-motion" 0.95/−0.95/0.95; "Outer at rest, middle moving" 0/0.8/0. Presets keep model and frame.
- [ ] **Step 1:** Write tests:
  - `Defaults_ProduceForces` (Rows has 3 entries, BadgeLevel Green).
  - `ChangingModel_ChangesForces` and `ChangingFrame_ChangesBetaInFrame` (Middle counter-moving, frame Cylinder2 →
    cylinder 2 BetaInFrame == 0).
  - `MiddleCannotLeave` (review focus 5): set cylinder 2 Y = 5 → Y stays 0, ValidationMessage non-empty, Forces object
    unchanged (same reference).
  - `OverlapRejected`: cylinder 1 Radius = 3.5 → reverted, message.
  - `NegativeLambdaRejected`: λ = −1 → reverted.
  - `BetaClamped`: 1.5 → 0.99.
  - `PresetKeepsModelAndFrame`.
  - `CheckOnlyInGr`: Newtonian → CheckText == `HudFormatter.CheckNotApplicable`.
- [ ] **Step 2:** `dotnet test --filter FullyQualifiedName~MainViewModelTests` → compile failure.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Same command → pass.
- [ ] **Step 5:** Commit `Add view model with validation and presets`.

### Task 8: Rendering and main window

**Files:**
- Create: `src/GravityCylinders/Rendering/CylinderRenderer.cs`, `Rendering/RingMarkerRenderer.cs`,
  `Rendering/ForceArrowRenderer.cs`
- Modify: `MainWindow.xaml`, `MainWindow.xaml.cs`, `README.md`
- Test: `tests/GravityCylinders.Tests/Rendering/RenderingTests.cs`

**Interfaces:**
- Consumes: `MeshBuilder`, `SceneStyle`, `ArrowScaling`, `FrameTimer`, `OrbitCamera`, `ViewportMouseController`,
  `MainViewModel`, `SceneForces`.
- Produces:
  - `CylinderRenderer.Build(SceneForces f, IReadOnlyList<Cylinder> c) → Model3DGroup` — one capped cylinder per
    cylinder along x, centred at (0, yᵢ, 0), length `LengthInFrame`, radius Rᵢ, 24 sides, `SceneStyle.ForMaterial(i)`; add
    `MeshBuilder.AddDisc(Point3D centre, Vector3D normal, double radius, int sides)` for the end caps if
    `AddCylinder` leaves the ends open.
  - `RingMarkerRenderer` — `const double RingSpacing = 4`, `const double SceneSpeedAtC = 20` (scene m/s of ring
    motion at β = 1; real speeds are far too fast to see); `Advance(SceneForces f, double dt, double timeScale)`
    adds `BetaInFrame · SceneSpeedAtC · dt · timeScale` to each phase (mod spacing); `Build(...)` places thin rings
    (radius 1.05 R) every `RingSpacing / γ` within the drawn length.
  - `ForceArrowRenderer.Build(SceneForces f, IReadOnlyList<Cylinder> c, bool showStrength, double scale)` — net arrow
    (white) from each cylinder's centre along ±y; per-source arrows (source's color, 0.6× radius) at x = ±3 m.
    Reference magnitude = largest |NetPerFrameLength| or pair value in the scene; `ArrowScaling.Length(m, ref, 3, scale)`
    with cell size 3 m; normalized mode uses m = ref for non-zero values. Arrows use per-frame-length forces (frame
    invariant), so frame switching changes lengths of cylinders but not arrows.
  - `MainWindow`: left panel (Presets ComboBox; Model ListBox; Frame ListBox; per-cylinder editor for λ, L, R, y, β;
    arrow options; time scale; validation message in amber), 3D viewport, overlay HUD (FrameText, badge colored by
    level, CheckText), bottom force table (`ItemsControl` over `Rows`), model help and simultaneity note under the
    table. Per-frame loop: `FrameTimer.Tick` → ring advance → rebuild rings; rebuild cylinders/arrows on
    `ForcesChanged`.
- [ ] **Step 1:** Write tests:
  - `CylinderLength_FollowsFrame`: β 0.6 cylinder seen from Lab in GR → mesh x-extent = 60/1.25 = 48 (±1e-9).
  - `Newtonian_NoContraction`: same → 60.
  - `ArrowPointsTowardSource`: two-cylinder scene, net arrow on cylinder at y = −3 extends to y > −3.
  - `ZeroNet_NoNetArrow` (middle symmetric scene): no vertices for the middle net arrow.
  - `Rings_StandStillForSelectedFrame`: frame Cylinder1, Advance by 1 s → cylinder 1 phase unchanged, others change.
  - `ExtremeForces_ArrowsCapped`: near-light preset → every arrow ≤ `3 · CellFill · MaxRelativeLength · scale`.
- [ ] **Step 2:** `dotnet test --filter FullyQualifiedName~RenderingTests` → compile failure.
- [ ] **Step 3:** Implement renderers, then the window wiring.
- [ ] **Step 4:** `dotnet test` → all tests pass; `dotnet build` → 0 warnings.
- [ ] **Step 5:** Launch with `dotnet run --project src/GravityCylinders`; screenshot; verify by eye: three cylinders
  with ends, arrows toward neighbours, switching Frame to "Cylinder 2" freezes cylinder 2's rings and changes lengths,
  switching Model changes the table, badge green.
- [ ] **Step 6:** Update `README.md` (controls, physics summary from spec §1 in 8–10 lines including the simultaneity
  note). Commit `Add 3D rendering and main window`.
