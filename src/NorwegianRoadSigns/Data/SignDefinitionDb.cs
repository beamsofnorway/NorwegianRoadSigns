using System.Text.Json;
using System.Text.Json.Serialization;
using NorwegianRoadSigns.Models;

namespace NorwegianRoadSigns.Data;

public class SignDefinitionDb
{
    private readonly Dictionary<string, SignDefinition> _definitions = new();
    private readonly string _cachePath;
    private bool _isLoaded;

    public SignDefinitionDb(string? cachePath = null)
    {
        _cachePath = cachePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "sign_definitions.json");
    }

    public void Load(IEnumerable<SignDefinition> definitions)
    {
        foreach (var def in definitions)
        {
            _definitions[def.SignNumber] = def;
        }
        _isLoaded = true;
    }

    public void LoadFromCache()
    {
        if (!File.Exists(_cachePath))
        {
            // Try embedded resource path
            var assemblyLocation = AppDomain.CurrentDomain.BaseDirectory;
            var altPath = Path.Combine(assemblyLocation, "..", "..", "..", "..", "src", "NorwegianRoadSigns", "Data", "sign_definitions.json");
            if (File.Exists(altPath))
            {
                LoadFromFile(altPath);
                return;
            }
            
            // Try project relative path
            altPath = Path.Combine(assemblyLocation, "sign_definitions.json");
            if (File.Exists(altPath))
            {
                LoadFromFile(altPath);
                return;
            }
            
            return;
        }

        LoadFromFile(_cachePath);
    }

    private void LoadFromFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };
            var definitions = JsonSerializer.Deserialize<List<SignDefinition>>(json, options);
            if (definitions != null)
            {
                Load(definitions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading sign definitions: {ex.Message}");
        }
    }

    public void SaveToCache()
    {
        var directory = Path.GetDirectoryName(_cachePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(_definitions.Values.ToList(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        });
        File.WriteAllText(_cachePath, json);
    }

    public SignDefinition? Get(string signNumber)
    {
        // Try exact match first
        if (_definitions.TryGetValue(signNumber, out var def))
        {
            return def;
        }

        // Try without variant suffix (e.g., "713" instead of "713.V")
        var baseNumber = signNumber.Split('.')[0].Split(' ')[0];
        if (_definitions.TryGetValue(baseNumber, out def))
        {
            return def;
        }

        // Try prefix match
        var prefix = baseNumber.Split('-')[0];
        foreach (var kvp in _definitions)
        {
            if (kvp.Key.StartsWith(prefix))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    public IEnumerable<SignDefinition> GetAll() => _definitions.Values;

    public bool IsLoaded => _isLoaded;

    public int Count => _definitions.Count;
}
