using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

var sourcePath = "image.png";
var outputDir = "Output";
Directory.CreateDirectory(outputDir);

using var full = Image.Load<Rgba32>(sourcePath);

var sizeX = full.Width /2;
var sizeY = full.Height /2;

(string name, int x, int y)[] sectors = new[]
{
    ("1", 0, 0),
    ("2", sizeX, 0),
    ("3", 0, sizeY),
    ("4", sizeX, sizeY)
};

foreach (var (name, x, y) in sectors)
{
    var crop = full.Clone(ctx => ctx.Crop(new Rectangle(x, y, sizeX, sizeY)));
    var trimmed = Trim(crop);
    if(trimmed != null)
        await trimmed.SaveAsync(Path.Combine(outputDir, $"{name}.png"), new PngEncoder());
}

return;

Image<Rgba32>? Trim(Image<Rgba32> source)
{
    int left = 0, right = source.Width, top = 0, bottom = 0;
    const int maxA = 200;
    bool found;

    int x;
    int y;
    
    found = false;
    for (y = 0; y < source.Height && found == false; y++)
    for (x = 0; x < source.Width && found == false; x++)
        if (source[x, y].A > maxA)
            found = true;
    top = y;
    
    found = false;
    for (y = source.Height - 1; y >= 0  && found == false; y--)
    for (x = 0; x < source.Width && found == false; x++)
        if (source[x, y].A > maxA)
            found = true;
    bottom = y;
    
    found = false;
    for (x = 0; x < source.Width && found == false; x++)
    for (y = 0; y < source.Height && found == false; y++)
        if (source[x, y].A > maxA)
            found = true;
    left = x;
    
    found = false;
    for (x = source.Width -1; x >= 0  && found == false; x--)
    for (y = 0; y < source.Height && found == false; y++)
        if (source[x, y].A > maxA)
            found = true;

    right = x;
    
    if(top > bottom || left > right)
        return null;
    // for(x = 0; x< source.Width; x++)
    //     source[x, top] = Rgba32.ParseHex("#FF0000");

    var box = new Rectangle(left, top, right - left, bottom - top);
    //var box = new Rectangle(0,0 , source.Width, source.Height);
    return source.Clone(ctx => ctx.Crop(box));
}