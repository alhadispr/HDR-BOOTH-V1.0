using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HdrBooth.Services;

/// <summary>Local photo processing utilities. Frame PNG must have transparent areas where the photo should show.</summary>
public sealed class PhotoProcessingService
{
    public string ComposePhotoWithFrame(string photoPath, string framePngPath, string outputPath)
    {
        if (!File.Exists(photoPath)) throw new FileNotFoundException("Foto sumber tidak ditemukan.", photoPath);
        if (!File.Exists(framePngPath)) throw new FileNotFoundException("File frame PNG tidak ditemukan.", framePngPath);

        var photo = LoadBitmap(photoPath);
        var frame = LoadBitmap(framePngPath);
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(photo, new Rect(0, 0, photo.PixelWidth, photo.PixelHeight));
            // Frame is scaled to the photo canvas; transparent frame regions reveal the photo.
            dc.DrawImage(frame, new Rect(0, 0, photo.PixelWidth, photo.PixelHeight));
        }

        var rendered = new RenderTargetBitmap(photo.PixelWidth, photo.PixelHeight, 96, 96, PixelFormats.Pbgra32);
        rendered.Render(visual);
        SavePng(rendered, outputPath);
        return outputPath;
    }

    /// <summary>
    /// Fills each configured percentage-based slot with a center-cropped source photo,
    /// then draws the transparent PNG frame on top. The frame's pixel dimensions define output size.
    /// </summary>
    public string ComposePhotoLayout(IReadOnlyList<string> photoPaths, HdrBooth.Models.FrameTemplate template, string outputPath)
    {
        if (photoPaths.Count == 0) throw new ArgumentException("Minimal satu foto diperlukan.", nameof(photoPaths));
        if (template.Slots.Count == 0) throw new ArgumentException("Template belum memiliki slot foto.", nameof(template));
        if (!File.Exists(template.OverlayImagePath)) throw new FileNotFoundException("PNG frame template tidak ditemukan.", template.OverlayImagePath);

        var frame = LoadBitmap(template.OverlayImagePath);
        int width = frame.PixelWidth;
        int height = frame.PixelHeight;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(0, 0, width, height));
            for (int i = 0; i < template.Slots.Count; i++)
            {
                if (i >= photoPaths.Count) break;
                var slot = template.Slots[i];
                if (!File.Exists(photoPaths[i])) throw new FileNotFoundException("Foto slot tidak ditemukan.", photoPaths[i]);

                double x = Math.Clamp(slot.X, 0, 100) / 100d * width;
                double y = Math.Clamp(slot.Y, 0, 100) / 100d * height;
                double w = Math.Clamp(slot.Width, 0, 100) / 100d * width;
                double h = Math.Clamp(slot.Height, 0, 100) / 100d * height;
                if (w <= 0 || h <= 0) continue;

                var photo = LoadBitmap(photoPaths[i]);
                var cropped = CropToAspect(photo, w / h);
                dc.DrawImage(cropped, new Rect(x, y, w, h));
            }
            // The transparent frame is always the top-most layer.
            dc.DrawImage(frame, new Rect(0, 0, width, height));
        }

        var rendered = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        rendered.Render(visual);
        SavePng(rendered, outputPath);
        return outputPath;
    }

    private static BitmapSource CropToAspect(BitmapSource source, double targetAspect)
    {
        double sourceAspect = (double)source.PixelWidth / source.PixelHeight;
        int cropWidth = source.PixelWidth;
        int cropHeight = source.PixelHeight;
        if (sourceAspect > targetAspect)
            cropWidth = Math.Max(1, (int)Math.Round(source.PixelHeight * targetAspect));
        else
            cropHeight = Math.Max(1, (int)Math.Round(source.PixelWidth / targetAspect));

        int x = (source.PixelWidth - cropWidth) / 2;
        int y = (source.PixelHeight - cropHeight) / 2;
        var crop = new CroppedBitmap(source, new Int32Rect(x, y, cropWidth, cropHeight));
        crop.Freeze();
        return crop;
    }

    public string CreateAnimatedGif(IReadOnlyList<string> imagePaths, string outputPath, int frameDelayMilliseconds = 700)
    {
        if (imagePaths.Count < 2) throw new ArgumentException("GIF membutuhkan minimal dua foto.", nameof(imagePaths));
        // Kept in the API for future encoder replacement; WPF GifBitmapEncoder does not
        // expose a reliable per-frame delay API through its public surface.
        _ = frameDelayMilliseconds;
        var encoder = new GifBitmapEncoder();
        foreach (var path in imagePaths)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Salah satu foto GIF tidak ditemukan.", path);
            var frame = LoadBitmap(path);
            // Use the universally supported BitmapFrame overload. The encoder writes
            // each image frame; delay metadata is not guaranteed by WPF's GIF encoder.
            encoder.Frames.Add(BitmapFrame.Create(frame));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        encoder.Save(stream);
        return outputPath;
    }

    private static BitmapSource LoadBitmap(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(Path.GetFullPath(path));
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private static void SavePng(BitmapSource bitmap, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        encoder.Save(stream);
    }
}
