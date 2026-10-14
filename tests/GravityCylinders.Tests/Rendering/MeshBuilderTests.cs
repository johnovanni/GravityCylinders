using GravityCylinders.Physics;
using GravityCylinders.Rendering;
using GravityCylinders.Visualization;
using static GravityCylinders.Tests.TestHelpers;

namespace GravityCylinders.Tests.Rendering;

public class MeshBuilderTests
{
    [Fact]
    public void Arrow_HasExpectedTopology()
    {
        var mb = new MeshBuilder();
        mb.AddArrow(new Point3D(), new Vector3D(0, 0, 2), 0.1, u: 0.7);
        var mesh = mb.ToMesh();

        Assert.Equal(20, mesh.Positions.Count);        // 12 shaft + 6 head ring + tip + back centre
        Assert.Equal(24 * 3, mesh.TriangleIndices.Count);
        Assert.All(mesh.TextureCoordinates, t => Assert.Equal(0.7, t.X));
        Assert.True(mesh.IsFrozen);
    }

    [Fact]
    public void Arrow_SpansStartToTip()
    {
        var mb = new MeshBuilder();
        mb.AddArrow(new Point3D(1, 1, 1), new Vector3D(3, 0, 0), 0.1);
        var mesh = mb.ToMesh();
        Assert.Equal(1.0, mesh.Positions.Min(p => p.X), 9);
        Assert.Equal(4.0, mesh.Positions.Max(p => p.X), 9);
    }

    [Fact]
    public void ZeroLengthArrow_IsSkipped()
    {
        var mb = new MeshBuilder();
        mb.AddArrow(new Point3D(), new Vector3D(), 0.1);
        Assert.Equal(0, mb.VertexCount);
    }

    [Fact]
    public void Sphere_HasUnitNormalsAndRadius()
    {
        var mb = new MeshBuilder();
        mb.AddSphere(new Point3D(1, 0, 0), 2, slices: 4, stacks: 2);
        var mesh = mb.ToMesh();

        Assert.Equal(3 * 5, mesh.Positions.Count);
        Assert.Equal(mesh.Positions.Count, mesh.Normals.Count);
        Assert.All(mesh.Normals, n => AssertClose(1, n.Length));
        Assert.All(mesh.Positions, p => AssertClose(2, (p - new Point3D(1, 0, 0)).Length));
    }

    [Fact]
    public void Box_HasSixQuads()
    {
        var mb = new MeshBuilder();
        mb.AddBox(new Point3D(), new Point3D(1, 2, 3));
        var mesh = mb.ToMesh();
        Assert.Equal(24, mesh.Positions.Count);
        Assert.Equal(36, mesh.TriangleIndices.Count);
    }

    [Fact]
    public void Cylinder_UsesTwoVerticesPerSide()
    {
        var mb = new MeshBuilder();
        mb.AddCylinder(new Point3D(), new Point3D(0, 5, 0), 1, sides: 8);
        Assert.Equal(16, mb.VertexCount);
    }

    [Fact]
    public void Materials_AreFrozen()
    {
        Assert.True(MeshBuilder.SolidMaterial(System.Windows.Media.Colors.Red).IsFrozen);
        Assert.True(MeshBuilder.GradientMaterial(System.Windows.Media.Colors.Red, System.Windows.Media.Colors.Blue).IsFrozen);
    }
}
