using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NorwegianRoadSigns.Models;

namespace NorwegianRoadSigns.Clients;

public class NvdbClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://nvdbapiles.atlas.vegvesen.no/vegobjekter/api/v4/vegobjekter";
    private bool _disposed;

    public NvdbClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "NorwegianRoadSigns/1.0");
        _httpClient.DefaultRequestHeaders.Add("X-Client", "NorwegianRoadSigns");
    }

    public async Task<SignPoint?> GetSignPointAsync(int id, int depth = 10)
    {
        var url = $"{BaseUrl}/95/{id}?dybde={depth}&inkluder=lokasjon,metadata,egenskaper,relasjoner,geometri";
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"NVDB API Error ({response.StatusCode}): {errorContent}");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        var nvdbSignPoint = JsonSerializer.Deserialize<NvdbSignPoint>(json, GetJsonOptions());
        
        if (nvdbSignPoint == null)
        {
            return null;
        }

        return MapToSignPoint(nvdbSignPoint);
    }

    public async Task<Portal?> GetPortalAsync(int id, int depth = 5)
    {
        var url = $"{BaseUrl}/24/{id}?dybde={depth}&inkluder=lokasjon,metadata,egenskaper,relasjoner,geometri";
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        var nvdbPortal = JsonSerializer.Deserialize<NvdbSignPoint>(json, GetJsonOptions());
        
        if (nvdbPortal == null)
        {
            return null;
        }

        return MapToPortal(nvdbPortal);
    }

    public async Task<List<SignPoint>> QuerySignPointsInAreaAsync(
        double minX, double minY, 
        double maxX, double maxY, 
        int limit = 1000)
    {
        var bbox = $"{minX},{minY},{maxX},{maxY}";
        var url = $"{BaseUrl}/95?bbox={bbox}&dybde=2&inkluder=egenskaper,geometri&antall={limit}";
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            return new List<SignPoint>();
        }

        var json = await response.Content.ReadAsStringAsync();
        var options = GetJsonOptions();
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        if (!root.TryGetProperty("objekter", out var objects))
        {
            return new List<SignPoint>();
        }

        var signPoints = new List<SignPoint>();
        foreach (var obj in objects.EnumerateArray())
        {
            var nvdbSignPoint = JsonSerializer.Deserialize<NvdbSignPoint>(obj.GetRawText(), options);
            if (nvdbSignPoint != null)
            {
                signPoints.Add(MapToSignPoint(nvdbSignPoint));
            }
        }

        return signPoints;
    }

    private SignPoint MapToSignPoint(NvdbSignPoint nvdb)
    {
        var signPoint = new SignPoint
        {
            Id = nvdb.Id,
            Version = nvdb.Metadata?.Version ?? 1,
            Plates = new List<SignPlate>()
        };

        // Parse geometry
        if (nvdb.Geometry?.Wkt != null)
        {
            var coords = ParseWktPoint(nvdb.Geometry.Wkt);
            signPoint.X = coords.x;
            signPoint.Y = coords.y;
            signPoint.Z = coords.z;
            signPoint.Srid = nvdb.Geometry.Srid;
        }

        // Parse properties
        if (nvdb.Properties != null)
        {
            foreach (var prop in nvdb.Properties)
            {
                switch (prop.Id)
                {
                    case 1877: // Antall oppsettingsutstyr
                        if (prop.Value is JsonElement je && je.ValueKind == JsonValueKind.Number)
                            signPoint.PostCount = je.GetInt32();
                        break;
                    case 1887: // Antall fundament
                        break;
                    case 1671: // Fundamentering
                        if (prop.Value is JsonElement je2 && je2.ValueKind == JsonValueKind.String)
                            signPoint.FoundationType = je2.GetString();
                        else
                            signPoint.FoundationType = prop.Value?.ToString();
                        break;
                    case 1876: // Oppsettingsutstyr
                        if (prop.Value is JsonElement je3 && je3.ValueKind == JsonValueKind.String)
                            signPoint.PostType = je3.GetString();
                        else
                            signPoint.PostType = prop.Value?.ToString();
                        break;
                }
            }
        }

        // Parse location
        if (nvdb.Location != null)
        {
            signPoint.Municipality = nvdb.Location.Kommuner?.FirstOrDefault();
            signPoint.County = nvdb.Location.Fylker?.FirstOrDefault();
            
            if (nvdb.Location.Addresses?.Any() == true)
            {
                signPoint.RoadName = nvdb.Location.Addresses.First().Name;
            }
            
            if (nvdb.Location.Geometry?.Wkt != null)
            {
                var coords = ParseWktPoint(nvdb.Location.Geometry.Wkt);
                signPoint.X = coords.x;
                signPoint.Y = coords.y;
                signPoint.Z = coords.z;
            }
        }

        // Parse children (sign plates)
        if (nvdb.Relations?.Children != null)
        {
            foreach (var child in nvdb.Relations.Children)
            {
                if (child.Vegobjekter != null)
                {
                    foreach (var vegobjekt in child.Vegobjekter)
                    {
                        var plate = MapToSignPlateFromJson(vegobjekt);
                        if (plate != null)
                        {
                            signPoint.Plates.Add(plate);
                        }
                    }
                }
            }
        }

        return signPoint;
    }

    private SignPlate? MapToSignPlateFromJson(JsonElement json)
    {
        var plate = new SignPlate
        {
            IsElectronic = false
        };

        if (json.TryGetProperty("id", out var idElement))
        {
            plate.Id = idElement.GetInt32();
        }

        if (json.TryGetProperty("metadata", out var metadataElement))
        {
            if (metadataElement.TryGetProperty("versjon", out var versionElement))
            {
                plate.Version = versionElement.GetInt32();
            }
        }

        if (json.TryGetProperty("egenskaper", out var egenskaperElement))
        {
            foreach (var prop in egenskaperElement.EnumerateArray())
            {
                var propId = prop.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0;
                var propValue = prop.TryGetProperty("verdi", out var valueProp) ? valueProp : default;
                var propName = prop.TryGetProperty("navn", out var nameProp) ? nameProp.GetString() : "";

                switch (propId)
                {
                    case 5530: // Skiltnummer
                        if (propValue.ValueKind == JsonValueKind.String)
                            plate.SignNumber = propValue.GetString();
                        break;
                    case 1294: // Farge
                        if (propValue.ValueKind == JsonValueKind.String)
                            plate.Color = ParseColor(propValue.GetString());
                        break;
                    case 1328: // Bredde, målt
                        if (propValue.ValueKind == JsonValueKind.Number)
                            plate.Width = propValue.GetDouble();
                        break;
                    case 1588: // Høyde, egen
                        if (propValue.ValueKind == JsonValueKind.Number)
                            plate.Height = propValue.GetDouble();
                        break;
                    case 1912: // Tekst-/Symbolhøyde
                        if (propValue.ValueKind == JsonValueKind.Number)
                            plate.TextHeight = propValue.GetDouble();
                        break;
                    case 1910: // Tekst
                        if (propValue.ValueKind == JsonValueKind.String)
                            plate.Text = propValue.GetString();
                        break;
                    case 5128: // Plasseringskode
                        if (propValue.ValueKind == JsonValueKind.Number)
                            plate.PlacementCode = propValue.GetInt32();
                        break;
                    case 1894: // Ansiktsside, rettet mot
                        if (propValue.ValueKind == JsonValueKind.String)
                            plate.FaceDirection = propValue.GetString();
                        break;
                    case 1921: // Folieklasse
                        if (propValue.ValueKind == JsonValueKind.String)
                            plate.FoilClass = propValue.GetString();
                        break;
                    case 2108: // Oppsettingsdato
                        if (propValue.ValueKind == JsonValueKind.String && DateTime.TryParse(propValue.GetString(), out var date))
                        {
                            plate.InstallationDate = date;
                        }
                        break;
                }
            }
        }

        return plate;
    }

    private SignPlate? MapToSignPlate(NvdbSignPlate nvdb)
    {
        var plate = new SignPlate
        {
            Id = nvdb.Id,
            Version = nvdb.Metadata?.Version ?? 1,
            IsElectronic = false
        };

        if (nvdb.Properties == null)
        {
            return plate;
        }

        foreach (var prop in nvdb.Properties)
        {
            switch (prop.Id)
            {
                case 5530: // Skiltnummer
                    plate.SignNumber = prop.Value?.ToString();
                    break;
                case 1294: // Farge
                    plate.Color = ParseColor(prop.Value?.ToString());
                    break;
                case 1328: // Bredde, målt
                    plate.Width = Convert.ToDouble(prop.Value);
                    break;
                case 1588: // Høyde, egen
                    plate.Height = Convert.ToDouble(prop.Value);
                    break;
                case 1912: // Tekst-/Symbolhøyde
                    plate.TextHeight = Convert.ToDouble(prop.Value);
                    break;
                case 1910: // Tekst
                    plate.Text = prop.Value?.ToString();
                    break;
                case 5128: // Plasseringskode
                    plate.PlacementCode = Convert.ToInt32(prop.Value);
                    break;
                case 1894: // Ansiktsside, rettet mot
                    plate.FaceDirection = prop.Value?.ToString();
                    break;
                case 1921: // Folieklasse
                    plate.FoilClass = prop.Value?.ToString();
                    break;
                case 2108: // Oppsettingsdato
                    if (DateTime.TryParse(prop.Value?.ToString(), out var date))
                    {
                        plate.InstallationDate = date;
                    }
                    break;
            }
        }

        return plate;
    }

    private Portal? MapToPortal(NvdbSignPoint nvdb)
    {
        var portal = new Portal
        {
            Id = nvdb.Id,
            Version = nvdb.Metadata?.Version ?? 1,
            Name = nvdb.Metadata?.Type?.Name
        };

        if (nvdb.Geometry?.Wkt != null)
        {
            var coords = ParseWktPoint(nvdb.Geometry.Wkt);
            portal.X = coords.x;
            portal.Y = coords.y;
            portal.Z = coords.z;
            portal.Srid = nvdb.Geometry.Srid;
        }

        // Parse children (sign points)
        if (nvdb.Relations?.Children != null)
        {
            // Note: For portals, children might be different object types
            // This needs adjustment based on actual NVDB API structure
        }

        return portal;
    }

    private SignColor ParseColor(string? colorName)
    {
        return colorName?.ToLower() switch
        {
            "hvit" => SignColor.Hvit,
            "gul" => SignColor.Gul,
            "brun" => SignColor.Brun,
            "blå" => SignColor.Blå,
            "rød" => SignColor.Rød,
            "grønn" => SignColor.Grønn,
            "svart" => SignColor.Svart,
            "oransje" => SignColor.Oransje,
            "grå" => SignColor.Grå,
            "gulgrønn" => SignColor.GulGrønn,
            _ => SignColor.Hvit
        };
    }

    private (double x, double y, double z) ParseWktPoint(string wkt)
    {
        // Parse "POINT Z (x y z)" format
        var start = wkt.IndexOf('(');
        var end = wkt.IndexOf(')');
        if (start < 0 || end < 0)
        {
            return (0, 0, 0);
        }

        var coordsStr = wkt.Substring(start + 1, end - start - 1);
        var parts = coordsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length >= 2)
        {
            var x = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
            var y = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            var z = parts.Length >= 3 
                ? double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture) 
                : 0.0;
            return (x, y, z);
        }
        
        return (0, 0, 0);
    }

    private JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
