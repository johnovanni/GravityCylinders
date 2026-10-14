using System.Windows.Media.Media3D;
using GravityCylinders.Physics;

namespace GravityCylinders.Rendering;

/// <summary>
/// Thin rings painted on each cylinder that slide along with it at its speed in the current frame, so the rest
/// frame's own cylinder visibly stands still. Real speeds are far too fast to see, so a speed of c is drawn as
/// <see cref="SceneSpeedAtC"/> scene metres per second.
/// </summary>
public sealed class RingMarkerRenderer
{
    /// <summary>Proper spacing between rings (m); in the frame it is contracted by 1/γ.</summary>
    public const double RingSpacing = 4;
    public const double SceneSpeedAtC = 20;
    private const double RingThickness = 0.15;

    private static readonly System.Windows.Media.Media3D.Material RingMaterial = MeshBuilder.SolidMaterial(SceneStyle.RingMarker, 0.5);

    public double[] Phases { get; } = new double[3];

    public void Advance(SceneForces forces, double dt, double timeScale)
    {
        for (int i = 0; i < Phases.Length && i < forces.Cylinders.Count; i++)
        {
            double phase = Phases[i] + forces.Cylinders[i].BetaInFrame * SceneSpeedAtC * dt * timeScale;
            Phases[i] = phase - Math.Floor(phase / RingSpacing) * RingSpacing;
        }
    }

    public Model3DGroup Build(SceneForces forces, IReadOnlyList<Cylinder> cylinders)
    {
        var mb = new MeshBuilder();
        for (int i = 0; i < cylinders.Count; i++)
        {
            var c = cylinders[i];
            var f = forces.Cylinders[i];
            double spacing = RingSpacing / f.Gamma;
            double half = f.LengthInFrame / 2;
            double offset = Phases[i] / f.Gamma;
            double first = offset - Math.Ceiling((offset + half) / spacing) * spacing;
            for (double x = first; x <= half; x += spacing)
            {
                double a = Math.Max(x - RingThickness / 2, -half), b = Math.Min(x + RingThickness / 2, half);
                if (b - a > 1e-6)
                    mb.AddCylinder(new Point3D(a, c.Y, 0), new Point3D(b, c.Y, 0), c.Radius * 1.05, CylinderRenderer.Sides);
            }
        }
        var group = new Model3DGroup();
        group.Children.Add(SceneStyle.Model(mb.ToMesh(), RingMaterial));
        return group;
    }
}
