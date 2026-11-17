namespace HdrBooth.Models;

public sealed class EventProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Acara Baru";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? EventDate { get; set; }
    public bool IsActive { get; set; }
    public EventSettings Settings { get; set; } = new();
}

/// <summary>All booth preferences belong to one event profile; never shared between events.</summary>
public sealed class EventSettings
{
    // Welcome and closing screens
    public string WelcomeTitle { get; set; } = "Selamat Datang";
    public string WelcomeSubtitle { get; set; } = "Abadikan momen spesial Anda";
    public string ClosingTitle { get; set; } = "Terima Kasih";
    public string ClosingSubtitle { get; set; } = "Sampai bertemu kembali";

    // Session and layout
    public int CountdownSeconds { get; set; } = 3;
    public int PhotoCount { get; set; } = 4;
    public int RetakeLimit { get; set; } = 1;
    public string FrameFolder { get; set; } = "";
    public string FrameTemplatePath { get; set; } = "";
    public string LayoutName { get; set; } = "Strip 4 Foto";

    // Devices
    public string CameraId { get; set; } = "";
    public string CameraMode { get; set; } = "Auto";
    public string PrinterName { get; set; } = "";
    public string PrintSize { get; set; } = "4x6 inci";
    public int PrintCopies { get; set; } = 1;

    // Sharing and access
    public bool EnableQrSharing { get; set; } = true;
    public bool EnableCloudUpload { get; set; }
    public bool RequirePayment { get; set; }
    public bool EnableVoucher { get; set; }
    public string VoucherCode { get; set; } = "";

    // HDR BOOTH outputs
    public bool EnableGif { get; set; } = true;
    public bool EnableLivePhoto { get; set; } = true;
    public bool SaveOriginalPhotos { get; set; } = true;
    public bool EnableMovingPolaroid { get; set; }
    public bool SaveFramedPhotos { get; set; } = true;
}
