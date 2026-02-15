using System.Text.Json;
using System.Text.Json.Serialization;

namespace NorwegianRoadSigns.Models;

public class NvdbSignPoint
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public NvdbMetadata? Metadata { get; set; }

    [JsonPropertyName("egenskaper")]
    public List<NvdbProperty>? Properties { get; set; }

    [JsonPropertyName("geometri")]
    public GeometryWithQuality? Geometry { get; set; }

    [JsonPropertyName("lokasjon")]
    public NvdbLocation? Location { get; set; }

    [JsonPropertyName("relasjoner")]
    public NvdbRelations? Relations { get; set; }
}

public class NvdbMetadata
{
    [JsonPropertyName("type")]
    public NvdbTypeInfo? Type { get; set; }

    [JsonPropertyName("versjon")]
    public int Version { get; set; }

    [JsonPropertyName("startdato")]
    public string? StartDate { get; set; }

    [JsonPropertyName("sist_modifisert")]
    public string? LastModified { get; set; }
}

public class NvdbTypeInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("navn")]
    public string Name { get; set; } = string.Empty;
}

public class NvdbProperty
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("navn")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("verdi")]
    public object? Value { get; set; }

    [JsonPropertyName("enhet")]
    public NvdbUnit? Unit { get; set; }

    [JsonPropertyName("egenskapstype")]
    public string? PropertyType { get; set; }

    [JsonPropertyName("enum_id")]
    public int? EnumId { get; set; }
}

public class NvdbUnit
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("navn")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("kortnavn")]
    public string ShortName { get; set; } = string.Empty;
}

public class NvdbLocation
{
    [JsonPropertyName("kommuner")]
    public List<int>? Kommuner { get; set; }

    [JsonPropertyName("fylker")]
    public List<int>? Fylker { get; set; }

    [JsonPropertyName("kontraktsområder")]
    public List<NvdbContractArea>? ContractAreas { get; set; }

    [JsonPropertyName("vegforvaltere")]
    public List<NvdbRoadMaintainer>? RoadMaintainers { get; set; }

    [JsonPropertyName("adresser")]
    public List<NvdbAddress>? Addresses { get; set; }

    [JsonPropertyName("vegsystemreferanser")]
    public List<NvdbRoadSystemReference>? RoadSystemReferences { get; set; }

    [JsonPropertyName("stedfestinger")]
    public List<NvdbPosition>? Positions { get; set; }

    [JsonPropertyName("geometri")]
    public GeometryWithQuality? Geometry { get; set; }
}

public class NvdbContractArea
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("nummer")]
    public int Number { get; set; }

    [JsonPropertyName("navn")]
    public string Name { get; set; } = string.Empty;
}

public class NvdbRoadMaintainer
{
    [JsonPropertyName("enumid")]
    public int EnumId { get; set; }

    [JsonPropertyName("vegforvalter")]
    public string Name { get; set; } = string.Empty;
}

public class NvdbAddress
{
    [JsonPropertyName("navn")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("adressekode")]
    public int AddressCode { get; set; }
}

public class NvdbRoadSystemReference
{
    [JsonPropertyName("vegsystem")]
    public NvdbRoadSystem? RoadSystem { get; set; }

    [JsonPropertyName("strekning")]
    public NvdbStretch? Stretch { get; set; }

    [JsonPropertyName("metrertLokasjon")]
    public NvdbMeteredLocation? MeteredLocation { get; set; }

    [JsonPropertyName("kortform")]
    public string ShortForm { get; set; } = string.Empty;
}

public class NvdbRoadSystem
{
    [JsonPropertyName("vegkategori")]
    public string RoadCategory { get; set; } = string.Empty;

    [JsonPropertyName("fase")]
    public string Phase { get; set; } = string.Empty;

    [JsonPropertyName("nummer")]
    public int Number { get; set; }
}

public class NvdbStretch
{
    [JsonPropertyName("strekning")]
    public int StretchNumber { get; set; }

    [JsonPropertyName("delstrekning")]
    public int SubStretch { get; set; }

    [JsonPropertyName("arm")]
    public bool Arm { get; set; }

    [JsonPropertyName("adskilte_løp")]
    public string? SeparateLanes { get; set; }

    [JsonPropertyName("trafikantgruppe")]
    public string TrafficGroup { get; set; } = string.Empty;

    [JsonPropertyName("retning")]
    public string Direction { get; set; } = string.Empty;

    [JsonPropertyName("meter")]
    public double Meter { get; set; }
}

public class NvdbMeteredLocation
{
    [JsonPropertyName("retning")]
    public string Direction { get; set; } = string.Empty;

    [JsonPropertyName("sideposisjon")]
    public string SidePosition { get; set; } = string.Empty;
}

public class NvdbPosition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("veglenkesekvensid")]
    public int RoadLinkSequenceId { get; set; }

    [JsonPropertyName("relativPosisjon")]
    public double RelativePosition { get; set; }

    [JsonPropertyName("retning")]
    public string Direction { get; set; } = string.Empty;

    [JsonPropertyName("kjørefelt")]
    public List<object>? Lanes { get; set; }

    [JsonPropertyName("sideposisjon")]
    public string SidePosition { get; set; } = string.Empty;

    [JsonPropertyName("kortform")]
    public string ShortForm { get; set; } = string.Empty;
}

public class NvdbRelations
{
    [JsonPropertyName("barn")]
    public List<NvdbChildRelation>? Children { get; set; }

    [JsonPropertyName("foreldre")]
    public List<NvdbParentRelation>? Parents { get; set; }
}

public class NvdbChildRelation
{
    [JsonPropertyName("listeid")]
    public long ListId { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public NvdbTypeInfo? Type { get; set; }

    [JsonPropertyName("vegobjekter")]
    public List<JsonElement>? Vegobjekter { get; set; }
}

public class NvdbParentRelation
{
    [JsonPropertyName("listeid")]
    public long ListId { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public NvdbTypeInfo? Type { get; set; }

    [JsonPropertyName("vegobjekter")]
    public List<int>? ObjectIds { get; set; }

    [JsonPropertyName("relasjonstype")]
    public string? RelationType { get; set; }
}

public class NvdbSignPlate
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public NvdbMetadata? Metadata { get; set; }

    [JsonPropertyName("egenskaper")]
    public List<NvdbProperty>? Properties { get; set; }

    [JsonPropertyName("geometri")]
    public GeometryWithQuality? Geometry { get; set; }

    [JsonPropertyName("lokasjon")]
    public NvdbLocation? Location { get; set; }

    [JsonPropertyName("relasjoner")]
    public NvdbRelations? Relations { get; set; }
}
