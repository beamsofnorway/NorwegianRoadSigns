using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using NorwegianRoadSigns.Models;

namespace NorwegianRoadSigns.Generators;

public class TextureGenerator
{
    private const int DefaultTextureSize = 1024;
    
    private static readonly Dictionary<SignColor, Rgba32> ColorMap = new()
    {
        { SignColor.Hvit, new Rgba32(255, 255, 255, 255) },
        { SignColor.Gul, new Rgba32(255, 206, 0, 255) },
        { SignColor.Brun, new Rgba32(139, 90, 43, 255) },
        { SignColor.Blå, new Rgba32(0, 94, 162, 255) },
        { SignColor.Rød, new Rgba32(213, 0, 50, 255) },
        { SignColor.Grønn, new Rgba32(0, 128, 64, 255) },
        { SignColor.Svart, new Rgba32(0, 0, 0, 255) },
        { SignColor.Oransje, new Rgba32(237, 137, 14, 255) },
        { SignColor.Grå, new Rgba32(128, 128, 128, 255) },
        { SignColor.GulGrønn, new Rgba32(173, 220, 0, 255) }
    };

    public async Task<byte[]> GenerateBaseTextureAsync(
        SignDefinition definition, 
        SignColor color,
        int size = DefaultTextureSize)
    {
        return await Task.Run(() => GenerateBaseTexture(definition, color, size));
    }

    public byte[] GenerateBaseTexture(
        SignDefinition definition, 
        SignColor color,
        int size = DefaultTextureSize)
    {
        using var image = new Image<Rgba32>(size, size);
        
        var backgroundColor = ColorMap.TryGetValue(color, out var bg) ? bg : new Rgba32(255, 255, 255, 255);
        var borderColor = ColorMap.TryGetValue(definition.BorderColor, out var bc) ? bc : new Rgba32(0, 0, 0, 255);
        
        var margin = (int)(size * 0.05f);
        var borderWidth = (int)(size * 0.08f);
        
        // Fill background
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                image[x, y] = backgroundColor;
            }
        }

        // Draw border based on geometry type
        switch (definition.GeometryType)
        {
            case GeometryType.Circle:
                DrawCircleBorder(image, size, borderWidth, borderColor);
                break;
                
            case GeometryType.Triangle:
                DrawTriangleBorder(image, size, borderWidth, borderColor);
                break;
                
            case GeometryType.Octagon:
                DrawOctagonBorder(image, size, borderWidth, borderColor);
                break;
                
            case GeometryType.Diamond:
                DrawDiamondBorder(image, size, borderWidth, borderColor);
                break;
                
            case GeometryType.Rectangle:
            default:
                DrawRectangleBorder(image, size, margin, borderWidth, borderColor);
                break;
        }

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> GenerateTextTextureAsync(
        string text,
        double fontHeight,
        SignColor textColor,
        SignColor backgroundColor,
        int width,
        int height)
    {
        return await Task.Run(() => GenerateTextTexture(text, fontHeight, textColor, backgroundColor, width, height));
    }

    public byte[] GenerateTextTexture(
        string text,
        double fontHeight,
        SignColor textColor,
        SignColor backgroundColor,
        int width,
        int height)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<byte>();
        }

        using var image = new Image<Rgba32>(width, height);
        
        var bgColor = ColorMap.TryGetValue(backgroundColor, out var bg) ? bg : new Rgba32(0, 0, 0, 0);
        var fgColor = ColorMap.TryGetValue(textColor, out var fg) ? fg : new Rgba32(0, 0, 0, 255);
        
        // Fill background
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = bgColor;
            }
        }

        // Draw text (simplified - just pixel-based text representation)
        // In production, use proper font rendering
        var fontSize = (int)(fontHeight * height / 1000);
        if (fontSize < 4) fontSize = 4;
        if (fontSize > height / 2) fontSize = height / 2;

        DrawSimpleText(image, text, width, height, fontSize, fgColor);

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    private void DrawSimpleText(Image<Rgba32> image, string text, int width, int height, int fontSize, Rgba32 color)
    {
        // Very simple text representation - draws rectangles for each "character"
        // This is a placeholder - in production, use proper font rendering
        var charWidth = fontSize * 3 / 4;
        var startX = (width - text.Length * charWidth) / 2;
        var startY = (height - fontSize) / 2;
        
        for (int i = 0; i < text.Length; i++)
        {
            var charX = startX + i * charWidth;
            if (charX < 0 || charX + charWidth > width) continue;
            
            // Simple box for each character as placeholder
            for (int y = startY; y < startY + fontSize && y < height; y++)
            {
                for (int x = charX; x < charX + charWidth && x < width; x++)
                {
                    if (y == startY || y == startY + fontSize - 1 || 
                        x == charX || x == charX + charWidth - 1)
                    {
                        image[x, y] = color;
                    }
                }
            }
        }
    }

    public byte[] CompositeTextures(
        byte[] baseTexture,
        byte[]? overlayTexture,
        int x,
        int y)
    {
        using var baseImage = Image.Load<Rgba32>(baseTexture);
        
        if (overlayTexture != null && overlayTexture.Length > 0)
        {
            using var overlayImage = Image.Load<Rgba32>(overlayTexture);
            
            for (int oy = 0; oy < overlayImage.Height; oy++)
            {
                for (int ox = 0; ox < overlayImage.Width; ox++)
                {
                    var targetX = x + ox;
                    var targetY = y + oy;
                    
                    if (targetX >= 0 && targetX < baseImage.Width && 
                        targetY >= 0 && targetY < baseImage.Height)
                    {
                        var overlayPixel = overlayImage[ox, oy];
                        if (overlayPixel.A > 0)
                        {
                            baseImage[targetX, targetY] = overlayPixel;
                        }
                    }
                }
            }
        }

        using var ms = new MemoryStream();
        baseImage.SaveAsPng(ms);
        return ms.ToArray();
    }

    public byte[] CreateSignTextureWithText(
        SignDefinition definition,
        SignColor signColor,
        string? customText,
        int size = DefaultTextureSize)
    {
        var baseTexture = GenerateBaseTexture(definition, signColor, size);
        
        if (string.IsNullOrEmpty(customText))
        {
            return baseTexture;
        }

        var textTexture = GenerateTextTexture(
            customText,
            definition.DefaultHeight * 0.15,
            SignColor.Svart,
            signColor,
            size,
            size / 4);

        return CompositeTextures(baseTexture, textTexture, 0, size * 3 / 4 - size / 8);
    }

    public async Task<byte[]?> DownloadImageAsync(string url, HttpClient? httpClient = null)
    {
        try
        {
            httpClient ??= new HttpClient();
            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch
        {
            return null;
        }
    }

    private void DrawRectangleBorder(Image<Rgba32> image, int size, int margin, int borderWidth, Rgba32 borderColor)
    {
        // Top border
        for (int y = margin; y < margin + borderWidth && y < size; y++)
        {
            for (int x = margin; x < size - margin; x++)
            {
                image[x, y] = borderColor;
            }
        }
        // Bottom border
        for (int y = size - margin - borderWidth; y < size - margin && y >= 0; y++)
        {
            for (int x = margin; x < size - margin; x++)
            {
                image[x, y] = borderColor;
            }
        }
        // Left border
        for (int x = margin; x < margin + borderWidth && x < size; x++)
        {
            for (int y = margin; y < size - margin; y++)
            {
                image[x, y] = borderColor;
            }
        }
        // Right border
        for (int x = size - margin - borderWidth; x < size - margin && x >= 0; x++)
        {
            for (int y = margin; y < size - margin; y++)
            {
                image[x, y] = borderColor;
            }
        }
    }

    private void DrawCircleBorder(Image<Rgba32> image, int size, int borderWidth, Rgba32 borderColor)
    {
        var center = size / 2f;
        var radius = size / 2f - borderWidth / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var dist = Math.Sqrt(dx * dx + dy * dy);
                
                if (dist >= radius - borderWidth / 2 && dist <= radius + borderWidth / 2)
                {
                    image[x, y] = borderColor;
                }
            }
        }
    }

    private void DrawTriangleBorder(Image<Rgba32> image, int size, int borderWidth, Rgba32 borderColor)
    {
        var points = new (float x, float y)[]
        {
            (size / 2f, borderWidth),
            (borderWidth, size - borderWidth),
            (size - borderWidth, size - borderWidth)
        };
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (IsPointNearTriangleLine(x, y, points, borderWidth))
                {
                    image[x, y] = borderColor;
                }
            }
        }
    }

    private bool IsPointNearTriangleLine(int x, int y, (float x, float y)[] points, int threshold)
    {
        for (int i = 0; i < 3; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % 3];
            if (DistanceToLine(x, y, p1.x, p1.y, p2.x, p2.y) < threshold)
            {
                return true;
            }
        }
        return false;
    }

    private double DistanceToLine(double px, double py, double x1, double y1, double x2, double y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len == 0) return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
        
        var t = Math.Max(0, Math.Min(1, ((px - x1) * dx + (py - y1) * dy) / (len * len)));
        var projX = x1 + t * dx;
        var projY = y1 + t * dy;
        
        return Math.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
    }

    private void DrawOctagonBorder(Image<Rgba32> image, int size, int borderWidth, Rgba32 borderColor)
    {
        var margin = borderWidth;
        var corner = (int)(size * 0.15f);
        
        var points = new (float x, float y)[]
        {
            (size / 2f, margin),
            (size - corner, margin),
            (size - margin, corner),
            (size - margin, size - corner),
            (size - corner, size - margin),
            (corner, size - margin),
            (margin, size - corner),
            (margin, corner)
        };
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (IsPointNearPolygonLine(x, y, points, borderWidth))
                {
                    image[x, y] = borderColor;
                }
            }
        }
    }

    private void DrawDiamondBorder(Image<Rgba32> image, int size, int borderWidth, Rgba32 borderColor)
    {
        var center = size / 2f;
        var inset = borderWidth;
        
        var points = new (float x, float y)[]
        {
            (center, inset),
            (size - inset, center),
            (center, size - inset),
            (inset, center)
        };
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (IsPointNearPolygonLine(x, y, points, borderWidth))
                {
                    image[x, y] = borderColor;
                }
            }
        }
    }

    private bool IsPointNearPolygonLine(int x, int y, (float x, float y)[] points, int threshold)
    {
        for (int i = 0; i < points.Length; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % points.Length];
            if (DistanceToLine(x, y, p1.x, p1.y, p2.x, p2.y) < threshold)
            {
                return true;
            }
        }
        return false;
    }
}
