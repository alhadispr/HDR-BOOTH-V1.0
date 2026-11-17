using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HdrBooth.Services;

public sealed class WindowsPrintService
{
    public IReadOnlyList<string> GetInstalledPrinters()
    {
        try
        {
            using var server = new LocalPrintServer();
            return server.GetPrintQueues()
                .Select(q => q.Name)
                .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public bool PrintPhoto(Window owner, string imagePath, string printerName, string paperSize, int copies)
    {
        if (!File.Exists(imagePath)) throw new FileNotFoundException("File foto untuk dicetak tidak ditemukan.", imagePath);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(Path.GetFullPath(imagePath));
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        var dialog = new PrintDialog { UserPageRangeEnabled = false };
        if (!string.IsNullOrWhiteSpace(printerName))
        {
            using var server = new LocalPrintServer();
            var queue = server.GetPrintQueues().FirstOrDefault(q =>
                string.Equals(q.Name, printerName, StringComparison.OrdinalIgnoreCase));
            if (queue is not null) dialog.PrintQueue = queue;
        }

        var (widthInches, heightInches) = paperSize switch
        {
            "2x6 inci" => (2d, 6d),
            "5x7 inci" => (5d, 7d),
            "A4" => (8.27d, 11.69d),
            _ => (4d, 6d)
        };
        dialog.PrintTicket ??= new PrintTicket();
        dialog.PrintTicket.CopyCount = Math.Clamp(copies, 1, 20);
        dialog.PrintTicket.PageMediaSize = new PageMediaSize(widthInches * 96d, heightInches * 96d);

        if (dialog.ShowDialog() != true) return false;

        double pageWidth = widthInches * 96d;
        double pageHeight = heightInches * 96d;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, pageWidth, pageHeight));
            double scale = Math.Min(pageWidth / bitmap.PixelWidth, pageHeight / bitmap.PixelHeight);
            double w = bitmap.PixelWidth * scale;
            double h = bitmap.PixelHeight * scale;
            dc.DrawImage(bitmap, new Rect((pageWidth - w) / 2, (pageHeight - h) / 2, w, h));
        }
        visual.Transform = Transform.Identity;
        dialog.PrintVisual(visual, "HDR BOOTH Photo");
        return true;
    }
}
