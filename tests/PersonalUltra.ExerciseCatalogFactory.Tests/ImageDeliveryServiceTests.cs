using PersonalUltra.ExerciseCatalogFactory.Images;
using SkiaSharp;

namespace PersonalUltra.ExerciseCatalogFactory.Tests;

public sealed class ImageDeliveryServiceTests
{
    [Fact]
    public void Delivery_derivative_is_a_640_square_webp_smaller_than_the_png_master()
    {
        using var bitmap = new SKBitmap(1024, 1024);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Black);
            using var paint = new SKPaint { Color = new SKColor(255, 106, 19), IsAntialias = true };
            canvas.DrawCircle(512, 512, 360, paint);
        }
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100)!;
        var png = encoded.ToArray();

        var webp = ImageDeliveryService.CreateWebp(png);

        using var result = SKBitmap.Decode(webp);
        Assert.NotNull(result);
        Assert.Equal(640, result.Width);
        Assert.Equal(640, result.Height);
        Assert.True(webp.Length < png.Length);
    }
}
