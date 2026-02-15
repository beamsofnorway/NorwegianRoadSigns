using System.Text;
using System.Xml.Linq;
using NorwegianRoadSigns.Models;

namespace NorwegianRoadSigns.Exporters;

public class DaeWriter
{
    private const string DaeNamespace = "http://www.collada.org/2005/11/COLLADASchema";
    private const float ScaleFactor = 1.0f; // Mesh is already in meters

    public string GenerateDae(SignMesh signMesh, string? texturePath = null)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(XName.Get("COLLADA", DaeNamespace),
                new XAttribute("version", "1.4.1"),
                new XAttribute(XNamespace.Xmlns + "xml", "http://www.w3.org/XML/1998/namespace"),
                GenerateAsset(),
                GenerateLibraryImages(signMesh, texturePath),
                GenerateLibraryMaterials(),
                GenerateLibraryEffects(),
                GenerateLibraryGeometries(signMesh),
                GenerateLibraryVisualScenes(signMesh)
            )
        );

        return doc.ToString();
    }

    public Stream GenerateDaeStream(SignMesh signMesh, string? texturePath = null)
    {
        var dae = GenerateDae(signMesh, texturePath);
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(dae);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    public async Task WriteDaeAsync(SignMesh signMesh, string filePath, string? texturePath = null)
    {
        var dae = GenerateDae(signMesh, texturePath);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        await File.WriteAllTextAsync(filePath, dae, Encoding.UTF8);
    }

    private XElement GenerateAsset()
    {
        return new XElement(XName.Get("asset", DaeNamespace),
            new XElement(XName.Get("created", DaeNamespace), DateTime.UtcNow.ToString("o")),
            new XElement(XName.Get("modified", DaeNamespace), DateTime.UtcNow.ToString("o")),
            new XElement(XName.Get("up_axis", DaeNamespace), "Z_UP")
        );
    }

    private XElement GenerateLibraryImages(SignMesh signMesh, string? texturePath)
    {
        var images = new List<XElement>();
        
        // Add texture image
        if (!string.IsNullOrEmpty(texturePath) && File.Exists(texturePath))
        {
            images.Add(new XElement(XName.Get("image", DaeNamespace),
                new XAttribute("id", "sign_texture"),
                new XElement(XName.Get("init_from", DaeNamespace), Path.GetFileName(texturePath))
            ));
        }
        
        // Add any generated textures
        foreach (var texture in signMesh.Textures)
        {
            images.Add(new XElement(XName.Get("image", DaeNamespace),
                new XAttribute("id", texture.Key),
                new XElement(XName.Get("init_from", DaeNamespace), $"{texture.Key}.png")
            ));
        }

        return new XElement(XName.Get("library_images", DaeNamespace), images);
    }

    private XElement GenerateLibraryMaterials()
    {
        return new XElement(XName.Get("library_materials", DaeNamespace),
            new XElement(XName.Get("material", DaeNamespace),
                new XAttribute("id", "sign_material"),
                new XElement(XName.Get("instance_effect", DaeNamespace),
                    new XAttribute("url", "#sign_effect")
                )
            )
        );
    }

    private XElement GenerateLibraryEffects()
    {
        return new XElement(XName.Get("library_effects", DaeNamespace),
            new XElement(XName.Get("effect", DaeNamespace),
                new XAttribute("id", "sign_effect"),
                new XElement(XName.Get("profile_COMMON", DaeNamespace),
                    new XElement(XName.Get("technique", DaeNamespace),
                        new XAttribute("sid", "common"),
                        new XElement(XName.Get("phong", DaeNamespace),
                            new XElement(XName.Get("diffuse", DaeNamespace),
                                new XElement(XName.Get("color", DaeNamespace), "1 1 1 1")
                            ),
                            new XElement(XName.Get("specular", DaeNamespace),
                                new XElement(XName.Get("color", DaeNamespace), "0.2 0.2 0.2 1")
                            ),
                            new XElement(XName.Get("shininess", DaeNamespace),
                                new XElement(XName.Get("float", DaeNamespace), "64")
                            )
                        )
                    )
                )
            )
        );
    }

    private XElement GenerateLibraryGeometries(SignMesh signMesh)
    {
        var geometries = new List<XElement>();
        var meshId = 0;

        // Post mesh
        if (signMesh.Post.Vertices.Count > 0)
        {
            geometries.Add(GenerateGeometryMesh(signMesh.Post, $"post_geometry_{meshId++}"));
        }

        // Bracket mesh
        if (signMesh.Bracket.Vertices.Count > 0)
        {
            geometries.Add(GenerateGeometryMesh(signMesh.Bracket, $"bracket_geometry_{meshId++}"));
        }

        // Plate meshes
        foreach (var plate in signMesh.Plates)
        {
            if (plate.Mesh.Vertices.Count > 0)
            {
                geometries.Add(GenerateGeometryMesh(plate.Mesh, $"plate_geometry_{plate.Plate.Id}"));
            }
        }

        return new XElement(XName.Get("library_geometries", DaeNamespace), geometries);
    }

    private XElement GenerateGeometryMesh(Mesh mesh, string id)
    {
        var positions = new StringBuilder();
        var normals = new StringBuilder();
        var uvs = new StringBuilder();
        var indices = new StringBuilder();

        foreach (var v in mesh.Vertices)
        {
            positions.Append($"{v.X * ScaleFactor} {v.Y * ScaleFactor} {v.Z * ScaleFactor} ");
        }

        foreach (var n in mesh.Normals)
        {
            normals.Append($"{n.X} {n.Y} {n.Z} ");
        }

        foreach (var uv in mesh.UVs)
        {
            uvs.Append($"{uv.X} {uv.Y} ");
        }

        for (int i = 0; i < mesh.Indices.Count; i++)
        {
            indices.Append(mesh.Indices[i]);
            if (i < mesh.Indices.Count - 1)
            {
                indices.Append(i % 3 == 2 ? " " : ", ");
            }
        }

        var vertCount = mesh.Vertices.Count;

        return new XElement(XName.Get("geometry", DaeNamespace),
            new XAttribute("id", id),
            new XAttribute("name", mesh.Name),
            new XElement(XName.Get("mesh", DaeNamespace),
                // Position source
                new XElement(XName.Get("source", DaeNamespace),
                    new XAttribute("id", $"{id}-positions"),
                    new XElement(XName.Get("float_array", DaeNamespace),
                        new XAttribute("id", $"{id}-positions-array"),
                        new XAttribute("count", vertCount * 3),
                        positions.ToString().Trim()
                    ),
                    new XElement(XName.Get("technique_common", DaeNamespace),
                        new XElement(XName.Get("accessor", DaeNamespace),
                            new XAttribute("source", $"#{id}-positions-array"),
                            new XAttribute("count", vertCount),
                            new XAttribute("stride", 3),
                            new XElement(XName.Get("param", DaeNamespace),
                                new XAttribute("name", "X"),
                                new XAttribute("type", "float")
                            ),
                            new XElement(XName.Get("param", DaeNamespace),
                                new XAttribute("name", "Y"),
                                new XAttribute("type", "float")
                            ),
                            new XElement(XName.Get("param", DaeNamespace),
                                new XAttribute("name", "Z"),
                                new XAttribute("type", "float")
                            )
                        )
                    )
                ),
                // Normal source
                new XElement(XName.Get("source", DaeNamespace),
                    new XAttribute("id", $"{id}-normals"),
                    new XElement(XName.Get("float_array", DaeNamespace),
                        new XAttribute("id", $"{id}-normals-array"),
                        new XAttribute("count", vertCount * 3),
                        normals.ToString().Trim()
                    ),
                    new XElement(XName.Get("technique_common", DaeNamespace),
                        new XElement(XName.Get("accessor", DaeNamespace),
                            new XAttribute("source", $"#{id}-normals-array"),
                            new XAttribute("count", vertCount),
                            new XAttribute("stride", 3),
                            new XElement(XName.Get("param", DaeNamespace),
                                new XAttribute("name", "X"),
                                new XAttribute("type", "float")
                            ),
                            new XElement(XName.Get("param", DaeNamespace),
                                new XAttribute("name", "Y"),
                                new XAttribute("type", "float")
                            ),
                            new XElement(XName.Get("param", DaeNamespace),
                                new XAttribute("name", "Z"),
                                new XAttribute("type", "float")
                            )
                        )
                    )
                ),
                // UV source
                new XElement(XName.Get("source", DaeNamespace),
                    new XAttribute("id", $"{id}-uvs"),
                    new XElement(XName.Get("float_array", DaeNamespace),
                        new XAttribute("id", $"{id}-uvs-array"),
                        new XAttribute("count", vertCount * 2),
                        uvs.ToString().Trim()
                    ),
                    new XElement(XName.Get("technique_common", DaeNamespace),
                        new XElement(XName.Get("accessor", DaeNamespace),
                            new XAttribute("source", $"#{id}-uvs-array"),
                            new XAttribute("count", vertCount),
                            new XAttribute("stride", 2),
                            new XElement(XName.Get("param", DaeNamespace),
                                new XAttribute("name", "S"),
                                new XAttribute("type", "float")
                            ),
                            new XElement(XName.Get("param", DaeNamespace),
                                new XAttribute("name", "T"),
                                new XAttribute("type", "float")
                            )
                        )
                    )
                ),
                // Vertices
                new XElement(XName.Get("vertices", DaeNamespace),
                    new XAttribute("id", $"{id}-vertices"),
                    new XElement(XName.Get("input", DaeNamespace),
                        new XAttribute("semantic", "POSITION"),
                        new XAttribute("source", $"#{id}-positions")
                    )
                ),
                // Triangles
                new XElement(XName.Get("triangles", DaeNamespace),
                    new XAttribute("count", mesh.Indices.Count / 3),
                    new XElement(XName.Get("input", DaeNamespace),
                        new XAttribute("semantic", "VERTEX"),
                        new XAttribute("source", $"#{id}-vertices"),
                        new XAttribute("offset", 0)
                    ),
                    new XElement(XName.Get("input", DaeNamespace),
                        new XAttribute("semantic", "NORMAL"),
                        new XAttribute("source", $"#{id}-normals"),
                        new XAttribute("offset", 1)
                    ),
                    new XElement(XName.Get("input", DaeNamespace),
                        new XAttribute("semantic", "TEXCOORD"),
                        new XAttribute("source", $"#{id}-uvs"),
                        new XAttribute("offset", 2)
                    ),
                    new XElement(XName.Get("p", DaeNamespace),
                        GenerateIndicesString(mesh)
                    )
                )
            )
        );
    }

    private string GenerateIndicesString(Mesh mesh)
    {
        var sb = new StringBuilder();
        
        // For COLLADA, each triangle needs 3 indices, each with offset for position (0), normal (1), texcoord (2)
        for (int i = 0; i < mesh.Indices.Count; i++)
        {
            var idx = mesh.Indices[i];
            // Use the same index for all inputs (vertices, normals, UVs share the same index)
            var normalIdx = idx;
            var uvIdx = idx;
            
            sb.Append($"{idx} {normalIdx} {uvIdx}");
            
            if (i < mesh.Indices.Count - 1)
            {
                sb.Append(" ");
            }
        }
        return sb.ToString();
    }

    private XElement GenerateLibraryVisualScenes(SignMesh signMesh)
    {
        var nodes = new List<XElement>();
        var nodeId = 0;

        // Root node
        nodes.Add(new XElement(XName.Get("node", DaeNamespace),
            new XAttribute("id", "sign_root"),
            new XAttribute("name", "sign_root"),
            new XAttribute("type", "NODE")
        ));

        // Post node
        if (signMesh.Post.Vertices.Count > 0)
        {
            nodes.Add(new XElement(XName.Get("node", DaeNamespace),
                new XAttribute("id", "post"),
                new XAttribute("name", "post"),
                new XAttribute("type", "NODE"),
                new XElement(XName.Get("instance_geometry", DaeNamespace),
                    new XAttribute("url", $"#post_geometry_{nodeId++}"),
                    new XElement(XName.Get("bind_material", DaeNamespace),
                        new XElement(XName.Get("technique_common", DaeNamespace),
                            new XElement(XName.Get("instance_material", DaeNamespace),
                                new XAttribute("symbol", "material0"),
                                new XAttribute("target", "#sign_material")
                            )
                        )
                    )
                )
            ));
        }

        // Bracket node - position at top of post
        if (signMesh.Bracket.Vertices.Count > 0)
        {
            nodes.Add(new XElement(XName.Get("node", DaeNamespace),
                new XAttribute("id", "bracket"),
                new XAttribute("name", "bracket"),
                new XAttribute("type", "NODE"),
                new XElement(XName.Get("translate", DaeNamespace),
                    new XAttribute("sid", "translation"),
                    "0 0 2.0"  // Bracket at top of post
                ),
                new XElement(XName.Get("instance_geometry", DaeNamespace),
                    new XAttribute("url", $"#bracket_geometry_{nodeId++}")
                )
            ));
        }

        // Plate nodes - mesh is already positioned correctly
        foreach (var plate in signMesh.Plates)
        {
            if (plate.Mesh.Vertices.Count > 0)
            {
                nodes.Add(new XElement(XName.Get("node", DaeNamespace),
                    new XAttribute("id", $"plate_{plate.Plate.Id}"),
                    new XAttribute("name", $"plate_{plate.Plate.SignNumber ?? plate.Plate.Id.ToString()}"),
                    new XAttribute("type", "NODE"),
                    new XElement(XName.Get("instance_geometry", DaeNamespace),
                        new XAttribute("url", $"#plate_geometry_{plate.Plate.Id}"),
                        new XElement(XName.Get("bind_material", DaeNamespace),
                            new XElement(XName.Get("technique_common", DaeNamespace),
                                new XElement(XName.Get("instance_material", DaeNamespace),
                                    new XAttribute("symbol", "material0"),
                                    new XAttribute("target", "#sign_material")
                                )
                            )
                        )
                    )
                ));
            }
        }

        return new XElement(XName.Get("library_visual_scenes", DaeNamespace),
            new XElement(XName.Get("visual_scene", DaeNamespace),
                new XAttribute("id", "Scene"),
                new XAttribute("name", "Scene"),
                nodes
            )
        );
    }
}
