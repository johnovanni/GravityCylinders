# Gravity Cylinders — Design

Date: 2026-10-13
Status: approved in conversation, pending written-spec review

## Purpose

An interactive 3D WPF app (.NET 10) that builds intuition for how motion and choice of frame change gravity. Three
finite but very long, parallel, non-rotating cylinders of mass move along their long axes at user-set speeds. The app
shows the gravitational force on each cylinder, in the frame of the lab or of any cylinder, under a Newtonian, a
special-relativistic or a general-relativistic (weak-field) model.

It is a sibling of `EMFieldVisualizer` (same layout, controls and look), but there is only gravity: no
gravito-electric / gravito-magnetic split, and no field map.

Success criteria:
- Switching the **Model** listbox (Newtonian / Special relativity / General relativity) and the **Frame** listbox
  (Lab / Cylinder 1 / 2 / 3) changes arrows and numbers, and each value is correct within its model (verified by
  tests against analytic results).
- The force breakdown (Newtonian per metre → × GR factor → ÷ γ per element → total over the overlap in the frame) is visible for every cylinder.
- In GR, an independent calculation done directly in the chosen frame agrees with the rest-frame value transformed
  into that frame, and the app shows the agreement.

## 1. Physics

Notation: cylinders i ∈ {1, 2, 3}; proper (rest-frame) linear density λᵢ (kg/m); proper length Lᵢ; radius Rᵢ;
transverse position yᵢ (z = 0); lab velocity βᵢ c along x. Separation dᵢⱼ = |yᵢ − yⱼ|. Relative speed of i and j:
wᵢⱼ = |βᵢ − βⱼ| / (1 − βᵢβⱼ) (relativistic); γ_w = 1/√(1 − w²). Cylinders are centered on x = 0 at the snapshot
instant (steady state: overlap treated as constant).

Weak field throughout: Gλ/c² ≪ 1. Badge shows max Gλᵢ/c²: green < 10⁻³, amber < 10⁻², red otherwise (values still
computed, labeled outside the model's validity).

### 1.1 Newtonian (rest mass sources and feels gravity; Galilean frames)
Force on i from j: the exact force between two parallel, centered segments of lengths A (on) and B (source) at
separation d:

  F_seg(λ_a, A, λ_b, B, d) = (2G λ_a λ_b / d) · [ √(((A+B)/2)² + d²) − √(((A−B)/2)² + d²) ]

which tends to (2Gλ_aλ_b/d) · min(A, B) for A, B ≫ d. Newtonian mode uses proper lengths and densities. Frames are
Galilean: velocities subtract, lengths and forces are the same in every frame.

### 1.2 Weak-field GR (the full model)
In the rest frame of source j the metric is static: h_μν = −2Φⱼ (η_μν + 2 u_μ u_ν)/c², with Φⱼ the Newtonian potential
of segment j (proper density λⱼ, proper length Lⱼ) and u the source 4-velocity. For a test element of cylinder i with
4-velocity U, the transverse geodesic force (exact to first order in G, any speed, any position, because h_yα = 0 for
α ≠ y and Uʸ = 0) is

  dp_y/dτ = −m ∂_yΦⱼ · (2(u·U)²/c⁴ − 1) = −m ∂_yΦⱼ · γ_w²(1 + w²/c²).

dp_y/dτ is the force on the element in i's own rest frame, and it depends only on the source's **proper** density.
So the primary, frame-independent quantity is the force per unit proper length on an interior element of i:

  f_i←j = (2G λᵢ λⱼ / dᵢⱼ) · K(wᵢⱼ),   K_GR(w) = γ_w²(1 + w²/c²).

### 1.3 Special relativity (relativistic kinematics, Newtonian coupling)
Same as GR with K_SR = 1. This is not a self-consistent theory of gravity; it isolates what relativistic kinematics
alone does, and the app's help text says so. GR = SR × γ_w²(1 + w²) exactly, so the breakdown is clean.
Newtonian mode: K_N = 1 with Galilean frames.

### 1.4 Frames and totals
Observer frame k ∈ {Lab, 1, 2, 3} with lab velocity β_k (0 for Lab). SR/GR: β_i,k = (βᵢ − β_k)/(1 − βᵢβ_k),
γ_ik from it. Newtonian: β_i,k = βᵢ − β_k, γ_ik ≡ 1.
- Per element: force in frame k = dp_y/dt_k = f_i←j / γ_ik per unit proper length (time dilation; p_y is invariant
  under boosts along x). Per unit length **in frame k** (elements are γ_ik denser) this is f_i←j again — invariant.
- Length of i in frame k: Lᵢ/γ_ik; clock rate 1/γ_ik.
- Total force on i in frame k: the elements of i that overlap j *at one instant of frame k* depend on k's
  simultaneity (sliding cylinders), so totals are computed in frame k's own geometry:

    F_i←j^k = K(wᵢⱼ) · F_seg(λᵢ, γ_jk·Lᵢ/γ_ik, λⱼ, Lⱼ, dᵢⱼ) / γ_jk

  (i's elements over its frame-k length Lᵢ/γ_ik, mass per k-length γ_ik λᵢ times 1/γ_ik for dt_k, in the boosted
  field of j, Φⱼ(γ_jk x); substituting x′ = γ_jk x gives this exact form — amended after final review, the earlier
  "static contracted segment" form had the wrong end-fringe width d instead of d/γ_jk).
  For long cylinders this is f_i←j × overlap length in frame k, overlap = min(Lᵢ/γ_ik, Lⱼ/γ_jk). Totals therefore
  differ between frames even though per-length forces do not — a genuine relativity-of-simultaneity effect the HUD
  points out. With equal proper lengths, rest-frame totals obey F_i←j^(i) = −F_j←i^(j).
- Net force on i = sum over j ≠ i (signed along y; attraction toward j), both per length and total.

### 1.5 Independent frame-k check (GR)
`WeakFieldMetricCheck` builds h_μν(x⁰, x, y, z) for each moving source directly in frame k (source 4-velocity u in
frame k; Φⱼ evaluated at the source rest-frame coordinate x′ = γ_jk(x − β_jk x⁰)), takes Christoffel symbols by central
finite differences, and integrates −c² Γʸ_αβ Uᵅ Uᵝ / γ_ik over the elements of i as they lie in frame k at x⁰ = 0
(mass per k-length γ_ik λᵢ). The HUD shows "direct in frame k: … (Δ = …%)". Tests assert agreement with §1.4's total
exactly (up to quadrature error), at any length.

## 2. Scene, inputs and UI

- Cylinders lie along x, side by side along y in the z = 0 plane. Ordering y₁ < y₂ < y₃ is enforced (cylinder 2 is
  always in the middle); separations must exceed the sum of radii.
- Per-cylinder inputs: λ (kg/m), L (m), R (m), y (m), β (−0.99 … 0.99, lab frame, sign = ±x).
- **Model** listbox: Newtonian / Special relativity / General relativity.
- **Frame** listbox: Lab / Cylinder 1 / Cylinder 2 / Cylinder 3. Newtonian mode uses Galilean transforms.
- Presets: all co-moving; middle cylinder counter-moving; near-light counter-motion (β = ±0.95); outer cylinders at
  rest, middle moving.
- Units: SI.

### Display
- 3D viewport with the EM app's controls: left-drag rotate, right/middle-drag pan, wheel zoom.
- Cylinders drawn with visible end caps, length = length in the current frame (contracted in SR/GR).
- Ring markers on each cylinder slide at its speed in the current frame (animated; time-scale slider), so the
  selected cylinder's rings stand still.
- On each cylinder: one net force arrow (y direction, at the cylinder's center) and two thinner, color-coded
  per-source arrows. Arrow scaling as in the EM app (√ length scaling with a reference force, or normalized).
- Force table (one row per cylinder): speed in frame, γ, length in frame, then per source: Newtonian force per
  metre, × GR factor, ÷ γ_ik (per element), overlap length in frame, total in frame; net force total and per unit length. Plus the frame-k check line and the
  weak-field badge.

## 3. Architecture

```
src/GravityCylinders/
  Physics/         Units, Cylinder, Relativity (γ, velocity addition), FiniteSegment (F_seg, Φ, ∂yΦ),
                   ForceModels (Newtonian / SpecialRelativity / GeneralRelativity : IForceModel),
                   FrameTransform, ForceSolver (per-pair + net, rest and frame-k), WeakFieldMetricCheck
  Visualization/   ArrowScaling, FrameTimer                         — no WPF UI
  Presentation/    ForceTableBuilder, HudFormatter
  Rendering/       MeshBuilder, OrbitCamera, SceneStyle, CylinderRenderer, RingMarkerRenderer, ForceArrowRenderer
  Input/           ViewportMouseController
  MainViewModel    UI state, presets;  MainWindow: wiring + per-frame loop only
tests/GravityCylinders.Tests/
```

Copied from `EMFieldVisualizer` with minimal changes: OrbitCamera, MeshBuilder, SceneStyle, ViewportMouseController,
FrameTimer, ArrowScaling, ObservableObject, TestHelpers, and their tests.

Key interfaces:
- `IForceModel.RestForce(Cylinder on, Cylinder source)` → `PairForce { Newtonian, CouplingFactor, Rest }`.
- `ForceSolver.Solve(IReadOnlyList<Cylinder>, IForceModel, Frame)` → per-cylinder `CylinderForces` (pair forces,
  frame factor, force in frame, per-length, net).
- `FrameTransform` — Galilean or Lorentz, chosen by the model.

Per-frame data flow: VM state → model + frame → `ForceSolver` → table/HUD; renderers draw cylinders (frame lengths),
rings (advanced by frame dt × time scale × frame speed) and arrows. Forces are recomputed only when inputs change.

## 4. Edge cases
- β clamped to ±0.99; λ, L, R > 0; overlapping cylinders (d ≤ Rᵢ + Rⱼ) rejected with an inline message.
- Reordering y values that would break y₁ < y₂ < y₃ is rejected (value reverts, message shown).
- Weak-field badge red when Gλ/c² ≥ 10⁻²; numbers still shown and labeled.
- Metric check skipped (shown as "n/a") in Newtonian and SR modes.

## 5. Testing (xUnit, analytic checks)
- All velocities zero → all three models give the Newtonian force, in every frame.
- F_seg → 2Gλ_aλ_b·L/d for L ≫ d; F_seg matches numerical double integration for short segments.
- GR co-moving cylinders (w = 0) → exactly Newtonian; GR/SR ratio = γ_w²(1 + w²); at small w the GR excess ≈ 2w²/c².
- Relativistic velocity addition; per-unit-length force in frame k equals f_i←j in every frame; per-element force in
  frame k = f_i←j / γ_ik; long-cylinder totals = f × overlap in frame k.
- Equal proper lengths: rest-frame totals equal and opposite (F_i←j^(i) = −F_j←i^(j)).
- Galilean frames in Newtonian mode: force identical in all frames.
- WeakFieldMetricCheck agrees with §1.4 within 0.2% for L/d = 1000, in all four frames.
- Middle cylinder between two equal, symmetric co-moving outers → net force zero; counter-moving outers with
  different speeds → nonzero net in GR, zero in Newtonian.
- View model: ordering constraint, both listboxes change outputs, presets load; HudFormatter/table formatting.
- Copied tests for camera, arrow scaling, frame timer.

## Out of scope (YAGNI)
Strong-field GR (exact cylinder metrics), cylinders moving transversely under the forces (dynamics), animated
sliding/changing overlap, rotation, field maps, macOS/iOS.
