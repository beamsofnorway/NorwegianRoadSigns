namespace NorwegianRoadSigns.Models;

public class SignPlate
{
    public int Id { get; set; }
    public int Version { get; set; }
    public string? SignNumber { get; set; }
    public string? SignDescription { get; set; }
    public SignColor Color { get; set; }
    public double Width { get; set; }      // in mm
    public double Height { get; set; }     // in mm
    public double TextHeight { get; set; } // in mm
    public string? Text { get; set; }
    public int PlacementCode { get; set; } // 1=top, 2=middle, 3=bottom
    public string? FaceDirection { get; set; }
    public string? FoilClass { get; set; }
    public DateTime? InstallationDate { get; set; }
    public bool IsElectronic { get; set; }
    public bool IsLit { get; set; }
    public double? EmissiveIntensity { get; set; }
}

public class SignPoint
{
    public int Id { get; set; }
    public int Version { get; set; }
    public int? ParentPortalId { get; set; }
    public int PostCount { get; set; }
    public string? PostType { get; set; }
    public string? FoundationType { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public int Srid { get; set; }
    public string? RoadName { get; set; }
    public int? Municipality { get; set; }
    public int? County { get; set; }
    public List<SignPlate> Plates { get; set; } = new();
}

public class Portal
{
    public int Id { get; set; }
    public int Version { get; set; }
    public string? Name { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public int Srid { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public List<SignPoint> SignPoints { get; set; } = new();
}
