using HtmlAgilityPack;
using NorwegianRoadSigns.Models;

namespace NorwegianRoadSigns.Data;

public class LovdataScraper
{
    private const string BaseUrl = "https://lovdata.no/dokument/SF/forskrift/2005-10-07-1219";
    private static readonly Dictionary<string, SignCategory> ChapterToCategory = new()
    {
        { "KAPITTEL_2", SignCategory.Fareskilt },
        { "KAPITTEL_3", SignCategory.VikepliktOgForkjør },
        { "KAPITTEL_4", SignCategory.Forbudsskilt },
        { "KAPITTEL_5", SignCategory.Påbudsskilt },
        { "KAPITTEL_6", SignCategory.Opplysningsskilt },
        { "KAPITTEL_7", SignCategory.Opplysningsskilt },
        { "KAPITTEL_8", SignCategory.Serviceskilt },
        { "KAPITTEL_9", SignCategory.Vegvisningsskilt },
        { "KAPITTEL_10", SignCategory.Underskilt },
        { "KAPITTEL_11", SignCategory.Markeringsskilt }
    };

    private static readonly Dictionary<string, GeometryType> SignNumberToGeometry = new()
    {
        { "1", GeometryType.Triangle },   // Warning signs start with 1
        { "2", GeometryType.Diamond },    // Right of way
        { "3", GeometryType.Circle },     // Prohibition
        { "4", GeometryType.Circle },    // Mandatory
        { "5", GeometryType.Rectangle }, // Information
        { "6", GeometryType.Rectangle }, // Service
        { "7", GeometryType.Rectangle }, // Route/direction
        { "8", GeometryType.Rectangle }, // More route signs
        { "9", GeometryType.Rectangle }  // Supplementary
    };

    public async Task<List<SignDefinition>> ScrapeAllSignDefinitionsAsync(HttpClient? httpClient = null)
    {
        var definitions = new List<SignDefinition>();
        httpClient ??= new HttpClient();

        var chapters = new[]
        {
            ("KAPITTEL_2", "Fareskilt"),      // §§ 3-4
            ("KAPITTEL_3", "Vikeplikt"),       // §§ 5-6
            ("KAPITTEL_4", "Forbudsskilt"),    // §§ 7-8
            ("KAPITTEL_5", "Påbudsskilt"),     // §§ 9-10
            ("KAPITTEL_6", "Opplysningsskilt"),// §§ 11-12
            ("KAPITTEL_8", "Serviceskilt"),    // §§ 13-14
            ("KAPITTEL_9", "Vegvisningsskilt"),// §§ 15-16
            ("KAPITTEL_10", "Underskilt"),     // §§ 17-18
            ("KAPITTEL_11", "Markeringsskilt") // §§ 19-20
        };

        foreach (var (chapter, name) in chapters)
        {
            try
            {
                var chapterDefs = await ScrapeChapterAsync(httpClient, chapter, name);
                definitions.AddRange(chapterDefs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scraping {chapter}: {ex.Message}");
            }
        }

        return definitions;
    }

    public async Task<List<SignDefinition>> ScrapeChapterAsync(HttpClient httpClient, string chapter, string categoryName)
    {
        var definitions = new List<SignDefinition>();
        var url = $"{BaseUrl}/{chapter}#{chapter}";
        
        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return definitions;
        }

        var html = await response.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var category = ChapterToCategory.TryGetValue(chapter, out var cat) ? cat : SignCategory.Other;

        // Find all sign images and their numbers
        var signNodes = doc.DocumentNode.SelectNodes("//img[contains(@src, '/static/SF/sf-')]");
        if (signNodes == null)
        {
            return definitions;
        }

        foreach (var imgNode in signNodes)
        {
            var src = imgNode.GetAttributeValue("src", "");
            var signNumber = ExtractSignNumberFromUrl(src);
            if (string.IsNullOrEmpty(signNumber))
            {
                continue;
            }

            var signNumberPrefix = signNumber.Split('.')[0][0].ToString();
            var geometryType = SignNumberToGeometry.TryGetValue(signNumberPrefix, out var geo) 
                ? geo 
                : GeometryType.Rectangle;

            var def = new SignDefinition
            {
                SignNumber = signNumber,
                Description = GetDescriptionFromImageNode(imgNode),
                Category = category,
                GeometryType = geometryType,
                DefaultWidth = GetDefaultWidth(signNumber, geometryType),
                DefaultHeight = GetDefaultHeight(signNumber, geometryType),
                PrimaryColor = GetDefaultColor(category),
                BorderColor = SignColor.Svart,
                ImageUrl = $"https://lovdata.no{src}",
                HasText = signNumber.StartsWith("7") || signNumber.StartsWith("8") || signNumber.StartsWith("5")
            };

            definitions.Add(def);
        }

        return definitions;
    }

    private string? ExtractSignNumberFromUrl(string url)
    {
        // URL pattern: /static/SF/sf-20051007-1219-100-1-01.gif
        var match = System.Text.RegularExpressions.Regex.Match(url, @"sf-[\d]+-(\d+(?:\.\d+)?)-\d+\.\w+$");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return null;
    }

    private string GetDescriptionFromImageNode(HtmlNode imgNode)
    {
        // Try to get description from following sibling nodes
        var next = imgNode.NextSibling;
        while (next != null)
        {
            if (next.NodeType == HtmlNodeType.Text)
            {
                var text = next.InnerText.Trim();
                if (!string.IsNullOrEmpty(text) && char.IsDigit(text[0]))
                {
                    return text;
                }
            }
            next = next.NextSibling;
        }
        return string.Empty;
    }

    private double GetDefaultWidth(string signNumber, GeometryType geometryType)
    {
        var prefix = signNumber.Split('.')[0];
        
        // Standard sizes based on sign type
        return prefix switch
        {
            "100" or "102" or "104" => 600,  // Small triangle
            "106" or "108" or "110" => 700,  // Medium triangle
            "112" or "114" or "116" => 800,  // Large triangle
            "118" or "120" or "122" => 800,  // Warning signs
            "124" or "126" or "132" => 700,
            "134" or "135" or "136" => 600,
            "138" or "139" => 600,
            "140" or "142" or "144" => 700,
            "146" or "148" or "149" => 700,
            "150" or "151" or "152" => 700,
            "154" or "155" or "156" => 600,
            
            "202" or "204" => 750,  // Stop sign (octagon)
            "206" or "208" => 600,
            "210" or "212" or "214" => 600,
            
            "302" => 600,  // Circle prohibition
            "306" => 600,
            "308" or "310" or "312" or "314" or "316" => 600,
            "318" or "320" or "322" => 600,
            "324" or "326" => 600,
            "330" or "332" => 600,
            "334" or "335" => 600,
            "336" or "337" => 600,
            "362" or "364" => 600,
            "366" or "368" => 600,
            "370" or "372" => 600,
            "376" or "378" => 600,
            
            "402" or "404" => 600,  // Circle mandatory
            "406" or "408" => 600,
            
            "502" or "504" => 400,  // Rectangle info
            "506" or "508" => 400,
            "510" or "512" => 600,
            
            _ when geometryType == GeometryType.Rectangle => 600,
            _ when geometryType == GeometryType.Circle => 600,
            _ when geometryType == GeometryType.Triangle => 700,
            _ => 600
        };
    }

    private double GetDefaultHeight(string signNumber, GeometryType geometryType)
    {
        var prefix = signNumber.Split('.')[0];
        
        // Some signs have different aspect ratios
        return prefix switch
        {
            "100" or "102" or "104" => 700,  // Triangle
            "106" or "108" or "110" => 800,
            "112" or "114" or "116" => 900,
            
            "202" => 750,  // Stop sign (octagon)
            "204" => 750,
            
            "362" or "364" or "366" or "368" => 800,  // Speed limit signs
            "370" or "372" => 600,
            "376" or "378" => 600,
            
            "502" or "504" => 600,  // Rectangle info
            "506" or "508" => 600,
            
            _ when geometryType == GeometryType.Triangle => 800,
            _ => GetDefaultWidth(signNumber, geometryType) // Square-ish
        };
    }

    private SignColor GetDefaultColor(SignCategory category)
    {
        return category switch
        {
            SignCategory.Fareskilt => SignColor.Hvit,  // White with red border
            SignCategory.VikepliktOgForkjør => SignColor.Hvit,  // White
            SignCategory.Forbudsskilt => SignColor.Hvit,  // White with red border
            SignCategory.Påbudsskilt => SignColor.Blå,  // Blue
            SignCategory.Opplysningsskilt => SignColor.Hvit,  // White
            SignCategory.Serviceskilt => SignColor.Blå,  // Blue
            SignCategory.Vegvisningsskilt => SignColor.Hvit,  // White or yellow
            SignCategory.Underskilt => SignColor.Hvit,
            SignCategory.Markeringsskilt => SignColor.Hvit,
            _ => SignColor.Hvit
        };
    }
}
