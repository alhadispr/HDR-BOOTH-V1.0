namespace HdrBooth.Models;

public sealed class FrameTemplate
{
    public string Name { get; set; } = "Template Frame";
    public int CanvasWidth { get; set; } = 1200;
    public int CanvasHeight { get; set; } = 1800;
    public string OverlayImagePath { get; set; } = "";
    public List<FramePhotoSlot> Slots { get; set; } = new();
}

/// <summary>Slot coordinates are percentages (0-100) relative to the final canvas.</summary>
public sealed class FramePhotoSlot
{
    public string Name { get; set; } = "Foto 1";
    public double X { get; set; } = 10;
    public double Y { get; set; } = 10;
    public double Width { get; set; } = 80;
    public double Height { get; set; } = 18;
}
