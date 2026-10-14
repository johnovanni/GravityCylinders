using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GravityCylinders.Rendering;

/// <summary>Shared colours, materials and lighting for the 3D scene.</summary>
public static class SceneStyle
{
    public static readonly Color Cylinder1 = Color.FromRgb(0xFF, 0x8A, 0x3D);
    public static readonly Color Cylinder2 = Color.FromRgb(0x4D, 0xA3, 0xFF);
    public static readonly Color Cylinder3 = Color.FromRgb(0xB5, 0x7B, 0xFF);
    public static readonly Color ForceNet = Color.FromRgb(0xFF, 0xFF, 0xFF);
    public static readonly Color RingMarker = Color.FromRgb(0xE8, 0xE8, 0xE8);
    public static readonly Color Axis = Color.FromRgb(0x50, 0x58, 0x68);

    private static readonly Color[] CylinderColors = [Cylinder1, Cylinder2, Cylinder3];
    private static readonly Material[] CylinderMaterials = CylinderColors.Select(c => MeshBuilder.SolidMaterial(c)).ToArray();

    public static Color ForColor(int index) => CylinderColors[index];

    public static Material ForMaterial(int index) => CylinderMaterials[index];

    public static void AddLights(Model3DGroup group)
    {
        group.Children.Add(new AmbientLight(Color.FromRgb(0x50, 0x55, 0x60)));
        group.Children.Add(new DirectionalLight(Color.FromRgb(0xC8, 0xC8, 0xC8), new Vector3D(-0.5, -1, -0.7)));
        group.Children.Add(new DirectionalLight(Color.FromRgb(0x70, 0x70, 0x78), new Vector3D(0.6, 0.4, 0.8)));
    }

    /// <summary>A model with the same material on both faces.</summary>
    public static GeometryModel3D Model(MeshGeometry3D? mesh, Material material) =>
        new(mesh, material) { BackMaterial = material };
}
