using SkiaSharp;

namespace Pantreats.Services
{
    public static class ImageOptimizerService
    {
        public static void OptimizeItemImages(IWebHostEnvironment env)
        {
            var folderPath = Path.Combine(env.WebRootPath, "images", "items");

            if (!Directory.Exists(folderPath))
                return;

            var imageFiles = Directory.GetFiles(folderPath, "*.png");

            foreach (var file in imageFiles)
            {
                var webpFile = Path.ChangeExtension(file, ".webp");

                if (File.Exists(webpFile))
                    continue;

                using var input = File.OpenRead(file);
                using var original = SKBitmap.Decode(input);

                if (original == null)
                    continue;

                if (original.Width == 600 && original.Height == 600)
                    continue;

                var maxSize = 600;
                var ratio = Math.Min(
                    (double)maxSize / original.Width,
                    (double)maxSize / original.Height
                );

                var newWidth = (int)(original.Width * ratio);
                var newHeight = (int)(original.Height * ratio);

                using var resized = original.Resize(
                new SKImageInfo(newWidth, newHeight),
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)
                );

                if (resized == null)
                    continue;

                using var image = SKImage.FromBitmap(resized);
                using var data = image.Encode(SKEncodedImageFormat.Webp, 80);

                File.WriteAllBytes(webpFile, data.ToArray());
            }
        }
    }
}
