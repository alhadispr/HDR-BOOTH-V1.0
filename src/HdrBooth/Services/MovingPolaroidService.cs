using System.IO;
using System.Text.Json;
using QRCoder;
using HdrBooth.Models;

namespace HdrBooth.Services;

/// <summary>
/// Exports trigger image + video assets in a folder ready for manual import into Artivive Bridge.
/// This service does not upload or publish to Artivive; no official public API integration is configured.
/// </summary>
public sealed class MovingPolaroidService
{
    public string CreatePackage(EventProfile eventProfile, Guid? sessionId, int photoIndex,
        string targetImagePath, string overlayVideoPath, string eventFolder)
    {
        if (!File.Exists(targetImagePath))
            throw new FileNotFoundException("Gambar trigger Artivive tidak ditemukan.", targetImagePath);
        if (!File.Exists(overlayVideoPath))
            throw new FileNotFoundException("Video overlay tidak ditemukan.", overlayVideoPath);
        if (!string.Equals(Path.GetExtension(overlayVideoPath), ".mp4", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Video overlay harus berformat MP4 agar siap diimpor ke Artivive.");

        var info = new FileInfo(overlayVideoPath);
        if (info.Length > 100L * 1024 * 1024)
            throw new InvalidOperationException("Ukuran video melebihi 100 MB. Kompres video sebelum menyiapkan paket Artivive.");

        var packageRoot = Path.Combine(eventFolder, "artivive-export");
        var packageName = $"ARTIVIVE_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}"[..30];
        var packageFolder = Path.Combine(packageRoot, packageName);
        Directory.CreateDirectory(packageFolder);

        var ext = Path.GetExtension(targetImagePath).Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                  Path.GetExtension(targetImagePath).Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ? ".jpg" : ".png";
        var targetName = "01_TRIGGER_IMAGE" + ext;
        var videoName = "02_VIDEO_OVERLAY.mp4";
        File.Copy(targetImagePath, Path.Combine(packageFolder, targetName), true);
        File.Copy(overlayVideoPath, Path.Combine(packageFolder, videoName), true);

        var manifest = new MovingPolaroidManifest
        {
            EventId = eventProfile.Id,
            EventName = eventProfile.Name,
            SessionId = sessionId,
            PhotoIndex = photoIndex,
            TargetImage = targetName,
            OverlayVideo = videoName,
            Status = "ARTIVIVE_BRIDGE_IMPORT_READY_MANUAL_PUBLISH_REQUIRED",
            Notes = "Import trigger image and video overlay into Artivive Bridge, position the video layer, then publish."
        };
        File.WriteAllText(Path.Combine(packageFolder, "manifest.json"),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

        File.WriteAllText(Path.Combine(packageFolder, "ARTIVIVE-IMPORT-GUIDE.txt"), """
        HDR BOOTH — PAKET IMPOR ARTIVIVE
        ================================

        FILE DI DALAM FOLDER:
        1. 01_TRIGGER_IMAGE.(jpg/png) — unggah sebagai Trigger Image di Artivive Bridge.
        2. 02_VIDEO_OVERLAY.mp4 — unggah sebagai Video Layer.
        3. manifest.json — metadata event dan pasangan aset.

        LANGKAH DI ARTIVIVE:
        1. Buka Artivive Bridge / Editor dan login ke Creator Account.
        2. Pilih Create Artwork.
        3. Upload 01_TRIGGER_IMAGE sebagai Trigger Image.
        4. Tambahkan Video Layer dan upload 02_VIDEO_OVERLAY.mp4.
        5. Atur posisi, ukuran, dan crop video agar tepat menutupi area foto.
        6. Preview hasilnya, lalu Publish.
        7. Untuk pengalaman tanpa aplikasi, aktifkan/pilih WebAR saat tersedia dan gunakan QR/link yang diberikan Artivive.
           Jika menggunakan Artivive App, pelanggan membuka aplikasi lalu memindai Trigger Image.

        CATATAN PENTING:
        - Paket ini disiapkan HDR BOOTH untuk impor manual; belum mengunggah atau menerbitkan artwork otomatis.
        - QR Sharing lokal HDR BOOTH bukan QR WebAR Artivive.
        - Artivive menyarankan trigger image yang tajam, kontras, dan memiliki detail visual unik.
        - Panduan Artivive menyebut video hingga 100 MB dan durasi di bawah 45 detik untuk alur umum.
        - Batas gambar dan fitur dapat bergantung pada paket akun Artivive.
        - Jangan masukkan QR code ke dalam trigger image; QR WebAR harus terpisah.
        """);
        return packageFolder;
    }

    public string SaveArtworkLinkAndQr(string packageFolder, string artworkUrl)
    {
        if (!Uri.TryCreate(artworkUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new InvalidOperationException("Masukkan tautan artwork Artivive yang valid (https:// atau http://).");

        var manifestPath = Path.Combine(packageFolder, "manifest.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("Manifest paket Artivive tidak ditemukan.", manifestPath);

        var manifest = JsonSerializer.Deserialize<MovingPolaroidManifest>(File.ReadAllText(manifestPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? throw new InvalidDataException("Manifest Artivive tidak dapat dibaca.");
        manifest.ArtworkUrl = uri.ToString();
        manifest.ArtworkQrFile = "artwork-qr.png";
        manifest.PublishedAt = DateTime.Now;
        manifest.Status = "ARTWORK_LINK_SAVED_QR_READY";
        manifest.Notes = "Artwork URL saved from user input. Confirm that the URL is a published Artivive WebAR link before customer use.";
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(uri.ToString(), QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        var bytes = qr.GetGraphic(10);
        var qrPath = Path.Combine(packageFolder, "artwork-qr.png");
        File.WriteAllBytes(qrPath, bytes);
        File.WriteAllText(Path.Combine(packageFolder, "ARTWORK-LINK.txt"),
            "ARTIVIVE ARTWORK URL\\n" + uri + "\\n\\nQR: artwork-qr.png\\n");
        return qrPath;
    }
}
