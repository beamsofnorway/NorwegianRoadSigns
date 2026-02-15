namespace NorwegianRoadSigns.Models;

public class Geometry
{
    public string Wkt { get; set; } = string.Empty;
    public int Srid { get; set; }
    public bool Egengeometri { get; set; }
}

public class GeometryWithQuality : Geometry
{
    public GeometryQuality? Kvalitet { get; set; }
    public string? Datafangstdato { get; set; }
    public int? Høydereferanse { get; set; }
}

public class GeometryQuality
{
    public string? Datafangstmetode { get; set; }
    public int? Nøyaktighet { get; set; }
    public int? Synbarhet { get; set; }
    public string? DatafangstmetodeHøyde { get; set; }
    public int? NøyaktighetHøyde { get; set; }
    public double? MaksimaltAvvik { get; set; }
}
