using NorwegianRoadSigns.Data;
using NorwegianRoadSigns.Generators;

Console.WriteLine("Norwegian Road Signs - 3D Model Generator");
Console.WriteLine("===========================================");

var command = args.Length > 0 ? args[0].ToLower() : "nvdb";

switch (command)
{
    case "scrape":
        await RunVegvesenScraper();
        break;
    case "nvdb":
    default:
        await RunNvdbGenerator();
        break;
}

async Task RunVegvesenScraper()
{
    Console.WriteLine("\n--- Downloading signs from Vegvesen.no ---");
    
    var scraper = new VegvesenScraper();
    var outputDir = "signs";
    
    var progress = new Progress<string>(msg => Console.WriteLine($"  {msg}"));
    
    try
    {
        await scraper.DownloadAndExtractSignsAsync(outputDir, progress: progress);
        
        Console.WriteLine("\n--- Parsing downloaded signs ---");
        var signPaths = await scraper.GetExtractedSignPathsAsync(outputDir);
        
        foreach (var (category, files) in signPaths)
        {
            Console.WriteLine($"{category}: {files.Count} files");
            foreach (var file in files.Take(5))
            {
                Console.WriteLine($"  - {Path.GetFileName(file)}");
            }
            if (files.Count > 5)
            {
                Console.WriteLine($"  ... and {files.Count - 5} more");
            }
        }
        
        Console.WriteLine("\nDone!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}

async Task RunNvdbGenerator()
{
    var generator = new SignGenerator();

    Console.WriteLine("\nFetching sign from NVDB...");

    try
    {
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
        
        Console.WriteLine("\nGenerating DAE file...");
        var outputPath = "output/sign_86278833.dae";
        await generator.ToDaeAsync(signMesh, outputPath);
        
        Console.WriteLine($"DAE saved to: {outputPath}");
        
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
}
