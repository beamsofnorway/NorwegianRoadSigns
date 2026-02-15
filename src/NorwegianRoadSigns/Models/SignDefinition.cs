namespace NorwegianRoadSigns.Models;

public class SignDefinition
{
    public string SignNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SignCategory Category { get; set; }
    public GeometryType GeometryType { get; set; }
    public double DefaultWidth { get; set; }   // in mm
    public double DefaultHeight { get; set; }  // in mm
    public SignColor PrimaryColor { get; set; }
    public SignColor BorderColor { get; set; }
    public string? ImageUrl { get; set; }
    public string? SymbolDescription { get; set; }
    public bool HasText { get; set; }
    public bool IsVariable { get; set; }
    public bool IsElectronic { get; set; }
    public List<SignSize> Sizes { get; set; } = new();
}

public class SignSize
{
    public string Name { get; set; } = string.Empty;
    public double Width { get; set; }
    public double Height { get; set; }
}
