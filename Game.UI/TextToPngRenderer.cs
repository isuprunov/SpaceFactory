using SkiaSharp;
using System.IO;

public class TextToPngRenderer
{
    private readonly int _width;
    private readonly int _height;
    private readonly string _fontFamily;

    public TextToPngRenderer(int width, int height, string fontFamily)
    {
        _width = width;
        _height = height;
        _fontFamily = fontFamily;
    }

    public MemoryStream Render(string text)
    {
        var surface = SKSurface.Create(new SKImageInfo(_width, _height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        using var typeface = SKFontManager.Default.MatchFamily(_fontFamily)
                            ?? SKFontManager.Default.MatchFamily("Arial")
                            ?? SKTypeface.Default;

        float fontSize = FindMaxFontSize(text, typeface);

        using var font = new SKFont(typeface, fontSize)
        {
            Edging = SKFontEdging.Antialias,
            Subpixel = true
        };

        using var paint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true
        };

        var metrics = font.Metrics;
        float lineHeight = metrics.Descent - metrics.Ascent;
        float x = 0;
        float y = -metrics.Ascent;

        foreach (char ch in text)
        {
            var s = ch.ToString();
            float glyphWidth = font.MeasureText(s);

            if (x + glyphWidth > _width)
            {
                x = 0;
                y += lineHeight;
            }

            if (y + metrics.Descent > _height)
                break;

            canvas.DrawText(s, x, y, font, paint);
            x += glyphWidth;
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        if (data == null)
            throw new InvalidOperationException("Failed to encode image.");

        var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    private float FindMaxFontSize(string text, SKTypeface typeface)
    {
        for (float size = 200; size >= 1; size--)
        {
            using var font = new SKFont(typeface, size)
            {
                Edging = SKFontEdging.Antialias,
                Subpixel = true
            };

            var metrics = font.Metrics;
            float lineHeight = metrics.Descent - metrics.Ascent;
            float x = 0;
            float y = -metrics.Ascent;

            bool fits = true;

            foreach (char ch in text)
            {
                float glyphWidth = font.MeasureText(ch.ToString());

                if (x + glyphWidth > _width)
                {
                    x = 0;
                    y += lineHeight;
                }

                if (y + metrics.Descent > _height)
                {
                    fits = false;
                    break;
                }

                x += glyphWidth;
            }

            if (fits)
                return size;
        }

        return 1;
    }
}
