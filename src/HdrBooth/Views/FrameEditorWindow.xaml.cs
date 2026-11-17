using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using HdrBooth.Models;

namespace HdrBooth.Views;

public partial class FrameEditorWindow : Window
{
    private readonly EventProfile _profile;
    private readonly string _templatePath;
    private readonly ObservableCollection<FramePhotoSlot> _slots = new();
    private FrameTemplate _template = new();
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public FrameEditorWindow(EventProfile profile)
    {
        InitializeComponent();
        _profile = profile;
        EventTitle.Text = profile.Name;
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HDR-BOOTH", "events", profile.Id.ToString("N"));
        Directory.CreateDirectory(root);
        _templatePath = Path.Combine(root, "frame-template.json");
        SlotsGrid.ItemsSource = _slots;
        LoadTemplate();
        SlotOverlay.SizeChanged += (_, _) => DrawSlots();
    }

    private void LoadTemplate()
    {
        if (File.Exists(_templatePath))
        {
            try { _template = JsonSerializer.Deserialize<FrameTemplate>(File.ReadAllText(_templatePath), _json) ?? new(); }
            catch { _template = new(); }
        }
        else if (!string.IsNullOrWhiteSpace(_profile.Settings.FrameTemplatePath) &&
                 File.Exists(_profile.Settings.FrameTemplatePath))
        {
            try { _template = JsonSerializer.Deserialize<FrameTemplate>(File.ReadAllText(_profile.Settings.FrameTemplatePath), _json) ?? new(); }
            catch { _template = new(); }
        }

        if (_template.Slots.Count == 0)
        {
            for (int i = 0; i < Math.Clamp(_profile.Settings.PhotoCount, 1, 12); i++)
                _template.Slots.Add(new FramePhotoSlot { Name = $"Foto {i + 1}", X = 10, Y = 8 + i * (82.0 / Math.Clamp(_profile.Settings.PhotoCount, 1, 12)), Width = 80, Height = 82.0 / Math.Clamp(_profile.Settings.PhotoCount, 1, 12) - 2 });
        }
        _slots.Clear();
        foreach (var slot in _template.Slots) _slots.Add(slot);
        LoadPreview();
    }

    private void LoadPreview()
    {
        var path = _template.OverlayImagePath;
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit(); bitmap.UriSource = new Uri(path); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.EndInit();
                FramePreview.Source = bitmap;
                EmptyHint.Visibility = Visibility.Collapsed;
                FramePathText.Text = path;
            }
            catch { FramePreview.Source = null; EmptyHint.Visibility = Visibility.Visible; }
        }
        else
        {
            FramePreview.Source = null;
            EmptyHint.Visibility = Visibility.Visible;
            FramePathText.Text = "Belum ada PNG frame";
        }
        Dispatcher.InvokeAsync(DrawSlots);
    }

    private void DrawSlots()
    {
        SlotOverlay.Children.Clear();
        double w = SlotOverlay.ActualWidth, h = SlotOverlay.ActualHeight;
        if (w <= 0 || h <= 0) return;
        foreach (var slot in _slots)
        {
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = Math.Max(0, w * Math.Clamp(slot.Width, 0, 100) / 100),
                Height = Math.Max(0, h * Math.Clamp(slot.Height, 0, 100) / 100),
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(217, 45, 58)),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(35, 217, 45, 58)),
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            Canvas.SetLeft(rect, w * Math.Clamp(slot.X, 0, 100) / 100);
            Canvas.SetTop(rect, h * Math.Clamp(slot.Y, 0, 100) / 100);
            SlotOverlay.Children.Add(rect);
            var label = new TextBlock { Text = slot.Name, Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(190, 30, 45)), FontSize = 11, Padding = new Thickness(3, 1, 3, 1) };
            Canvas.SetLeft(label, w * Math.Clamp(slot.X, 0, 100) / 100);
            Canvas.SetTop(label, h * Math.Clamp(slot.Y, 0, 100) / 100);
            SlotOverlay.Children.Add(label);
        }
    }

    private void ChooseFrame_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Pilih PNG frame transparan", Filter = "PNG Image (*.png)|*.png", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true)
        {
            _template.OverlayImagePath = dialog.FileName;
            LoadPreview();
        }
    }

    private void AddSlot_Click(object sender, RoutedEventArgs e)
    {
        _slots.Add(new FramePhotoSlot { Name = $"Foto {_slots.Count + 1}", X = 10, Y = 10, Width = 80, Height = 18 });
        DrawSlots();
    }

    private void RemoveSlot_Click(object sender, RoutedEventArgs e)
    {
        if (SlotsGrid.SelectedItem is FramePhotoSlot slot) _slots.Remove(slot);
        DrawSlots();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        SlotsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        SlotsGrid.CommitEdit(DataGridEditingUnit.Row, true);
        foreach (var slot in _slots)
        {
            slot.X = Math.Clamp(slot.X, 0, 100); slot.Y = Math.Clamp(slot.Y, 0, 100);
            slot.Width = Math.Clamp(slot.Width, 0, 100); slot.Height = Math.Clamp(slot.Height, 0, 100);
        }
        DrawSlots();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Refresh_Click(sender, e);
        if (string.IsNullOrWhiteSpace(_template.OverlayImagePath) || !File.Exists(_template.OverlayImagePath))
        {
            MessageBox.Show(this, "Pilih file PNG frame terlebih dahulu.", "Frame belum dipilih", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (_slots.Count == 0)
        {
            MessageBox.Show(this, "Tambahkan minimal satu slot foto.", "Slot belum ada", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _template.Slots = _slots.ToList();
        _template.Name = Path.GetFileNameWithoutExtension(_template.OverlayImagePath);
        try
        {
            File.WriteAllText(_templatePath, JsonSerializer.Serialize(_template, _json));
            _profile.Settings.FrameTemplatePath = _templatePath;
            _profile.Settings.FrameFolder = Path.GetDirectoryName(_template.OverlayImagePath) ?? "";
            MessageBox.Show(this, "Template frame berhasil disimpan untuk event ini.", "Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Gagal menyimpan template: {ex.Message}", "Kesalahan", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
