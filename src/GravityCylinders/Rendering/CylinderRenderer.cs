using System.Windows.Media.Media3D;
using GravityCylinders.Physics;

namespace GravityCylinders.Rendering;

/// <summary>Draws each cylinder along x, centred at (0, y, 0), with its length as measured in the current frame.</summary>
public static class CylinderRenderer
{
    public const int Sides = 24;

    /// <summary>One child model per cylinder, in cylinder order.</summary>
    public static Model3DGroup Build(SceneForces forces, IReadOnlyList<Cylinder> cylinders)
    {
        var group = new Model3DGroup();
        for (int i = 0; i < cylinders.Count; i++)
        {
            var c = cylinders[i];
            double half = forces.Cylinders[i].LengthInFrame / 2;
            var a = new Point3D(-half, c.Y, 0);
            var b = new Point3D(half, c.Y, 0);
            var mb = new MeshBuilder();
            mb.AddCylinder(a, b, c.Radius, Sides);
            mb.AddDisc(a, new Vector3D(-1, 0, 0), c.Radius, Sides);
            mb.AddDisc(b, new Vector3D(1, 0, 0), c.Radius, Sides);
            group.Children.Add(SceneStyle.Model(mb.ToMesh(), SceneStyle.ForMaterial(i)));
        }
        return group;
    }
}
