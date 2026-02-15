namespace NorwegianRoadSigns.Models;

public class Vector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3() { }

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3 Zero => new(0, 0, 0);
    public static Vector3 UnitX => new(1, 0, 0);
    public static Vector3 UnitY => new(0, 1, 0);
    public static Vector3 UnitZ => new(0, 0, 1);

    public Vector3 Normalize()
    {
        var length = (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        if (length > 0)
        {
            return new Vector3(X / length, Y / length, Z / length);
        }
        return Zero;
    }

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3 operator *(Vector3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);
    public static Vector3 operator /(Vector3 a, float s) => new(a.X / s, a.Y / s, a.Z / s);
}

public class Vector2
{
    public float X { get; set; }
    public float Y { get; set; }

    public Vector2() { }

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static Vector2 Zero => new(0, 0);
    public static Vector2 One => new(1, 1);
}

public class Mesh
{
    public List<Vector3> Vertices { get; set; } = new();
    public List<Vector3> Normals { get; set; } = new();
    public List<Vector2> UVs { get; set; } = new();
    public List<int> Indices { get; set; } = new();
    public string Name { get; set; } = string.Empty;
}

public class SignMesh
{
    public Mesh Post { get; set; } = new();
    public Mesh Bracket { get; set; } = new();
    public List<SignPlateMesh> Plates { get; set; } = new();
    public string? TexturePath { get; set; }
    public Dictionary<string, byte[]> Textures { get; set; } = new();
}

public class SignPlateMesh
{
    public Mesh Mesh { get; set; } = new();
    public SignPlate Plate { get; set; } = new();
    public byte[]? Texture { get; set; }
    public string? TextureName { get; set; }
    public SignColor Color { get; set; }
    public double OffsetX { get; set; }  // X-axis (left/right of post)
    public double OffsetY { get; set; }  // Y-axis (front/back of post)
    public double OffsetZ { get; set; }  // Z-axis (height on post)
    public bool Rotate90 { get; set; }  // Whether to rotate 90° around Z axis
}
