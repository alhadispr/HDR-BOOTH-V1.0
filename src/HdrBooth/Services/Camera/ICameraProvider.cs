using System.Windows.Media.Imaging;

namespace HdrBooth.Services.Camera;

public enum CameraConnectionState { Disconnected, Connected, Demo }

public sealed record CameraStatus(CameraConnectionState State, string DeviceName, string Message);

public interface ICameraProvider
{
    Task<CameraStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<BitmapSource> CaptureAsync(CancellationToken cancellationToken = default);
}
