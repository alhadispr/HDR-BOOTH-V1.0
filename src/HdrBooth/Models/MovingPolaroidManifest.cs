namespace HdrBooth.Models;

/// <summary>
/// Describes one Moving Polaroid experience. This is an asset manifest;
/// mobile image tracking is implemented in a later milestone.
/// </summary>
public sealed class MovingPolaroidManifest
{
    public string Product { get; set; } = "HDR BOOTH";
    public string Feature { get; set; } = "Moving Polaroid";
    public string Status { get; set; } = "ASSET_PACKAGE_READY_AR_TRACKING_PENDING";
    public string TrackingMode { get; set; } = "IMAGE_TARGET";
    public Guid EventId { get; set; }
    public string EventName { get; set; } = "";
    public Guid? SessionId { get; set; }
    public int PhotoIndex { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string TargetImage { get; set; } = "target-image.png";
    public string OverlayVideo { get; set; } = "overlay-video.mp4";
    public string ArtworkUrl { get; set; } = "";
    public string ArtworkQrFile { get; set; } = "";
    public DateTime? PublishedAt { get; set; }
    public string Notes { get; set; } = "AR image tracking viewer is not included in this package version.";
}
