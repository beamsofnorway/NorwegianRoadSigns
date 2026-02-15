using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using NorwegianRoadSigns.Models;

namespace NorwegianRoadSigns.Generators;

public class TextureGenerator
{
    private const int DefaultTextureSize = 1024;
    private const double FontSizeMultiplier = 7;
    
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

    private static readonly FontCollection FontCollection;
    private static Font? _trafficFont;

    static TextureGenerator()
    {
        FontCollection = new FontCollection();
        
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var fontPaths = new[]
        {
            Path.Combine(baseDir, "fonts", "Trafikkalfabetet.ttf"),
            Path.Combine(baseDir, "fonts", "traficomp.ttf"),
            Path.Combine(baseDir, "..", "fonts", "Trafikkalfabetet.ttf"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "traficomp.ttf"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Trafikkalfabetet.ttf")
        };

        foreach (var fontPath in fontPaths)
        {
            if (File.Exists(fontPath))
            {
                FontCollection.Add(fontPath);
                break;
            }
        }

        if (FontCollection.Families.Any())
        {
            try
            {
                _trafficFont = FontCollection.Families.First().CreateFont(48f, FontStyle.Regular);
            }
            catch
            {
                _trafficFont = null;
            }
        }
    }

    private static Font GetFont(float size)
    {
        if (_trafficFont != null)
        {
            try
            {
                return new Font(_trafficFont, size);
            }
            catch
            {
            }
        }

        var systemFont = SystemFonts.Get("Arial");
        return systemFont.CreateFont(size, FontStyle.Regular);
    }

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
        var aspectRatio = definition.DefaultWidth / definition.DefaultHeight;
        var width = (int)(size * aspectRatio);
        var height = size;
        
        return GenerateBaseTexture(definition, color, width, height);
    }

    public byte[] GenerateBaseTexture(
        SignDefinition definition, 
        SignColor color,
        int width,
        int height)
    {
        using var image = new Image<Rgba32>(width, height);
        
        var backgroundColor = ColorMap.TryGetValue(color, out var bg) ? bg : new Rgba32(255, 255, 255, 255);
        var borderColor = ColorMap.TryGetValue(definition.BorderColor, out var bc) ? bc : new Rgba32(0, 0, 0, 255);
        
        var margin = (int)(width * 0.05f);
        var borderWidth = (int)(Math.Min(width, height) * 0.08f);
        
        // Fill background
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = backgroundColor;
            }
        }

        // Draw border based on geometry type
        switch (definition.GeometryType)
        {
            case GeometryType.Circle:
                DrawCircleBorder(image, width, height, borderWidth, borderColor);
                break;
                
            case GeometryType.Triangle:
                DrawTriangleBorder(image, width, height, borderWidth, borderColor);
                break;
                
            case GeometryType.Octagon:
                DrawOctagonBorder(image, width, height, borderWidth, borderColor);
                break;
                
            case GeometryType.Diamond:
                DrawDiamondBorder(image, width, height, borderWidth, borderColor);
                break;
                
            case GeometryType.Rectangle:
            default:
                DrawRectangleBorder(image, width, height, margin, borderWidth, borderColor);
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
        int height,
        double signHeight)
    {
        return await Task.Run(() => GenerateTextTexture(text, fontHeight, textColor, backgroundColor, width, height, signHeight));
    }

    public byte[] GenerateTextTexture(
        string text,
        double fontHeight,
        SignColor textColor,
        SignColor backgroundColor,
        int width,
        int height,
        double signHeight)
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
        var fontSize = (int)(fontHeight * height / signHeight * FontSizeMultiplier);
        if (fontSize < 4) fontSize = 4;

        DrawSimpleText(image, text, width, height, fontSize, fgColor);

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    private void DrawSimpleText(Image<Rgba32> image, string text, int width, int height, int fontSize, Rgba32 color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var font = GetFont(fontSize);
        var textOptions = new RichTextOptions(font)
        {
            Origin = new PointF(width / 2f, height / 2f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var textSize = TextMeasurer.MeasureSize(text, textOptions);
        
        var adjustedX = (width - textSize.Width) / 2;
        var adjustedY = (height - textSize.Height) / 2;
        
        textOptions.Origin = new PointF(adjustedX + textSize.Width / 2, adjustedY + textSize.Height / 2);

        image.Mutate(ctx => ctx.DrawText(textOptions, text, color));
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
        int width = DefaultTextureSize)
    {
        var aspectRatio = definition.DefaultWidth / definition.DefaultHeight;
        var height = (int)(width / aspectRatio);
        
        return CreateSignTextureWithText(definition, signColor, customText, width, height);
    }

    public byte[] CreateSignTextureWithText(
        SignDefinition definition,
        SignColor signColor,
        string? customText,
        int width,
        int height)
    {
        return CreateSignTextureWithText(definition, signColor, customText, width, height, definition.DefaultHeight * 0.15);
    }

    public byte[] CreateSignTextureWithText(
        SignDefinition definition,
        SignColor signColor,
        string? customText,
        int width,
        int height,
        double fontHeight)
    {
        var baseTexture = GenerateBaseTexture(definition, signColor, width, height);
        
        if (string.IsNullOrEmpty(customText))
        {
            return baseTexture;
        }

        var textAreaHeight = height / 2;
        var textTexture = GenerateTextTexture(
            customText,
            fontHeight,
            SignColor.Svart,
            SignColor.Hvit,
            width,
            textAreaHeight,
            definition.DefaultHeight);

        return CompositeTextures(baseTexture, textTexture, 0, (height - textAreaHeight) / 2);
    }

    public byte[] GenerateTransparentTextTexture(
        string text,
        double fontHeight,
        SignColor textColor,
        int width,
        int height,
        double signHeight)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<byte>();
        }

        using var image = new Image<Rgba32>(width, height);
        
        var fgColor = ColorMap.TryGetValue(textColor, out var fg) ? fg : new Rgba32(0, 0, 0, 255);
        
        var fontSize = (int)(fontHeight * height / signHeight * FontSizeMultiplier);
        if (fontSize < 4) fontSize = 4;

        DrawSimpleText(image, text, width, height, fontSize, fgColor);

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public async Task<byte[]?> LoadImageFromFileAsync(string filePath, int targetSize = DefaultTextureSize)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var imageBytes = await File.ReadAllBytesAsync(filePath);
            return ResizeImage(imageBytes, targetSize, targetSize);
        }
        catch
        {
            return null;
        }
    }

    public byte[]? LoadImageFromFile(string filePath, int targetSize = DefaultTextureSize)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var imageBytes = File.ReadAllBytes(filePath);
            return ResizeImage(imageBytes, targetSize, targetSize);
        }
        catch
        {
            return null;
        }
    }

    public byte[] ResizeImage(byte[] imageBytes, int targetWidth, int targetHeight)
    {
        using var inputStream = new MemoryStream(imageBytes);
        using var image = Image.Load<Rgba32>(inputStream);
        
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(targetWidth, targetHeight),
            Mode = ResizeMode.Stretch
        }));

        using var outputStream = new MemoryStream();
        image.SaveAsPng(outputStream);
        return outputStream.ToArray();
    }

    public string? FindSignImage(string signsBaseDirectory, string signNumber, SignCategory category)
    {
        var categoryFolder = category switch
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
            _ => null
        };

        if (categoryFolder == null || !Directory.Exists(signsBaseDirectory))
        {
            return null;
        }

        var categoryDir = Path.Combine(signsBaseDirectory, categoryFolder);
        if (!Directory.Exists(categoryDir))
        {
            return null;
        }

        var baseSignNumber = signNumber.Split('.')[0];
        var variant = signNumber.Contains('.') ? signNumber.Split('.')[1].Split(' ')[0] : "";

        var files = Directory.GetFiles(categoryDir, "*.jpg");
        
        string? exactMatch = null;
        string? baseMatch = null;
        
        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            
            var match = System.Text.RegularExpressions.Regex.Match(fileName, @"^(\d+)(?:_(\d+|[A-Z]))?\b");
            if (!match.Success)
            {
                continue;
            }

            var fileSignNumber = match.Groups[1].Value;
            var fileVariant = match.Groups[2].Success ? match.Groups[2].Value : "";

            if (fileSignNumber == baseSignNumber)
            {
                if (!string.IsNullOrEmpty(variant) && fileVariant == variant)
                {
                    exactMatch = file;
                }
                else if (string.IsNullOrEmpty(fileVariant))
                {
                    baseMatch = file;
                }
            }
        }

        return exactMatch ?? baseMatch;
    }

    public byte[] CreateSignTextureFromImage(
        string? imagePath,
        SignDefinition definition,
        int size = DefaultTextureSize)
    {
        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            var imageBytes = File.ReadAllBytes(imagePath);
            return ResizeImage(imageBytes, size, size);
        }

        return GenerateBaseTexture(definition, definition.PrimaryColor, size);
    }

    public byte[] ResizeImageFromFile(
        string imagePath,
        int targetWidth,
        int targetHeight)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("Image not found", imagePath);
        }

        var imageBytes = File.ReadAllBytes(imagePath);
        return ResizeImage(imageBytes, targetWidth, targetHeight);
    }

    public (int width, int height) GetImageDimensions(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            return (0, 0);
        }

        try
        {
            using var stream = File.OpenRead(imagePath);
            var info = Image.Identify(stream);
            return (info.Width, info.Height);
        }
        catch
        {
            return (0, 0);
        }
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

    private void DrawRectangleBorder(Image<Rgba32> image, int width, int height, int margin, int borderWidth, Rgba32 borderColor)
    {
        // Top border
        for (int y = margin; y < margin + borderWidth && y < height; y++)
        {
            for (int x = margin; x < width - margin; x++)
            {
                image[x, y] = borderColor;
            }
        }
        // Bottom border
        for (int y = height - margin - borderWidth; y < height - margin && y >= 0; y++)
        {
            for (int x = margin; x < width - margin; x++)
            {
                image[x, y] = borderColor;
            }
        }
        // Left border
        for (int x = margin; x < margin + borderWidth && x < width; x++)
        {
            for (int y = margin; y < height - margin; y++)
            {
                image[x, y] = borderColor;
            }
        }
        // Right border
        for (int x = width - margin - borderWidth; x < width - margin && x >= 0; x++)
        {
            for (int y = margin; y < height - margin; y++)
            {
                image[x, y] = borderColor;
            }
        }
    }

    private void DrawCircleBorder(Image<Rgba32> image, int width, int height, int borderWidth, Rgba32 borderColor)
    {
        var centerX = width / 2f;
        var centerY = height / 2f;
        var radius = Math.Min(width, height) / 2f - borderWidth / 2f;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                var dist = Math.Sqrt(dx * dx + dy * dy);
                
                if (dist >= radius - borderWidth / 2 && dist <= radius + borderWidth / 2)
                {
                    image[x, y] = borderColor;
                }
            }
        }
    }

    private void DrawTriangleBorder(Image<Rgba32> image, int width, int height, int borderWidth, Rgba32 borderColor)
    {
        var size = Math.Min(width, height);
        var points = new (float x, float y)[]
        {
            (width / 2f, borderWidth),
            (borderWidth, height - borderWidth),
            (width - borderWidth, height - borderWidth)
        };
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
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

    private void DrawOctagonBorder(Image<Rgba32> image, int width, int height, int borderWidth, Rgba32 borderColor)
    {
        var margin = borderWidth;
        var corner = (int)(Math.Min(width, height) * 0.15f);
        
        var points = new (float x, float y)[]
        {
            (width / 2f, margin),
            (width - corner, margin),
            (width - margin, corner),
            (width - margin, height - corner),
            (width - corner, height - margin),
            (corner, height - margin),
            (margin, height - corner),
            (margin, corner)
        };
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (IsPointNearPolygonLine(x, y, points, borderWidth))
                {
                    image[x, y] = borderColor;
                }
            }
        }
    }

    private void DrawDiamondBorder(Image<Rgba32> image, int width, int height, int borderWidth, Rgba32 borderColor)
    {
        var centerX = width / 2f;
        var centerY = height / 2f;
        var inset = borderWidth;
        
        var points = new (float x, float y)[]
        {
            (centerX, inset),
            (width - inset, centerY),
            (centerX, height - inset),
            (inset, centerY)
        };
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
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
