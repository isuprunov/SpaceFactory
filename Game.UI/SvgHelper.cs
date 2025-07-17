using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;
using SKSvg = Svg.Skia.SKSvg;

namespace Game.UI;

public static class SvgHelper
{
    public static Bitmap? LoadSvg(string avaresPath, int width, int height)
    {
        if (AssetLoader.Exists(new Uri(avaresPath)) == false)
            return null;
        // Открываем SVG как stream
        using var stream = AssetLoader.Open(new Uri(avaresPath));
        if (stream == null)
            throw new FileNotFoundException($"Asset not found: {avaresPath}");

        // Загружаем SVG
        var svg = new SKSvg();
        svg.Load(stream); // Svg.Skia. В Extended вариант аналогичный

        var picture = svg.Picture
                      ?? throw new InvalidOperationException("SVG.Picture is null");

        var bounds = picture.CullRect;
        float scaleX = width / bounds.Width;
        float scaleY = height / bounds.Height;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(scaleX, scaleY);
        canvas.DrawPicture(picture);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        if (data == null)
            throw new InvalidOperationException("PNG encode returned null");

        var ms = new MemoryStream();
        data.SaveTo(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return new Bitmap(ms);
    }
}