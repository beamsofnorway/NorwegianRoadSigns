namespace NorwegianRoadSigns.Models;

public enum SignCategory
{
    Fareskilt,           // Warning signs (100-156)
    VikepliktOgForkjør, // Right of way signs (200s)
    Forbudsskilt,        // Prohibition signs (300s)
    Påbudsskilt,         // Mandatory signs (400s)
    Opplysningsskilt,    // Information signs (500s)
    Serviceskilt,        // Service signs (600s)
    Vegvisningsskilt,    // Direction signs (700-800s)
    Underskilt,          // Supplementary signs
    Markeringsskilt,     // Marking signs
    Trafikklys,          // Traffic lights
    Other
}
