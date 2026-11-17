using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HdrBooth.Services.Camera;

/// <summary>
/// Test-only camera provider. It generates a preview image and does NOT connect to a physical camera.
/// </summary>
public sealed class DemoCameraProvider : ICameraProvider
{
    public Task<CameraStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new CameraStatus(CameraConnectionState.Demo, "HDR BOOTH Demo Camera",
            "Mode demo aktif — bukan gambar dari kamera fisik."));

    public Task<BitmapSource> CaptureAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const int width = 1280, height = 720;
        var visual = new DrawingVisual();
       using (var dc = visual.RenderOpen())
{
    dc.DrawRectangle(
        new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(35, 39, 48)),
        null,
        new System.Windows.Rect(0, 0, width, height));

    dc.DrawRectangle(
        new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(217, 45, 58)),
        null,
        new System.Windows.Rect(0, 0, width, 12));

    var title = new FormattedText(
        "HDR BOOTH • DEMO CAPTURE",
        CultureInfo.GetCultureInfo("id-ID"),
        System.Windows.FlowDirection.LeftToRight,
        new Typeface("Segoe UI"),
        42,
        System.Windows.Media.Brushes.White,
        1.0);

    dc.DrawText(title, new System.Windows.Point(60, 260));

    var subtitle = new FormattedText(
        DateTime.Now.ToString("dd MMM yyyy • HH:mm:ss"),
        CultureInfo.GetCultureInfo("id-ID"),
        System.Windows.FlowDirection.LeftToRight,
        new Typeface("Segoe UI"),
        24,
        System.Windows.Media.Brushes.LightGray,
        1.0);

    dc.DrawText(subtitle, new System.Windows.Point(64, 325));

    dc.DrawEllipse(
        null,
        new System.Windows.Media.Pen(
            new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(217, 45, 58)),
            5),
        new System.Windows.Point(width - 120, 90),
        28,
        28);
}
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(35, 39, 48)), null, new Rect(0, 0, width, height));
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(217, 45, 58)), null, new Rect(0, 0, width, 12));
            var title = new FormattedText("HDR BOOTH • DEMO CAPTURE",
                CultureInfo.GetCultureInfo("id-ID"), FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 42, Brushes.White, 1.0);
            dc.DrawText(title, new Point(60, 260));
            var subtitle = new FormattedText(DateTime.Now.ToString("dd MMMM yyyy • HH:mm:ss", CultureInfo.GetCultureInfo("id-ID")),
                CultureInfo.GetCultureInfo("id-ID"), FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 24, Brushes.LightGray, 1.0);
            dc.DrawText(subtitle, new Point(64, 325));
            dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(217,45,58)), 5),
                new Point(width - 120, 90), 28, 28);
        }
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return Task.FromResult<BitmapSource>(bitmap);
    }
}
