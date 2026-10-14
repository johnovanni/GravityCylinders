using System.Windows.Media.Media3D;
using GravityCylinders.Physics;
using GravityCylinders.Visualization;

namespace GravityCylinders.Rendering;

/// <summary>
/// Force arrows along ±y: a white net arrow at each cylinder's centre and a thinner arrow, in the source's colour,
/// for the pull of each other cylinder. Arrows use force per metre in the current frame, which is the same in every
/// frame, so switching frames changes the cylinders' lengths but not the arrows.
/// </summary>
public static class ForceArrowRenderer
{
    /// <summary>A reference-strength arrow is <see cref="ArrowScaling.CellFill"/> of this many metres long.</summary>
    public const double CellSize = 3;
    public const double PairOffsetX = 3;

    private static readonly System.Windows.Media.Media3D.Material NetMaterial = MeshBuilder.SolidMaterial(SceneStyle.ForceNet, 0.4);

    public static Vector3D NetVector(SceneForces f, int on, bool showStrength, double scale) =>
        Arrow(f.Cylinders[on].NetPerFrameLength, Reference(f), showStrength, scale);

    public static Vector3D PairVector(SceneForces f, int on, int source, bool showStrength, double scale) =>
        Arrow(f.Cylinders[on].Pairs.Single(p => p.Source == source).PerFrameLength, Reference(f), showStrength, scale);

    public static Model3DGroup Build(SceneForces f, IReadOnlyList<Cylinder> cylinders, bool showStrength, double scale)
    {
        var group = new Model3DGroup();
        for (int i = 0; i < cylinders.Count; i++)
        {
            var c = cylinders[i];
            var net = NetVector(f, i, showStrength, scale);
            if (net.Length > 0)
                group.Children.Add(ArrowModel(Start(c, 0, net), net, 0.12, NetMaterial));

            var pairs = f.Cylinders[i].Pairs;
            for (int k = 0; k < pairs.Count; k++)
            {
                var v = PairVector(f, i, pairs[k].Source, showStrength, scale);
                if (v.Length == 0) continue;
                double x = k == 0 ? -PairOffsetX : PairOffsetX;
                group.Children.Add(ArrowModel(Start(c, x, v), v, 0.07, SceneStyle.ForMaterial(pairs[k].Source)));
            }
        }
        return group;
    }

    private static double Reference(SceneForces f)
    {
        double max = 0;
        foreach (var c in f.Cylinders)
        {
            max = Math.Max(max, Math.Abs(c.NetPerFrameLength));
            foreach (var p in c.Pairs) max = Math.Max(max, Math.Abs(p.PerFrameLength));
        }
        return max;
    }

    private static Vector3D Arrow(double force, double reference, bool showStrength, double scale)
    {
        double magnitude = Math.Abs(force);
        if (!showStrength && magnitude > 0) magnitude = reference;
        double length = ArrowScaling.Length(magnitude, reference, CellSize, scale);
        return new Vector3D(0, Math.Sign(force) * length, 0);
    }

    private static Point3D Start(Cylinder c, double x, Vector3D direction) =>
        new(x, c.Y + Math.Sign(direction.Y) * c.Radius, 0);

    private static GeometryModel3D ArrowModel(Point3D start, Vector3D v, double radius, System.Windows.Media.Media3D.Material material)
    {
        var mb = new MeshBuilder();
        mb.AddArrow(start, v, radius);
        return SceneStyle.Model(mb.ToMesh(), material);
    }
}
