using System.IO;
using System.Windows.Media.Imaging;
using HdrBooth.Models;

namespace HdrBooth.Services.Camera;

public sealed class CameraCaptureService
{
    private readonly ICameraProvider _provider;
    private readonly EventStore _store;

    public CameraCaptureService(ICameraProvider provider, EventStore store)
    {
        _provider = provider;
        _store = store;
    }

    public Task<CameraStatus> GetStatusAsync(CancellationToken ct = default) => _provider.GetStatusAsync(ct);

    public async Task<string> CaptureOriginalAsync(EventProfile profile, CancellationToken ct = default)
    {
        var bitmap = await _provider.CaptureAsync(ct);
        var folder = Path.Combine(_store.GetEventFolder(profile), "original");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"HDR_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);
        return path;
    }
}
