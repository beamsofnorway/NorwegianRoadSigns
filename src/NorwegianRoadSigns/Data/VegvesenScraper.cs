using System.IO.Compression;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using NorwegianRoadSigns.Models;

namespace NorwegianRoadSigns.Data;

public class VegvesenScraper
{
    private const string BaseUrl = "https://www.vegvesen.no/fag/veg-og-gate/trafikkskilt-og-vegoppmerking/filer-og-fargekoder-for-trafikkskilt/";
    
    private static readonly Dictionary<string, SignCategory> CategoryMap = new()
    {
        { "fareskilt", SignCategory.Fareskilt },
        { "vikeplikt", SignCategory.VikepliktOgForkjør },
        { "forbudsskilt", SignCategory.Forbudsskilt },
        { "pabudsskilt", SignCategory.Påbudsskilt },
        { "opplysningsskilt", SignCategory.Opplysningsskilt },
        { "serviceskilt", SignCategory.Serviceskilt },
        { "vegvisningsskilt", SignCategory.Vegvisningsskilt },
        { "underskilt", SignCategory.Underskilt },
        { "markeringsskilt", SignCategory.Markeringsskilt }
    };

    private static readonly Dictionary<string, GeometryType> SignNumberToGeometry = new()
    {
        { "1", GeometryType.Triangle },
        { "2", GeometryType.Diamond },
        { "3", GeometryType.Circle },
        { "4", GeometryType.Circle },
        { "5", GeometryType.Rectangle },
        { "6", GeometryType.Rectangle },
        { "7", GeometryType.Rectangle },
        { "8", GeometryType.Rectangle },
        { "9", GeometryType.Rectangle }
    };

    public async Task<List<SignDefinition>> ScrapeAllSignDefinitionsAsync(HttpClient? httpClient = null)
    {
        var definitions = new List<SignDefinition>();
        httpClient ??= new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        var response = await httpClient.GetAsync(BaseUrl);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to fetch page: {response.StatusCode}");
        }

        var html = await response.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var downloadLinks = doc.DocumentNode.SelectNodes("//a[contains(@href, '.zip')]");
        if (downloadLinks == null)
        {
            return definitions;
        }

        foreach (var link in downloadLinks)
        {
            var href = link.GetAttributeValue("href", "");
            var title = link.GetAttributeValue("title", "");
            
            if (string.IsNullOrEmpty(href) || !href.Contains("globalassets"))
            {
                continue;
            }

            if (!href.StartsWith("http"))
            {
                href = "https://www.vegvesen.no" + href;
            }
            
            var category = DetermineCategory(href, title);
            var format = href.Contains("eps", StringComparison.OrdinalIgnoreCase) ? "eps" : "jpg";
            
            var def = new SignDefinition
            {
                Description = $"Vegvesen {category} - {format.ToUpper()}",
                Category = category,
                GeometryType = GeometryType.Rectangle,
                DefaultWidth = 600,
                DefaultHeight = 600,
                PrimaryColor = GetDefaultColor(category),
                BorderColor = SignColor.Svart,
                ImageUrl = href,
                DownloadFormat = format
            };

            definitions.Add(def);
        }

        return definitions;
    }

    public async Task<string> DownloadAndExtractSignsAsync(
        string outputDirectory,
        HttpClient? httpClient = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        httpClient ??= new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var definitions = await ScrapeAllSignDefinitionsAsync(httpClient);
        var extractedPaths = new List<string>();

        foreach (var def in definitions)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            progress?.Report($"Downloading {def.Description}...");
            
            try
            {
                var zipUrl = def.ImageUrl;
                if (string.IsNullOrEmpty(zipUrl))
                {
                    continue;
                }

                var categoryFolder = GetCategoryFolder(def.Category);
                var categoryDir = Path.Combine(outputDirectory, categoryFolder);
                if (!Directory.Exists(categoryDir))
                {
                    Directory.CreateDirectory(categoryDir);
                }

                var cleanUrl = zipUrl.Split('?')[0];
                var zipFileName = Path.GetFileName(cleanUrl);
                var zipPath = Path.Combine(outputDirectory, zipFileName);

                if (!File.Exists(zipPath))
                {
                    var zipResponse = await httpClient.GetAsync(zipUrl, cancellationToken);
                    if (zipResponse.IsSuccessStatusCode)
                    {
                        var zipBytes = await zipResponse.Content.ReadAsByteArrayAsync(cancellationToken);
                        await File.WriteAllBytesAsync(zipPath, zipBytes, cancellationToken);
                    }
                }

                if (File.Exists(zipPath))
                {
                    progress?.Report($"Extracting {zipFileName}...");
                    ZipFile.ExtractToDirectory(zipPath, categoryDir, overwriteFiles: true);
                    extractedPaths.Add(categoryDir);
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"Error downloading {def.Description}: {ex.Message}");
            }
        }

        progress?.Report("Download and extraction complete!");
        return outputDirectory;
    }

    public async Task<Dictionary<string, List<string>>> GetExtractedSignPathsAsync(string baseDirectory)
    {
        var result = new Dictionary<string, List<string>>();
        
        if (!Directory.Exists(baseDirectory))
        {
            return result;
        }

        foreach (var category in CategoryMap.Keys)
        {
            var categoryDir = Path.Combine(baseDirectory, category);
            if (Directory.Exists(categoryDir))
            {
                var files = Directory.GetFiles(categoryDir, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".eps", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                if (files.Any())
                {
                    result[category] = files;
                }
            }
        }

        return result;
    }

    public async Task<SignDefinition?> ParseSignFromFilename(string filename)
    {
        var name = Path.GetFileNameWithoutExtension(filename);
        
        var match = Regex.Match(name, @"(\d+)");
        if (!match.Success)
        {
            return null;
        }

        var signNumber = match.Groups[1].Value;
        var prefix = signNumber[0].ToString();
        
        var geometryType = SignNumberToGeometry.TryGetValue(prefix, out var geo) 
            ? geo 
            : GeometryType.Rectangle;

        var category = GetCategoryFromSignNumber(signNumber);

        return new SignDefinition
        {
            SignNumber = signNumber,
            Description = name,
            Category = category,
            GeometryType = geometryType,
            DefaultWidth = GetDefaultWidth(signNumber, geometryType),
            DefaultHeight = GetDefaultHeight(signNumber, geometryType),
            PrimaryColor = GetDefaultColor(category),
            BorderColor = SignColor.Svart
        };
    }

    private SignCategory DetermineCategory(string href, string title)
    {
        var combined = (href + " " + title).ToLower();
        
        foreach (var kvp in CategoryMap)
        {
            if (combined.Contains(kvp.Key))
            {
                return kvp.Value;
            }
        }
        
        return SignCategory.Other;
    }

    private string GetCategoryFolder(SignCategory category)
    {
        return category switch
        {
            SignCategory.Fareskilt => "fareskilt",
            SignCategory.VikepliktOgForkjør => "vikeplikt",
            SignCategory.Forbudsskilt => "forbudsskilt",
            SignCategory.Påbudsskilt => "pabudsskilt",
            SignCategory.Opplysningsskilt => "opplysningsskilt",
            SignCategory.Serviceskilt => "serviceskilt",
            SignCategory.Vegvisningsskilt => "vegvisningsskilt",
            SignCategory.Underskilt => "underskilt",
            SignCategory.Markeringsskilt => "markeringsskilt",
            _ => "other"
        };
    }

    private SignCategory GetCategoryFromSignNumber(string signNumber)
    {
        var prefix = signNumber[0].ToString();
        return SignNumberToGeometry.TryGetValue(prefix, out _) 
            ? CategoryMap.Values.ElementAt(int.Parse(prefix) - 1) 
            : SignCategory.Other;
    }

    private SignColor GetDefaultColor(SignCategory category)
    {
        return category switch
        {
            SignCategory.Fareskilt => SignColor.Hvit,
            SignCategory.VikepliktOgForkjør => SignColor.Hvit,
            SignCategory.Forbudsskilt => SignColor.Hvit,
            SignCategory.Påbudsskilt => SignColor.Blå,
            SignCategory.Opplysningsskilt => SignColor.Hvit,
            SignCategory.Serviceskilt => SignColor.Blå,
            SignCategory.Vegvisningsskilt => SignColor.Hvit,
            SignCategory.Underskilt => SignColor.Hvit,
            SignCategory.Markeringsskilt => SignColor.Hvit,
            _ => SignColor.Hvit
        };
    }

    private double GetDefaultWidth(string signNumber, GeometryType geometryType)
    {
        var prefix = signNumber.Split('.')[0];
        
        return prefix switch
        {
            "100" or "102" or "104" => 600,
            "106" or "108" or "110" => 700,
            "112" or "114" or "116" => 800,
            "202" or "204" => 750,
            "206" or "208" => 600,
            "302" => 600,
            "362" or "364" => 600,
            "402" or "404" => 600,
            "502" or "504" => 400,
            _ when geometryType == GeometryType.Rectangle => 600,
            _ when geometryType == GeometryType.Circle => 600,
            _ when geometryType == GeometryType.Triangle => 700,
            _ => 600
        };
    }

    private double GetDefaultHeight(string signNumber, GeometryType geometryType)
    {
        var prefix = signNumber.Split('.')[0];
        
        return prefix switch
        {
            "100" or "102" or "104" => 700,
            "106" or "108" or "110" => 800,
            "112" or "114" or "116" => 900,
            "202" or "204" => 750,
            "362" or "364" => 800,
            "502" or "504" => 600,
            _ when geometryType == GeometryType.Triangle => 800,
            _ => GetDefaultWidth(signNumber, geometryType)
        };
    }
}
