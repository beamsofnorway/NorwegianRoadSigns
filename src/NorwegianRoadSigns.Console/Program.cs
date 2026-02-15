using NorwegianRoadSigns.Generators;

Console.WriteLine("Norwegian Road Signs - 3D Model Generator");
Console.WriteLine("===========================================");

var generator = new SignGenerator();

// Try to fetch a sign from NVDB
Console.WriteLine("\nFetching sign from NVDB...");

try
{
    // Test with route sign (713 series)
    var signPointId = 86278833;
    var signMesh = await generator.CreateFromNvdbAsync(signPointId);
    
    Console.WriteLine($"Successfully fetched sign point {signPointId}");
    Console.WriteLine($"  - Number of plates: {signMesh.Plates.Count}");
    
    foreach (var plate in signMesh.Plates)
    {
        Console.WriteLine($"    - Sign: {plate.Plate.SignNumber}");
        Console.WriteLine($"      Text: {plate.Plate.Text}");
        Console.WriteLine($"      Color: {plate.Plate.Color}");
        Console.WriteLine($"      Size: {plate.Plate.Width}x{plate.Plate.Height}mm");
    }
    
    // Generate DAE
    Console.WriteLine("\nGenerating DAE file...");
    var outputPath = "output/sign_86278833.dae";
    await generator.ToDaeAsync(signMesh, outputPath);
    
    Console.WriteLine($"DAE saved to: {outputPath}");
    
    // Show texture files created
    var outputDir = Path.GetDirectoryName(outputPath) ?? "output";
    var textureFiles = Directory.GetFiles(outputDir, "*.png");
    Console.WriteLine($"Texture files created: {textureFiles.Length}");
    foreach (var tex in textureFiles)
    {
        Console.WriteLine($"  - {Path.GetFileName(tex)}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

// Test with another sign point to verify different sign types
Console.WriteLine("\n--- Testing different sign types ---");
var testIds = new[] { 86278833 };
foreach (var id in testIds)
{
    try
    {
        var signMesh = await generator.CreateFromNvdbAsync(id);
        Console.WriteLine($"Sign {id}: {signMesh.Plates.Count} plates");
        foreach (var p in signMesh.Plates.Take(3))
        {
            Console.WriteLine($"  {p.Plate.SignNumber}: {p.Mesh.Name}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching {id}: {ex.Message}");
    }
}

Console.WriteLine("\nDone!");
