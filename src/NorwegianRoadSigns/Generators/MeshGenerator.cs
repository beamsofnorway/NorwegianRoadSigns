using NorwegianRoadSigns.Models;

namespace NorwegianRoadSigns.Generators;

public class MeshGenerator
{
    private const float DefaultThickness = 0.02f; // 20mm thickness for signs

    // Swap Y and Z for NVDB -> Blender coordinate conversion (height goes to Z)
    private static Vector3 SwapYAndZ(Vector3 v) => new Vector3(v.X, v.Z, v.Y);
    
    // For mesh generation: apply swap, then offsets
    // sideOffsetX: offset along X axis (left/right of post)
    // sideOffsetY: offset along Y axis (depth, for parallel signs)
    private Vector3 WithSideOffset(Vector3 v, float offsetX, float offsetY) => new Vector3(v.X + offsetX, v.Y + offsetY, v.Z);

    public Mesh CreateRectangle(float width, float height, float thickness = DefaultThickness, float sideOffsetX = 0, float sideOffsetY = 0)
    {
        var mesh = new Mesh { Name = "Rectangle" };
        
        var hw = width / 2;
        var hh = height / 2;
        var ht = thickness / 2;
        
        Vector3 WithOffset(Vector3 v) => WithSideOffset(v, sideOffsetX, sideOffsetY);

        // Front face (facing Y+ direction - towards road)
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, -ht, hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, -ht, hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, ht, hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, ht, hh)));
        
        // Duplicate for triangles
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, -ht, hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, ht, hh)));
        
        for (int i = 0; i < 6; i++)
            mesh.Normals.Add(new Vector3(0, 1, 0));
        
        mesh.UVs.Add(new Vector2(0, 1));
        mesh.UVs.Add(new Vector2(1, 1));
        mesh.UVs.Add(new Vector2(1, 0));
        mesh.UVs.Add(new Vector2(0, 1));
        mesh.UVs.Add(new Vector2(1, 0));
        mesh.UVs.Add(new Vector2(0, 0));
        
        mesh.Indices.Add(0); mesh.Indices.Add(1); mesh.Indices.Add(2);
        mesh.Indices.Add(3); mesh.Indices.Add(4); mesh.Indices.Add(5);

        // Back face
        var baseIdx = mesh.Vertices.Count;
        mesh.Vertices.Add(WithOffset(new Vector3(hw, -ht, -hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, -ht, -hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, ht, -hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, -ht, -hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, ht, -hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, ht, -hh)));
        
        var backNormal = new Vector3(0, -1, 0);
        for (int i = 0; i < 6; i++)
            mesh.Normals.Add(backNormal);
        
        mesh.UVs.Add(new Vector2(0, 1));
        mesh.UVs.Add(new Vector2(1, 1));
        mesh.UVs.Add(new Vector2(1, 0));
        mesh.UVs.Add(new Vector2(0, 1));
        mesh.UVs.Add(new Vector2(1, 0));
        mesh.UVs.Add(new Vector2(0, 0));
        
        mesh.Indices.Add(baseIdx + 0); mesh.Indices.Add(baseIdx + 1); mesh.Indices.Add(baseIdx + 2);
        mesh.Indices.Add(baseIdx + 3); mesh.Indices.Add(baseIdx + 4); mesh.Indices.Add(baseIdx + 5);

        // Top face (Z+)
        baseIdx = mesh.Vertices.Count;
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, ht, hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, ht, hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, ht, -hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, ht, hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, ht, -hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, ht, -hh)));
        
        var topNormal = new Vector3(0, 0, 1);
        for (int i = 0; i < 6; i++)
            mesh.Normals.Add(topNormal);
        
        mesh.UVs.Add(new Vector2(0, 1));
        mesh.UVs.Add(new Vector2(1, 1));
        mesh.UVs.Add(new Vector2(1, 0));
        mesh.UVs.Add(new Vector2(0, 1));
        mesh.UVs.Add(new Vector2(1, 0));
        mesh.UVs.Add(new Vector2(0, 0));
        
        mesh.Indices.Add(baseIdx + 0); mesh.Indices.Add(baseIdx + 1); mesh.Indices.Add(baseIdx + 2);
        mesh.Indices.Add(baseIdx + 3); mesh.Indices.Add(baseIdx + 4); mesh.Indices.Add(baseIdx + 5);

        // Bottom face (Z-)
        baseIdx = mesh.Vertices.Count;
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, -ht, -hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, -ht, -hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, -ht, hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, -ht, -hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, -ht, hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, -ht, hh)));
        
        var bottomNormal = new Vector3(0, 0, -1);
        for (int i = 0; i < 6; i++)
            mesh.Normals.Add(bottomNormal);
        
        mesh.UVs.Add(new Vector2(0, 1));
        mesh.UVs.Add(new Vector2(1, 1));
        mesh.UVs.Add(new Vector2(1, 0));
        mesh.UVs.Add(new Vector2(0, 1));
        mesh.UVs.Add(new Vector2(1, 0));
        mesh.UVs.Add(new Vector2(0, 0));
        
        mesh.Indices.Add(baseIdx + 0); mesh.Indices.Add(baseIdx + 1); mesh.Indices.Add(baseIdx + 2);
        mesh.Indices.Add(baseIdx + 3); mesh.Indices.Add(baseIdx + 4); mesh.Indices.Add(baseIdx + 5);

        // Right face
        baseIdx = mesh.Vertices.Count;
        mesh.Vertices.Add(WithOffset(new Vector3(hw, -ht, hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, -ht, -hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, ht, -hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, -ht, hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, ht, -hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, ht, hh)));
        
        var rightNormal = new Vector3(1, 0, 0);
        for (int i = 0; i < 6; i++)
            mesh.Normals.Add(rightNormal);
        
        mesh.UVs.Add(new Vector2(0, 1));
        mesh.UVs.Add(new Vector2(1, 1));
        mesh.UVs.Add(new Vector2(1, 0));
        mesh.UVs.Add(new Vector2(0, 1));
        mesh.UVs.Add(new Vector2(1, 0));
        mesh.UVs.Add(new Vector2(0, 0));
        
        mesh.Indices.Add(baseIdx + 0); mesh.Indices.Add(baseIdx + 1); mesh.Indices.Add(baseIdx + 2);
        mesh.Indices.Add(baseIdx + 3); mesh.Indices.Add(baseIdx + 4); mesh.Indices.Add(baseIdx + 5);

        // Left face
        baseIdx = mesh.Vertices.Count;
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, -ht, -hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, -ht, hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, ht, hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, -ht, -hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, ht, hh)));
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, ht, -hh)));
        
        var leftNormal = new Vector3(-1, 0, 0);
        for (int i = 0; i < 6; i++)
            mesh.Normals.Add(leftNormal);
        
        mesh.UVs.Add(new Vector2(0, 1));
        mesh.UVs.Add(new Vector2(1, 1));
        mesh.UVs.Add(new Vector2(1, 0));
        mesh.UVs.Add(new Vector2(0, 1));
        mesh.UVs.Add(new Vector2(1, 0));
        mesh.UVs.Add(new Vector2(0, 0));
        
        mesh.Indices.Add(baseIdx + 0); mesh.Indices.Add(baseIdx + 1); mesh.Indices.Add(baseIdx + 2);
        mesh.Indices.Add(baseIdx + 3); mesh.Indices.Add(baseIdx + 4); mesh.Indices.Add(baseIdx + 5);

        return mesh;
    }

    public Mesh CreateSegmentedRectangle713(float width, float height, int frontSegmentCount, float thickness = DefaultThickness, float sideOffsetX = 0, float sideOffsetY = 0)
    {
        var count = Math.Max(1, frontSegmentCount);
        var equalWidths = Enumerable.Repeat(1f, count).ToList();
        return CreateSegmentedRectangle713(width, height, equalWidths, thickness, sideOffsetX, sideOffsetY);
    }

    public Mesh CreateSegmentedRectangle713(float width, float height, IReadOnlyList<float> frontSegmentWidths, float thickness = DefaultThickness, float sideOffsetX = 0, float sideOffsetY = 0)
    {
        var mesh = new Mesh { Name = "SegmentedRectangle713" };

        var hw = width / 2;
        var hh = height / 2;
        var ht = thickness / 2;

        var segments = (frontSegmentWidths ?? Array.Empty<float>())
            .Where(w => w > 0)
            .ToList();

        if (segments.Count == 0)
        {
            segments.Add(width);
        }

        var total = segments.Sum();
        if (total <= 0)
        {
            total = width;
        }

        // Normalize incoming widths so they always span the full plate width.
        var normalized = segments.Select(w => (w / total) * width).ToList();

        void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Vector2 ua, Vector2 ub, Vector2 uc)
        {
            var baseIdx = mesh.Vertices.Count;

            mesh.Vertices.Add(WithSideOffset(a, sideOffsetX, sideOffsetY));
            mesh.Vertices.Add(WithSideOffset(b, sideOffsetX, sideOffsetY));
            mesh.Vertices.Add(WithSideOffset(c, sideOffsetX, sideOffsetY));

            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);

            mesh.UVs.Add(ua);
            mesh.UVs.Add(ub);
            mesh.UVs.Add(uc);

            mesh.Indices.Add(baseIdx);
            mesh.Indices.Add(baseIdx + 1);
            mesh.Indices.Add(baseIdx + 2);
        }

        void AddQuad(Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl, Vector3 normal, Vector2 uvBl, Vector2 uvBr, Vector2 uvTr, Vector2 uvTl)
        {
            AddTriangle(bl, br, tr, normal, uvBl, uvBr, uvTr);
            AddTriangle(bl, tr, tl, normal, uvBl, uvTr, uvTl);
        }

        // Front face: split into variable-width segments for arrow/symbol/text boxes.
        var x = -hw;
        foreach (var segWidth in normalized)
        {
            var x0 = x;
            var x1 = x + segWidth;
            var u0 = (x0 + hw) / width;
            var u1 = (x1 + hw) / width;

            AddQuad(
                new Vector3(x0, -ht, -hh),
                new Vector3(x1, -ht, -hh),
                new Vector3(x1, -ht, hh),
                new Vector3(x0, -ht, hh),
                new Vector3(0, -1, 0),
                new Vector2(u0, 1),
                new Vector2(u1, 1),
                new Vector2(u1, 0),
                new Vector2(u0, 0));

            x = x1;
        }

        // Back face (single quad)
        AddQuad(
            new Vector3(hw, ht, -hh),
            new Vector3(-hw, ht, -hh),
            new Vector3(-hw, ht, hh),
            new Vector3(hw, ht, hh),
            new Vector3(0, 1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0),
            new Vector2(0, 0));

        // Top
        AddQuad(
            new Vector3(-hw, -ht, hh),
            new Vector3(hw, -ht, hh),
            new Vector3(hw, ht, hh),
            new Vector3(-hw, ht, hh),
            new Vector3(0, 0, 1),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0),
            new Vector2(0, 0));

        // Bottom
        AddQuad(
            new Vector3(-hw, ht, -hh),
            new Vector3(hw, ht, -hh),
            new Vector3(hw, -ht, -hh),
            new Vector3(-hw, -ht, -hh),
            new Vector3(0, 0, -1),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0),
            new Vector2(0, 0));

        // Right
        AddQuad(
            new Vector3(hw, -ht, -hh),
            new Vector3(hw, ht, -hh),
            new Vector3(hw, ht, hh),
            new Vector3(hw, -ht, hh),
            new Vector3(1, 0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0),
            new Vector2(0, 0));

        // Left
        AddQuad(
            new Vector3(-hw, ht, -hh),
            new Vector3(-hw, -ht, -hh),
            new Vector3(-hw, -ht, hh),
            new Vector3(-hw, ht, hh),
            new Vector3(-1, 0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0),
            new Vector2(0, 0));

        return mesh;
    }

    public Mesh CreateTriangle(float size, float thickness = DefaultThickness, float sideOffsetX = 0, float sideOffsetY = 0)
    {
        var mesh = new Mesh { Name = "Triangle" };
        
        var h = size * (float)Math.Sqrt(3) / 2;
        var hw = size / 2;
        var ht = thickness / 2;
        
        Vector3 WithOffset(Vector3 v) => WithSideOffset(v, sideOffsetX, sideOffsetY);

        // Triangle pointing up (in Z) - front face
        mesh.Vertices.Add(WithOffset(new Vector3(0, ht, hw)));
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, ht, -hw)));
        mesh.Vertices.Add(WithOffset(new Vector3(hw, ht, -hw)));

        var frontNormal = WithOffset(new Vector3(0, 1, 0));
        mesh.Normals.Add(frontNormal);
        mesh.Normals.Add(frontNormal);
        mesh.Normals.Add(frontNormal);

        mesh.UVs.Add(new Vector2(0.5f, 0));
        mesh.UVs.Add(new Vector2(0, 1));
        mesh.UVs.Add(new Vector2(1, 1));

        mesh.Indices.Add(0); mesh.Indices.Add(1); mesh.Indices.Add(2);

        // Back face
        var baseIdx = mesh.Vertices.Count;
        mesh.Vertices.Add(WithOffset(new Vector3(hw, -ht, -hw)));
        mesh.Vertices.Add(WithOffset(new Vector3(-hw, -ht, -hw)));
        mesh.Vertices.Add(WithOffset(new Vector3(0, -ht, hw)));

        var backNormal = WithOffset(new Vector3(0, -1, 0));
        mesh.Normals.Add(backNormal);
        mesh.Normals.Add(backNormal);
        mesh.Normals.Add(backNormal);

        mesh.UVs.Add(new Vector2(0.5f, 1));
        mesh.UVs.Add(new Vector2(0, 0));
        mesh.UVs.Add(new Vector2(1, 0));

        mesh.Indices.Add(baseIdx + 0); mesh.Indices.Add(baseIdx + 1); mesh.Indices.Add(baseIdx + 2);

        return mesh;
    }

    public Mesh CreateCircle(float diameter, int segments = 32, float thickness = DefaultThickness, float sideOffsetX = 0, float sideOffsetY = 0)
    {
        var mesh = new Mesh { Name = "Circle" };
        
        var radius = diameter / 2;
        var ht = thickness / 2;
        
        Vector3 WithOffset(Vector3 v) => WithSideOffset(v, sideOffsetX, sideOffsetY);

        // Front face - fan of triangles from center
        mesh.Vertices.Add(WithOffset(new Vector3(0, ht, 0)));
        mesh.Normals.Add(WithOffset(Vector3.UnitY));
        mesh.UVs.Add(new Vector2(0.5f, 0.5f));

        for (int i = 0; i <= segments; i++)
        {
            var angle = 2 * Math.PI * i / segments;
            var x = (float)(radius * Math.Cos(angle));
            var z = (float)(radius * Math.Sin(angle));
            
            mesh.Vertices.Add(WithOffset(new Vector3(x, ht, z)));
            mesh.Normals.Add(WithOffset(Vector3.UnitY));
            mesh.UVs.Add(new Vector2((float)(Math.Cos(angle) / 2 + 0.5), (float)(Math.Sin(angle) / 2 + 0.5)));
        }

        for (int i = 1; i <= segments; i++)
        {
            mesh.Indices.Add(0);
            mesh.Indices.Add(i);
            mesh.Indices.Add(i + 1);
        }

        // Back face
        var baseIdx = mesh.Vertices.Count;
        mesh.Vertices.Add(WithOffset(new Vector3(0, -ht, 0)));
        mesh.Normals.Add(WithOffset(new Vector3(0, -1, 0)));
        mesh.UVs.Add(new Vector2(0.5f, 0.5f));

        for (int i = 0; i <= segments; i++)
        {
            var angle = 2 * Math.PI * i / segments;
            var x = (float)(radius * Math.Cos(angle));
            var z = (float)(radius * Math.Sin(angle));
            
            mesh.Vertices.Add(WithOffset(new Vector3(x, -ht, z)));
            mesh.Normals.Add(WithOffset(new Vector3(0, -1, 0)));
            mesh.UVs.Add(new Vector2((float)(Math.Cos(angle) / 2 + 0.5), (float)(Math.Sin(angle) / 2 + 0.5)));
        }

        for (int i = 1; i <= segments; i++)
        {
            mesh.Indices.Add(baseIdx);
            mesh.Indices.Add(baseIdx + i + 1);
            mesh.Indices.Add(baseIdx + i);
        }

        // Side face
        baseIdx = mesh.Vertices.Count;
        
        for (int i = 0; i <= segments; i++)
        {
            var angle = 2 * Math.PI * i / segments;
            var x = (float)(radius * Math.Cos(angle));
            var z = (float)(radius * Math.Sin(angle));
            var nx = (float)Math.Cos(angle);
            var nz = (float)Math.Sin(angle);
            
            mesh.Vertices.Add(WithOffset(new Vector3(x, ht, z)));
            mesh.Normals.Add(WithOffset(new Vector3(nx, 0, nz)));
            mesh.UVs.Add(new Vector2(i / (float)segments, 0));
            
            mesh.Vertices.Add(WithOffset(new Vector3(x, -ht, z)));
            mesh.Normals.Add(WithOffset(new Vector3(nx, 0, nz)));
            mesh.UVs.Add(new Vector2(i / (float)segments, 1));
        }

        for (int i = 0; i < segments; i++)
        {
            var bl = baseIdx + i * 2;
            var br = baseIdx + (i + 1) * 2;
            var tl = bl + 1;
            var tr = br + 1;
            
            mesh.Indices.Add(bl); mesh.Indices.Add(br); mesh.Indices.Add(tr);
            mesh.Indices.Add(bl); mesh.Indices.Add(tr); mesh.Indices.Add(tl);
        }

        return mesh;
    }

    public Mesh CreateOctagon(float size, float thickness = DefaultThickness, float sideOffsetX = 0, float sideOffsetY = 0)
    {
        return CreateCircle(size, 8, thickness, sideOffsetX, sideOffsetY);
    }

    public Mesh CreateCylinder(float diameter, float height, int segments = 16)
    {
        var mesh = new Mesh { Name = "Cylinder" };
        
        var radius = diameter / 2;

        // Side vertices - top ring (Y = height in original, becomes Z)
        for (int i = 0; i <= segments; i++)
        {
            var angle = 2 * Math.PI * i / segments;
            var x = (float)(radius * Math.Cos(angle));
            var z = (float)(radius * Math.Sin(angle));
            var nx = (float)Math.Cos(angle);
            var nz = (float)Math.Sin(angle);
            
            mesh.Vertices.Add(SwapYAndZ(new Vector3(x, height, z)));
            mesh.Normals.Add(SwapYAndZ(new Vector3(nx, 0, nz)));
            mesh.UVs.Add(new Vector2(i / (float)segments, 1));
        }

        // Side vertices - bottom ring (Y = 0)
        for (int i = 0; i <= segments; i++)
        {
            var angle = 2 * Math.PI * i / segments;
            var x = (float)(radius * Math.Cos(angle));
            var z = (float)(radius * Math.Sin(angle));
            var nx = (float)Math.Cos(angle);
            var nz = (float)Math.Sin(angle);
            
            mesh.Vertices.Add(SwapYAndZ(new Vector3(x, 0, z)));
            mesh.Normals.Add(SwapYAndZ(new Vector3(nx, 0, nz)));
            mesh.UVs.Add(new Vector2(i / (float)segments, 0));
        }

        // Side faces
        for (int i = 0; i < segments; i++)
        {
            var bl = i;
            var br = (i + 1);
            var tl = segments + 1 + i;
            var tr = segments + 1 + i + 1;
            
            mesh.Indices.Add(bl); mesh.Indices.Add(br); mesh.Indices.Add(tr);
            mesh.Indices.Add(bl); mesh.Indices.Add(tr); mesh.Indices.Add(tl);
        }

        // Top cap
        var topCenter = mesh.Vertices.Count;
        mesh.Vertices.Add(SwapYAndZ(new Vector3(0, height, 0)));
        mesh.Normals.Add(SwapYAndZ(Vector3.UnitY));
        mesh.UVs.Add(new Vector2(0.5f, 0.5f));

        for (int i = 0; i <= segments; i++)
        {
            var angle = 2 * Math.PI * i / segments;
            var x = (float)(radius * Math.Cos(angle));
            var z = (float)(radius * Math.Sin(angle));
            
            mesh.Vertices.Add(SwapYAndZ(new Vector3(x, height, z)));
            mesh.Normals.Add(SwapYAndZ(Vector3.UnitY));
            mesh.UVs.Add(new Vector2((float)(Math.Cos(angle) / 2 + 0.5), (float)(Math.Sin(angle) / 2 + 0.5)));
        }

        for (int i = 0; i < segments; i++)
        {
            mesh.Indices.Add(topCenter);
            mesh.Indices.Add(topCenter + i + 1);
            mesh.Indices.Add(topCenter + i + 2);
        }

        // Bottom cap
        var bottomCenter = mesh.Vertices.Count;
        mesh.Vertices.Add(SwapYAndZ(new Vector3(0, 0, 0)));
        mesh.Normals.Add(SwapYAndZ(new Vector3(0, -1, 0)));
        mesh.UVs.Add(new Vector2(0.5f, 0.5f));

        for (int i = 0; i <= segments; i++)
        {
            var angle = 2 * Math.PI * i / segments;
            var x = (float)(radius * Math.Cos(angle));
            var z = (float)(radius * Math.Sin(angle));
            
            mesh.Vertices.Add(SwapYAndZ(new Vector3(x, 0, z)));
            mesh.Normals.Add(SwapYAndZ(new Vector3(0, -1, 0)));
            mesh.UVs.Add(new Vector2((float)(Math.Cos(angle) / 2 + 0.5), (float)(Math.Sin(angle) / 2 + 0.5)));
        }

        for (int i = 0; i < segments; i++)
        {
            mesh.Indices.Add(bottomCenter);
            mesh.Indices.Add(bottomCenter + i + 2);
            mesh.Indices.Add(bottomCenter + i + 1);
        }

        return mesh;
    }

    public Mesh CreatePost(float diameter, float height, int segments = 12)
    {
        var cylinder = CreateCylinder(diameter, height, segments);
        cylinder.Name = "Post";
        return cylinder;
    }
}
