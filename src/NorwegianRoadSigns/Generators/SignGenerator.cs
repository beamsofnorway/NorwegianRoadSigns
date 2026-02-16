using NorwegianRoadSigns.Clients;
using NorwegianRoadSigns.Data;
using NorwegianRoadSigns.Exporters;
using NorwegianRoadSigns.Generators;
using NorwegianRoadSigns.Models;
using System.Text.RegularExpressions;

namespace NorwegianRoadSigns.Generators;

public class SignGenerator
{
    private readonly NvdbClient _nvdbClient;
    private readonly SignDefinitionDb _definitionDb;
    private readonly MeshGenerator _meshGenerator;
    private readonly TextureGenerator _textureGenerator;
    private readonly DaeWriter _daeWriter;
    private bool _definitionsLoaded;
    private string? _signsBaseDirectory;

    public SignGenerator(NvdbClient? nvdbClient = null, string? cachePath = null, string? signsBaseDirectory = null)
    {
        _nvdbClient = nvdbClient ?? new NvdbClient();
        _definitionDb = new SignDefinitionDb(cachePath);
        _meshGenerator = new MeshGenerator();
        _textureGenerator = new TextureGenerator();
        _daeWriter = new DaeWriter();
        _signsBaseDirectory = signsBaseDirectory ?? "signs";
    }

    public void SetSignsBaseDirectory(string path)
    {
        _signsBaseDirectory = path;
    }

    public async Task LoadDefinitionsAsync(bool forceRefresh = false)
    {
        if (_definitionsLoaded && !forceRefresh)
        {
            return;
        }

        // Try loading from embedded JSON first (sign_definitions.json in Data folder)
        _definitionDb.LoadFromCache();

        // If not loaded, the file might need to be copied to output directory
        // But we have the data, so it should work
        if (!_definitionDb.IsLoaded)
        {
            // Try loading from embedded resource or alternative paths
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var altPaths = new[]
            {
                Path.Combine(baseDir, "data", "sign_definitions.json"),
                Path.Combine(baseDir, "..", "..", "..", "..", "src", "NorwegianRoadSigns", "Data", "sign_definitions.json"),
                Path.Combine(baseDir, "sign_definitions.json")
            };

            foreach (var altPath in altPaths)
            {
                if (File.Exists(altPath))
                {
                    var db = new SignDefinitionDb(altPath);
                    db.LoadFromCache();
                    if (db.IsLoaded)
                    {
                        foreach (var def in db.GetAll())
                        {
                            _definitionDb.Load(new[] { def });
                        }
                        break;
                    }
                }
            }
        }

        // If still not loaded, create basic definitions
        if (!_definitionDb.IsLoaded)
        {
            CreateDefaultDefinitions();
        }

        _definitionsLoaded = true;
    }

    public async Task<SignMesh> CreateFromNvdbAsync(int signPointId)
    {
        var signPoint = await _nvdbClient.GetSignPointAsync(signPointId);
        if (signPoint == null)
        {
            throw new ArgumentException($"Sign point {signPointId} not found");
        }

        return CreateFromSignPoint(signPoint);
    }

    public SignMesh CreateFromSignPoint(SignPoint signPoint)
    {
        var signMesh = new SignMesh();

        // Load definitions if needed
        if (!_definitionsLoaded)
        {
            _definitionDb.LoadFromCache();
            if (!_definitionDb.IsLoaded)
            {
                // Create basic definitions if none loaded
                CreateDefaultDefinitions();
            }
            _definitionsLoaded = true;
        }

        // Group plates by FaceDirection to handle two-sided signs
        var plateGroups = signPoint.Plates
            .GroupBy(p => p.FaceDirection ?? "default")
            .ToList();
        
        // Check which directions we have
        var hasParallel = plateGroups.Any(g => g.Key.Contains("Parallelt"));
        var hasMot = plateGroups.Any(g => g.Key.Contains("mot") && !g.Key.Contains("Parallelt"));
        var hasI = plateGroups.Any(g => !g.Key.Contains("mot") && !g.Key.Contains("Parallelt"));
        
        // Calculate plate counts per column
        var parallelCount = plateGroups.Where(g => g.Key.Contains("Parallelt")).Sum(g => g.Count());
        var motCount = plateGroups.Where(g => g.Key.Contains("mot") && !g.Key.Contains("Parallelt")).Sum(g => g.Count());
        var iCount = plateGroups.Where(g => !g.Key.Contains("mot") && !g.Key.Contains("Parallelt")).Sum(g => g.Count());
        
        // Get individual plate heights from NVDB data
        // If plate height is missing, use 2.14x text height (15/7 ratio)
        double GetPlateHeight(SignPlate plate)
        {
            if (plate.Height > 0) return plate.Height;
            
            // Use 2.14x text height (15/7 = 2.14)
            if (plate.TextHeight > 0)
            {
                return (plate.TextHeight * 2.14)*0.001;
            }
            
            return 567; // mm default (1700mm / 3 for route signs)
        }
        
        // Get heights for each plate and calculate average for column height estimation
        var plateHeights = signPoint.Plates.Select(p => GetPlateHeight(p) / 1000.0).ToList();
        var avgHeightPerPlate = plateHeights.Count > 0 ? plateHeights.Average() : 0.567;
        
        // Calculate gap between plates
        var gap = 0.05f;
        
        // Calculate height needed for each column
        var parallelHeight = parallelCount > 0 ? parallelCount * (avgHeightPerPlate + gap) : 0;
        var motHeight = motCount > 0 ? motCount * (avgHeightPerPlate + gap) : 0;
        var iHeight = iCount > 0 ? iCount * (avgHeightPerPlate + gap) : 0;
        
        // The column that extends lowest determines the total sign height
        var maxColumnHeight = Math.Max(parallelHeight, Math.Max(motHeight, iHeight));
        
        // Post needs to be tall enough for the signs plus clearance
        var clearance = 1.0f; // 1m from ground to bottom of lowest sign
        var topClearance = 0.3f; // Extra at top
        var postHeight = (float)(maxColumnHeight + clearance + topClearance);
        postHeight = Math.Max(postHeight, 2.5f); // Minimum 2.5m
        postHeight = Math.Min(postHeight, 4.0f); // Maximum 4m
        
        // Generate post
        var postDiameter = 0.05f; // Default 50mm
        if (signPoint.PostType == "Stolpe 88,9 mm (3\")") postDiameter = 0.089f;
        signMesh.Post = _meshGenerator.CreatePost(postDiameter, postHeight);

        // Generate bracket (simplified)
        signMesh.Bracket = _meshGenerator.CreateRectangle(0.05f, 0.3f, 0.02f);
        
        // Calculate available height for signs (top of post)
        var maxSignTop = postHeight;
        
        // Calculate scale factor if needed
        var totalHeightNeeded = Math.Max(parallelHeight, Math.Max(motHeight, iHeight));
        var availableHeight = maxSignTop - 1.0f;
        float scaleFactor = totalHeightNeeded > availableHeight ? (float)(availableHeight / totalHeightNeeded) : 1.0f;
        scaleFactor = Math.Min(scaleFactor, 1.0f);
        
        // Post radius for separating plates horizontally
        var postRadius = postDiameter / 2;
        
        // First, calculate actual heights of all parallel plates to know where to start mot/i
        float totalParallelHeight = 0;
        if (parallelCount > 0)
        {
            var parallelGroup = plateGroups.FirstOrDefault(g => g.Key.Contains("Parallelt"));
            if (parallelGroup != null)
            {
                foreach (var plate in parallelGroup.OrderBy(p => p.PlacementCode))
                {
                    var h = (float)(GetPlateHeight(plate) * scaleFactor);
                    totalParallelHeight += h + (float)(0.05 * scaleFactor);
                }
            }
        }
        
        // Calculate starting heights
        float parallelStart = maxSignTop;
        float motStart = maxSignTop;
        float iStart = maxSignTop;
        
        if (parallelCount > 0 && (motCount > 0 || iCount > 0))
        {
            motStart = maxSignTop - totalParallelHeight;
            iStart = maxSignTop - totalParallelHeight;
        }
        
        // If there's no mot/i, parallel can be at top. Otherwise, parallel is at top only.
        // (Current logic: parallel always at top, mot/i below if parallel exists)
        
        // Process each side of the sign
        foreach (var group in plateGroups)
        {
            float startingHeight;
            float posX = 0, posY = 0;
            
            if (group.Key.Contains("Parallelt"))
            {
                // Parallel: at top, in front of post (+Y)
                posX = 0;
                posY = (float)postRadius;
                startingHeight = parallelStart;
            }
            else if (group.Key.Contains("mot"))
            {
                // mot: below parallel, right side of post (+X)
                posX = (float)(postRadius + 0.01);
                posY = 0;
                startingHeight = motStart;
            }
            else
            {
                // i: below parallel, left side of post (-X)
                posX = -(float)(postRadius + 0.01);
                posY = 0;
                startingHeight = iStart;
            }
            
            var currentHeight = startingHeight;
            var sortedPlates = group.OrderBy(p => p.PlacementCode).ToList();
            
            foreach (var plate in sortedPlates)
            {
                var parsedText = ParsePlateText(plate.Text);

                var definition = _definitionDb.Get(plate.SignNumber ?? "");
                double rawWidth = plate.Width > 0 ? Math.Min(plate.Width / 1000.0, 1.5) : (definition?.DefaultWidth / 1000.0 ?? 0.6);
                float width = (float)rawWidth;
                float height = (float)(GetPlateHeight(plate) * scaleFactor);
                height = Math.Max(height, 0.2f);

                Mesh mesh;
                var geoType = definition?.GeometryType ?? GeometryType.Rectangle;
                
                // Create mesh at origin with correct dimensions
                // Parallel: width in X, height in Z
                // mot/i: width in Y, height in Z
                switch (geoType)
                {
                    case GeometryType.Circle:
                        mesh = _meshGenerator.CreateCircle(Math.Min(width, height), 32, 0.02f, 0, 0);
                        break;
                    case GeometryType.Triangle:
                        mesh = _meshGenerator.CreateTriangle(Math.Max(width, height), 0.02f, 0, 0);
                        break;
                    case GeometryType.Octagon:
                        mesh = _meshGenerator.CreateOctagon(Math.Min(width, height), 0.02f, 0, 0);
                        break;
                    default:
                        // New staged mesh path for 713 directional signs.
                        // Legacy rectangle remains fallback for every other sign.
                        if ((plate.SignNumber ?? string.Empty).StartsWith("713", StringComparison.OrdinalIgnoreCase))
                        {
                            var arrowRight = !(plate.Text?.Contains("<-") ?? false);
                            mesh = _meshGenerator.CreateDirectionalRectangle713(width, height, arrowRight, 0.02f, 0, 0);
                        }
                        else
                        {
                            // Always create width in X, height in Z - we'll manually rotate for mot/i
                            mesh = _meshGenerator.CreateRectangle(width, height, 0.02f, 0, 0);
                        }
                        break;
                }
                
                // Now manually position each vertex
                // Parallel: no rotation (width in X, height in Z)
                // mot/i: rotate 90° (swap X and Y so width goes in Y)
                bool isParallel = group.Key.Contains("Parallelt");
                bool shouldRotate = !isParallel;
                
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    var v = mesh.Vertices[i];
                    float x, y, z;
                    
                    if (shouldRotate)
                    {
                        // Swap X and Y for mot/i
                        x = v.Y;
                        y = v.X;
                        z = v.Z;
                    }
                    else
                    {
                        x = v.X;
                        y = v.Y;
                        z = v.Z;
                    }
                    
                    // Position: X/Y offset, Z = bottom of sign
                    mesh.Vertices[i] = new Vector3(x + posX, y + posY, z + currentHeight - height / 2);
                }

                var textureName = $"sign_{plate.Id}";
                
                var texture = BuildPlateTexture(plate, definition, parsedText);

                var plateMesh = new SignPlateMesh
                {
                    Mesh = mesh,
                    Plate = plate,
                    Color = plate.Color,
                    Texture = texture,
                    TextureName = textureName
                };

                signMesh.Plates.Add(plateMesh);
                signMesh.Textures[textureName] = texture;

                currentHeight -= (float)(height + 0.05 * scaleFactor);
            }
        }

        return signMesh;
    }

    public string ToDae(SignMesh signMesh, string? filePath = null)
    {
        return _daeWriter.GenerateDae(signMesh, filePath);
    }

    public Stream ToDaeStream(SignMesh signMesh)
    {
        return _daeWriter.GenerateDaeStream(signMesh);
    }

    public async Task ToDaeAsync(SignMesh signMesh, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Save texture files
        foreach (var texture in signMesh.Textures)
        {
            var texturePath = Path.Combine(directory, $"{texture.Key}.png");
            await File.WriteAllBytesAsync(texturePath, texture.Value);
        }

        // Save DAE file
        await _daeWriter.WriteDaeAsync(signMesh, filePath);
    }

    public async Task<string> GenerateFromNvdbAsync(int signPointId, string? outputPath = null)
    {
        var signMesh = await CreateFromNvdbAsync(signPointId);
        
        if (string.IsNullOrEmpty(outputPath))
        {
            outputPath = $"sign_{signPointId}.dae";
        }

        await ToDaeAsync(signMesh, outputPath);
        
        return outputPath;
    }

    public async Task<SignMesh> GenerateFromNvdbWithTexturesAsync(int signPointId)
    {
        return await CreateFromNvdbAsync(signPointId);
    }

    private void CreateDefaultDefinitions()
    {
        // Comprehensive default definitions for common sign types
        var defaults = new List<SignDefinition>
        {
            // Fareskilt (Warning signs) - Triangle
            new() { SignNumber = "100", Description = "Farlig sving", Category = SignCategory.Fareskilt, GeometryType = GeometryType.Triangle, DefaultWidth = 600, DefaultHeight = 700, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            new() { SignNumber = "102", Description = "Farlige svinger", Category = SignCategory.Fareskilt, GeometryType = GeometryType.Triangle, DefaultWidth = 600, DefaultHeight = 700, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            new() { SignNumber = "104", Description = "Bratt bakke", Category = SignCategory.Fareskilt, GeometryType = GeometryType.Triangle, DefaultWidth = 700, DefaultHeight = 800, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            new() { SignNumber = "106", Description = "Smalere veg", Category = SignCategory.Fareskilt, GeometryType = GeometryType.Triangle, DefaultWidth = 700, DefaultHeight = 800, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            new() { SignNumber = "108", Description = "Ujevn veg", Category = SignCategory.Fareskilt, GeometryType = GeometryType.Triangle, DefaultWidth = 700, DefaultHeight = 800, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            new() { SignNumber = "110", Description = "Vegarbeid", Category = SignCategory.Fareskilt, GeometryType = GeometryType.Triangle, DefaultWidth = 700, DefaultHeight = 800, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            new() { SignNumber = "114", Description = "Rasfare", Category = SignCategory.Fareskilt, GeometryType = GeometryType.Triangle, DefaultWidth = 800, DefaultHeight = 900, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            new() { SignNumber = "116", Description = "Glatt kjørebane", Category = SignCategory.Fareskilt, GeometryType = GeometryType.Triangle, DefaultWidth = 800, DefaultHeight = 900, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            new() { SignNumber = "142", Description = "Barn", Category = SignCategory.Fareskilt, GeometryType = GeometryType.Triangle, DefaultWidth = 700, DefaultHeight = 800, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            new() { SignNumber = "144", Description = "Syklende", Category = SignCategory.Fareskilt, GeometryType = GeometryType.Triangle, DefaultWidth = 700, DefaultHeight = 800, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            new() { SignNumber = "146", Description = "Dyr", Category = SignCategory.Fareskilt, GeometryType = GeometryType.Triangle, DefaultWidth = 700, DefaultHeight = 800, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            
            // Vikeplikt og forkjør - Diamond/Octagon
            new() { SignNumber = "202", Description = "Vikeplikt", Category = SignCategory.VikepliktOgForkjør, GeometryType = GeometryType.Octagon, DefaultWidth = 750, DefaultHeight = 750, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Rød },
            new() { SignNumber = "204", Description = "Stopp", Category = SignCategory.VikepliktOgForkjør, GeometryType = GeometryType.Octagon, DefaultWidth = 750, DefaultHeight = 750, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Rød },
            new() { SignNumber = "206", Description = "Forkjørsveg", Category = SignCategory.VikepliktOgForkjør, GeometryType = GeometryType.Diamond, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            new() { SignNumber = "208", Description = "Slutt på forkjørsveg", Category = SignCategory.VikepliktOgForkjør, GeometryType = GeometryType.Diamond, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            
            // Forbudsskilt - Circle
            new() { SignNumber = "302", Description = "Innkjøring forbudt", Category = SignCategory.Forbudsskilt, GeometryType = GeometryType.Circle, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Rød },
            new() { SignNumber = "362", Description = "Fartsgrense", Category = SignCategory.Forbudsskilt, GeometryType = GeometryType.Circle, DefaultWidth = 600, DefaultHeight = 800, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Rød, HasText = true },
            new() { SignNumber = "364", Description = "Slutt på særskilt fartsgrense", Category = SignCategory.Forbudsskilt, GeometryType = GeometryType.Circle, DefaultWidth = 600, DefaultHeight = 800, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Rød },
            new() { SignNumber = "370", Description = "Stans forbudt", Category = SignCategory.Forbudsskilt, GeometryType = GeometryType.Circle, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Rød },
            new() { SignNumber = "372", Description = "Parkering forbudt", Category = SignCategory.Forbudsskilt, GeometryType = GeometryType.Circle, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Rød },
            new() { SignNumber = "312", Description = "Breddegrense", Category = SignCategory.Forbudsskilt, GeometryType = GeometryType.Circle, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Rød, HasText = true },
            new() { SignNumber = "314", Description = "Høydegrense", Category = SignCategory.Forbudsskilt, GeometryType = GeometryType.Circle, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Rød, HasText = true },
            new() { SignNumber = "316", Description = "Lengdegrense", Category = SignCategory.Forbudsskilt, GeometryType = GeometryType.Circle, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Rød, HasText = true },
            new() { SignNumber = "318", Description = "Totalvektgrense", Category = SignCategory.Forbudsskilt, GeometryType = GeometryType.Circle, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Rød, HasText = true },
            
            // Påbudsskilt - Circle blue
            new() { SignNumber = "402", Description = "Påbudt kjøreretning", Category = SignCategory.Påbudsskilt, GeometryType = GeometryType.Circle, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Blå, BorderColor = SignColor.Svart },
            new() { SignNumber = "404", Description = "Påbudt kjørefelt", Category = SignCategory.Påbudsskilt, GeometryType = GeometryType.Circle, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Blå, BorderColor = SignColor.Svart },
            new() { SignNumber = "406", Description = "Påbudt rundkjøring", Category = SignCategory.Påbudsskilt, GeometryType = GeometryType.Circle, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Blå, BorderColor = SignColor.Svart },
            new() { SignNumber = "408", Description = "Påbudt kjøreretning i rundkjøring", Category = SignCategory.Påbudsskilt, GeometryType = GeometryType.Circle, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Blå, BorderColor = SignColor.Svart },
            
            // Opplysningsskilt - Rectangle
            new() { SignNumber = "502", Description = "Motorveg", Category = SignCategory.Opplysningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 400, DefaultHeight = 400, PrimaryColor = SignColor.Blå, BorderColor = SignColor.Hvit },
            new() { SignNumber = "504", Description = "Motorvei", Category = SignCategory.Opplysningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 400, DefaultHeight = 400, PrimaryColor = SignColor.Blå, BorderColor = SignColor.Hvit },
            new() { SignNumber = "506", Description = "Motortrafikkveg", Category = SignCategory.Opplysningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 400, DefaultHeight = 400, PrimaryColor = SignColor.Blå, BorderColor = SignColor.Hvit },
            new() { SignNumber = "520", Description = "Parkering", Category = SignCategory.Opplysningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 400, DefaultHeight = 400, PrimaryColor = SignColor.Blå, BorderColor = SignColor.Svart },
            new() { SignNumber = "540", Description = "Gatetun", Category = SignCategory.Opplysningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 500, DefaultHeight = 500, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            new() { SignNumber = "548", Description = "Gågate", Category = SignCategory.Opplysningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 500, DefaultHeight = 500, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            
            // Vegvisningsskilt - Rectangle (with text)
            new() { SignNumber = "701", Description = "Europaveg", Category = SignCategory.Vegvisningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Gul, BorderColor = SignColor.Svart, HasText = true },
            new() { SignNumber = "703", Description = "Riksveg", Category = SignCategory.Vegvisningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Gul, BorderColor = SignColor.Svart, HasText = true },
            new() { SignNumber = "704", Description = "Fylkesveg", Category = SignCategory.Vegvisningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Gul, BorderColor = SignColor.Svart, HasText = true },
            new() { SignNumber = "713", Description = "Vanlig vegviser", Category = SignCategory.Vegvisningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 1500, DefaultHeight = 1000, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart, HasText = true },
            new() { SignNumber = "714", Description = "Avstand til sted", Category = SignCategory.Vegvisningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 800, DefaultHeight = 600, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart, HasText = true },
            new() { SignNumber = "716", Description = "Retningsviser", Category = SignCategory.Vegvisningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 600, DefaultHeight = 800, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart, HasText = true },
            new() { SignNumber = "801", Description = "Stedsnavn", Category = SignCategory.Vegvisningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 800, DefaultHeight = 300, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart, HasText = true },
            new() { SignNumber = "820", Description = "Veganviser", Category = SignCategory.Vegvisningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 1500, DefaultHeight = 1000, PrimaryColor = SignColor.Gul, BorderColor = SignColor.Svart, HasText = true },
            new() { SignNumber = "830", Description = "Kartmerke", Category = SignCategory.Vegvisningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 800, DefaultHeight = 600, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart, HasText = true },
            new() { SignNumber = "840", Description = "Omkjøring", Category = SignCategory.Vegvisningsskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 600, DefaultHeight = 600, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart },
            
            // Serviceskilt
            new() { SignNumber = "560", Description = "Buss", Category = SignCategory.Serviceskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 400, DefaultHeight = 400, PrimaryColor = SignColor.Blå, BorderColor = SignColor.Svart },
            new() { SignNumber = "562", Description = "Trikk", Category = SignCategory.Serviceskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 400, DefaultHeight = 400, PrimaryColor = SignColor.Blå, BorderColor = SignColor.Svart },
            new() { SignNumber = "524", Description = "Handicap", Category = SignCategory.Serviceskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 400, DefaultHeight = 400, PrimaryColor = SignColor.Blå, BorderColor = SignColor.Svart },
            new() { SignNumber = "526", Description = "Lading", Category = SignCategory.Serviceskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 400, DefaultHeight = 400, PrimaryColor = SignColor.Blå, BorderColor = SignColor.Svart },
            
            // Underskilt
            new() { SignNumber = "901", Description = "Avstand til sted", Category = SignCategory.Underskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 600, DefaultHeight = 200, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart, HasText = true },
            new() { SignNumber = "902", Description = "Underskilt", Category = SignCategory.Underskilt, GeometryType = GeometryType.Rectangle, DefaultWidth = 600, DefaultHeight = 200, PrimaryColor = SignColor.Hvit, BorderColor = SignColor.Svart, HasText = true },
        };
        
        _definitionDb.Load(defaults);
    }

    public void Dispose()
    {
        _nvdbClient.Dispose();
    }

    private byte[] BuildPlateTexture(SignPlate plate, SignDefinition? definition, ParsedPlateText parsedText)
    {
        var signNumber = plate.SignNumber ?? string.Empty;
        var definitionForCategory = definition ?? new SignDefinition
        {
            BorderColor = SignColor.Svart,
            Category = SignCategory.Other,
            DefaultWidth = 600,
            DefaultHeight = 600,
            GeometryType = GeometryType.Rectangle
        };

        var aspectRatio = (float)(definitionForCategory.DefaultHeight / definitionForCategory.DefaultWidth);
        var targetWidth = 1024;
        var targetHeight = (int)(targetWidth * aspectRatio);

        var fontHeight = plate.TextHeight > 0
            ? plate.TextHeight
            : definitionForCategory.DefaultHeight / 2.14;

        var category = definitionForCategory.Category;
        string? imagePath = null;

        if (!string.IsNullOrEmpty(_signsBaseDirectory))
        {
            imagePath = _textureGenerator.FindSignImage(_signsBaseDirectory, signNumber, category);

            if (!string.IsNullOrEmpty(imagePath))
            {
                var imgDims = _textureGenerator.GetImageDimensions(imagePath);
                if (imgDims.width > 0 && imgDims.height > 0)
                {
                    targetWidth = 1024;
                    targetHeight = (int)(targetWidth * (float)imgDims.height / imgDims.width);
                }
            }
        }

        if (TryCreateTemplateTexture(
                plate,
                signNumber,
                parsedText,
                definitionForCategory,
                targetWidth,
                targetHeight,
                fontHeight,
                out var templateTexture))
        {
            return templateTexture;
        }

        return CreateLegacyPlateTexture(
            plate,
            parsedText,
            definitionForCategory,
            targetWidth,
            targetHeight,
            fontHeight,
            imagePath);
    }

    private bool TryCreateTemplateTexture(
        SignPlate plate,
        string signNumber,
        ParsedPlateText parsedText,
        SignDefinition definition,
        int targetWidth,
        int targetHeight,
        double fontHeight,
        out byte[] texture)
    {
        texture = Array.Empty<byte>();

        // New templated path (first rollout): 713 one-line directional signs.
        // Keep legacy path as fallback for all other sign families and layouts.
        if (!signNumber.StartsWith("713", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(parsedText.DisplayText) || parsedText.DisplayText.Contains("\n"))
        {
            return false;
        }

        var arrowRight = !(plate.Text?.Contains("<-") ?? false);
        texture = _textureGenerator.CreateTemplate713OneLineTexture(
            definition,
            plate.Color,
            parsedText.DisplayText,
            targetWidth,
            targetHeight,
            fontHeight,
            arrowRight);

        return texture.Length > 0;
    }

    private byte[] CreateLegacyPlateTexture(
        SignPlate plate,
        ParsedPlateText parsedText,
        SignDefinition definitionForCategory,
        int targetWidth,
        int targetHeight,
        double fontHeight,
        string? imagePath)
    {
        var hasText = !string.IsNullOrEmpty(parsedText.DisplayText);
        var hasPrebakedElements = parsedText.ElementTokens.Count > 0;

        if (!string.IsNullOrEmpty(_signsBaseDirectory))
        {
            if (hasText || hasPrebakedElements)
            {
                byte[] baseTexture;

                if (!string.IsNullOrEmpty(imagePath))
                {
                    baseTexture = _textureGenerator.ResizeImageFromFile(imagePath, targetWidth, targetHeight);
                }
                else
                {
                    baseTexture = _textureGenerator.CreateSignTextureWithText(
                        definitionForCategory,
                        plate.Color,
                        null,
                        targetWidth,
                        targetHeight,
                        fontHeight);
                }

                if (hasPrebakedElements)
                {
                    baseTexture = CompositePrebakedElements(
                        baseTexture,
                        parsedText.ElementTokens,
                        targetWidth,
                        targetHeight);
                }

                if (hasText)
                {
                    var textTexture = _textureGenerator.GenerateTransparentTextTexture(
                        parsedText.DisplayText,
                        fontHeight,
                        SignColor.Svart,
                        targetWidth,
                        targetHeight / 2,
                        definitionForCategory.DefaultHeight);

                    baseTexture = _textureGenerator.CompositeTextures(baseTexture, textTexture, 0, (targetHeight - targetHeight / 2) / 2);
                }

                return baseTexture;
            }

            if (!string.IsNullOrEmpty(imagePath))
            {
                return _textureGenerator.ResizeImageFromFile(imagePath, targetWidth, targetHeight);
            }
        }

        return _textureGenerator.CreateSignTextureWithText(
            definitionForCategory,
            plate.Color,
            parsedText.DisplayText,
            targetWidth,
            targetHeight,
            fontHeight);
    }

    private static ParsedPlateText ParsePlateText(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return new ParsedPlateText(string.Empty, []);
        }

        var pattern = new Regex(@"@(?<code>\d+(?:\.\d+)?)@(?:\$(?<variant>\d+)\$)?");
        var tokens = pattern
            .Matches(rawText)
            .Select(m =>
            {
                var code = m.Groups["code"].Value;
                var variant = m.Groups["variant"].Success ? m.Groups["variant"].Value : null;
                return string.IsNullOrWhiteSpace(variant) ? code : $"{code}_{variant}";
            })
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var displayText = pattern.Replace(rawText, " ");
        displayText = Regex.Replace(displayText, @"\s+", " ").Trim();

        return new ParsedPlateText(displayText, tokens);
    }

    private byte[] CompositePrebakedElements(
        byte[] baseTexture,
        IReadOnlyList<string> elementTokens,
        int targetWidth,
        int targetHeight)
    {
        if (string.IsNullOrWhiteSpace(_signsBaseDirectory) || elementTokens.Count == 0)
        {
            return baseTexture;
        }

        var areaHeight = targetHeight / 2;
        var maxElementHeight = (int)(areaHeight * 0.7);
        var maxElementWidth = Math.Max(32, targetWidth / 5);

        var overlays = new List<(byte[] bytes, int width, int height)>();
        foreach (var token in elementTokens)
        {
            var elementPath = _textureGenerator.FindSignElementImage(_signsBaseDirectory, token);
            if (string.IsNullOrEmpty(elementPath))
            {
                continue;
            }

            var dims = _textureGenerator.GetImageDimensions(elementPath);
            if (dims.width <= 0 || dims.height <= 0)
            {
                continue;
            }

            var scale = Math.Min((double)maxElementWidth / dims.width, (double)maxElementHeight / dims.height);
            scale = Math.Min(scale, 1.0);

            var width = Math.Max(16, (int)(dims.width * scale));
            var height = Math.Max(16, (int)(dims.height * scale));
            var imageBytes = _textureGenerator.ResizeImageFromFile(elementPath, width, height);
            overlays.Add((imageBytes, width, height));
        }

        if (overlays.Count == 0)
        {
            return baseTexture;
        }

        var totalWidth = overlays.Sum(o => o.width) + (overlays.Count - 1) * 16;
        var currentX = Math.Max(0, (targetWidth - totalWidth) / 2);
        var targetY = Math.Max(0, (targetHeight - areaHeight) / 2);

        foreach (var overlay in overlays)
        {
            var y = targetY + Math.Max(0, (areaHeight - overlay.height) / 2);
            baseTexture = _textureGenerator.CompositeTextures(baseTexture, overlay.bytes, currentX, y);
            currentX += overlay.width + 16;
        }

        return baseTexture;
    }

    private sealed record ParsedPlateText(string DisplayText, List<string> ElementTokens);
}
