using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Media;
using HdrBooth.Models;
using HdrBooth.Services;
using HdrBooth.Services.Camera;

namespace HdrBooth.Views;

public partial class BoothDisplayWindow : Window
{
    private readonly EventProfile _event;
    private readonly EventStore _store;
    private readonly CameraCaptureService _camera;
    private readonly PhotoProcessingService _processor = new();
    private readonly List<string> _sessionPhotoPaths = new();
    private Guid? _sessionId;
    private bool _isCapturing;
    private string? _gifOutputPath;
    private string? _framedOutputPath;
    private string? _livePhotoPackagePath;
    private string? _liveVideoPath;
    private int? _retakeIndex;
    private LocalShareServer? _shareServer;
    private string? _movingPolaroidPackagePath;

    public BoothDisplayWindow(EventProfile activeEvent, EventStore store)
    {
        InitializeComponent();
        _event = activeEvent;
        _store = store;
        _camera = new CameraCaptureService(new DemoCameraProvider(), store);
        EventNameText.Text = activeEvent.Name;
        WelcomeTitleText.Text = activeEvent.Settings.WelcomeTitle;
        WelcomeSubtitleText.Text = activeEvent.Settings.WelcomeSubtitle;
        LayoutNameText.Text = activeEvent.Settings.LayoutName;
        PhotoCountText.Text = $"{Math.Clamp(activeEvent.Settings.PhotoCount, 1, 12)} FOTO";
        ConfigureLayoutPreview();
        Loaded += async (_, _) =>
        {
            var status = await _camera.GetStatusAsync();
            var sdk = CanonEdsdkDiagnostics.Check();
            CameraStatusText.Text = $"{status.Message}\\n{sdk.Message}\\nMode aktif saat ini: DEMO (pengujian).";
        };
    }

    private void ConfigureLayoutPreview()
    {
        var count = Math.Clamp(_event.Settings.PhotoCount, 1, 12);
        var layout = _event.Settings.LayoutName ?? "Layout Foto";
        LayoutNameText.Text = layout;
        LayoutSlotsPanel.Children.Clear();

        int columns = layout.Contains("2x2", StringComparison.OrdinalIgnoreCase) ? 2 :
                      layout.Contains("Strip", StringComparison.OrdinalIgnoreCase) ? 1 :
                      Math.Min(count, 3);
        int rows = (int)Math.Ceiling(count / (double)columns);
        LayoutSlotsPanel.Columns = columns;
        LayoutSlotsPanel.Rows = rows;

        for (int i = 1; i <= count; i++)
        {
            var slot = new Border
            {
                Margin = new Thickness(4),
                MinWidth = columns == 1 ? 82 : 74,
                MinHeight = columns == 1 ? 68 : 88,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 225, 228)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Child = new TextBlock
                {
                    Text = $"FOTO {i}",
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 155)),
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            LayoutSlotsPanel.Children.Add(slot);
        }

        LayoutDescriptionText.Text = $"{layout} • {count} jepretan per sesi";
        var templatePath = _event.Settings.FrameTemplatePath;
        try
        {
            if (!string.IsNullOrWhiteSpace(templatePath) && File.Exists(templatePath))
            {
                using var stream = File.OpenRead(templatePath);
                var template = JsonSerializer.Deserialize<FrameTemplate>(stream);
                var framePath = template?.OverlayImagePath;
                if (!string.IsNullOrWhiteSpace(framePath) && File.Exists(framePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(framePath, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    LayoutFramePreviewImage.Source = bitmap;
                    LayoutPreviewHintText.Text = "Frame event aktif";
                    return;
                }
            }
        }
        catch
        {
            // Preview layout tetap dapat digunakan meskipun file frame tidak dapat dibaca.
        }

        LayoutFramePreviewImage.Source = null;
        LayoutPreviewHintText.Text = "Frame belum dipasang — preview slot ditampilkan";
    }

    private void ShowPanel(string panel)
    {
        WelcomePanel.Visibility = panel == "welcome" ? Visibility.Visible : Visibility.Collapsed;
        LayoutPanel.Visibility = panel == "layout" ? Visibility.Visible : Visibility.Collapsed;
        CameraPanel.Visibility = panel == "camera" ? Visibility.Visible : Visibility.Collapsed;
        ReviewPanel.Visibility = panel == "review" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckSessionReadiness())
            return;

        _shareServer?.Dispose();
        _shareServer = null;
        ShareQrImage.Source = null;
        ShareStatusText.Text = "QR belum dibuat.";
        _sessionPhotoPaths.Clear();
        _sessionId = null;
        _gifOutputPath = null;
        _framedOutputPath = null;
        _movingPolaroidPackagePath = null;
        MovingPolaroidStatusText.Text = "Paket impor Artivive belum disiapkan.";
        ArtiviveArtworkUrlTextBox.Text = "";
        ArtiviveQrImage.Source = null;
        ArtiviveLinkStatusText.Text = "QR Artivive belum dibuat.";
        _shareServer?.Dispose();
        _shareServer = null;
        ShareQrImage.Source = null;
        ShareStatusText.Text = "QR perlu dibuat ulang setelah foto diubah.";
        LivePhotoMedia.Stop();
        LivePhotoMedia.Source = null;
        if (!string.IsNullOrWhiteSpace(_livePhotoPackagePath) && Directory.Exists(_livePhotoPackagePath))
            Directory.Delete(_livePhotoPackagePath, true);
        _livePhotoPackagePath = null;
        _liveVideoPath = null;
        _livePhotoPackagePath = null;
        _liveVideoPath = null;
        _retakeIndex = null;
        PhotoThumbnailPanel.Children.Clear();
        CameraPreviewImage.Source = null;
        PreviewHintText.Visibility = Visibility.Visible;
        CountdownText.Text = "";
        CameraCountdownOverlay.Visibility = Visibility.Collapsed;
        CaptureResultOverlay.Visibility = Visibility.Collapsed;
        CaptureFlashOverlay.BeginAnimation(OpacityProperty, null);
        CaptureFlashOverlay.Opacity = 0;
        UpdateCaptureProgress();
        CaptureButton.IsEnabled = true;
        CaptureButton.Content = "AMBIL FOTO";
        RetakeButton.IsEnabled = false;
        ShowPanel("layout");
    }

    private bool CheckSessionReadiness()
    {
        var warnings = new List<string>();
        var settings = _event.Settings;

        if (string.IsNullOrWhiteSpace(_event.Name))
            warnings.Add("Nama event belum diisi.");
        if (settings.PhotoCount < 1 || settings.PhotoCount > 12)
            warnings.Add("Jumlah foto di luar batas 1–12; aplikasi akan memakai batas aman.");
        if (string.IsNullOrWhiteSpace(settings.FrameTemplatePath) || !File.Exists(settings.FrameTemplatePath))
            warnings.Add("Frame belum dipasang atau file frame tidak ditemukan. Foto tetap bisa dibuat tanpa frame.");

        // The current capture provider is deliberately a demo provider until Canon EDSDK is integrated.
        warnings.Add("Kamera masih dalam MODE DEMO. Pemotretan belum menggunakan Canon EOS 3000D.");

        try
        {
            var eventFolder = _store.GetEventFolder(_event);
            Directory.CreateDirectory(eventFolder);
            var probe = Path.Combine(eventFolder, $".hdrbooth-write-test-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "HDR BOOTH");
            File.Delete(probe);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Folder penyimpanan event tidak dapat ditulis. Periksa izin folder atau lokasi penyimpanan.\\n\\nDetail: {ex.Message}",
                "HDR BOOTH — Pemeriksaan Sesi", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        string report = warnings.Count == 0
            ? "Pemeriksaan selesai. Sesi siap dimulai."
            : "Pemeriksaan sebelum sesi:\\n\\n• " + string.Join("\\n• ", warnings);

        var result = MessageBox.Show(
            report + "\\n\\nLanjutkan ke sesi foto?",
            "HDR BOOTH — Pemeriksaan Sesi",
            MessageBoxButton.YesNo,
            warnings.Any(w => w.Contains("MODE DEMO")) ? MessageBoxImage.Warning : MessageBoxImage.Information);

        return result == MessageBoxResult.Yes;
    }

    private void Continue_Click(object sender, RoutedEventArgs e) => ShowPanel("camera");
    private void BackToLayout_Click(object sender, RoutedEventArgs e) => ShowPanel("layout");
    private void ReturnToCamera_Click(object sender, RoutedEventArgs e)
    {
        CaptureButton.IsEnabled = _retakeIndex.HasValue || _sessionPhotoPaths.Count < TargetPhotoCount;
        CaptureButton.Content = _retakeIndex.HasValue ? $"AMBIL ULANG FOTO {_retakeIndex.Value + 1}" :
            (CaptureButton.IsEnabled ? "AMBIL FOTO" : "JUMLAH FOTO TERCAPAI");
        RetakeButton.IsEnabled = _sessionPhotoPaths.Count > 0;
        ShowPanel("camera");
    }

    private int TargetPhotoCount => Math.Clamp(_event.Settings.PhotoCount, 1, 12);

    private async void Capture_Click(object sender, RoutedEventArgs e)
    {
        if (_isCapturing || (_sessionPhotoPaths.Count >= TargetPhotoCount && !_retakeIndex.HasValue)) return;
        _isCapturing = true;
        CaptureButton.IsEnabled = false;
        RetakeButton.IsEnabled = false;
        try
        {
            int seconds = Math.Clamp(_event.Settings.CountdownSeconds, 0, 30);
            for (int remaining = seconds; remaining > 0; remaining--)
            {
                CountdownText.Text = remaining.ToString();
                CameraCountdownOverlayText.Text = remaining.ToString();
                CameraCountdownOverlay.Visibility = Visibility.Visible;
                AnimateCountdownPulse();
                await Task.Delay(1000);
            }
            CountdownText.Text = "CHEESE!";
            CameraCountdownOverlayText.Text = "CHEESE!";
            CameraCountdownOverlay.Visibility = Visibility.Visible;
            await Task.Delay(250);
            CameraCountdownOverlay.Visibility = Visibility.Collapsed;
            _sessionId ??= await _store.CreateSessionAsync(_event.Id);
            var path = await _camera.CaptureOriginalAsync(_event);
            if (_retakeIndex.HasValue && _retakeIndex.Value >= 0 && _retakeIndex.Value < _sessionPhotoPaths.Count)
            {
                int replaceAt = _retakeIndex.Value;
                var previousPath = _sessionPhotoPaths[replaceAt];
                await ClearGeneratedOutputsAsync();
                await _store.DeletePhotoByOriginalPathAsync(_event.Id, _sessionId.Value, previousPath);
                if (File.Exists(previousPath)) File.Delete(previousPath);
                _sessionPhotoPaths[replaceAt] = path;
                _retakeIndex = null;
                await _store.RegisterPhotoAsync(_event.Id, _sessionId.Value, path, null);
                CaptureProgressText.Text = $"Foto nomor {replaceAt + 1} berhasil diganti.";
            }
            else
            {
                _sessionPhotoPaths.Add(path);
                await _store.RegisterPhotoAsync(_event.Id, _sessionId.Value, path, null);
            }
            await ShowCapturedPhotoAsync(path);
            CameraStatusText.Text = "MODE DEMO — belum terhubung ke Canon EOS 3000D";
            UpdateCaptureProgress();
            if (_sessionPhotoPaths.Count >= TargetPhotoCount)
            {
                CaptureProgressText.Text = "Semua foto selesai. Lanjutkan ke review.";
                await PrepareReviewAsync();
                ShowPanel("review");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Capture gagal: {ex.Message}", "HDR BOOTH", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isCapturing = false;
            CountdownText.Text = "";
            CaptureButton.IsEnabled = _retakeIndex.HasValue || _sessionPhotoPaths.Count < TargetPhotoCount;
            CaptureButton.Content = _retakeIndex.HasValue ? $"AMBIL ULANG FOTO {_retakeIndex.Value + 1}" :
                (CaptureButton.IsEnabled ? "AMBIL FOTO" : "JUMLAH FOTO TERCAPAI");
            RetakeButton.IsEnabled = _sessionPhotoPaths.Count > 0 && !_retakeIndex.HasValue;
        }
    }

    private async void Retake_Click(object sender, RoutedEventArgs e)
    {
        if (_isCapturing || _sessionPhotoPaths.Count == 0 || !_sessionId.HasValue) return;
        var previous = _sessionPhotoPaths[^1];
        try
        {
            await ClearGeneratedOutputsAsync();
            await _store.DeletePhotoByOriginalPathAsync(_event.Id, _sessionId.Value, previous);
            if (File.Exists(previous)) File.Delete(previous);
            _sessionPhotoPaths.RemoveAt(_sessionPhotoPaths.Count - 1);
            CaptureProgressText.Text = "Foto terakhir dihapus. Silakan ambil ulang.";
            CaptureButton.IsEnabled = true;
            CaptureButton.Content = "AMBIL ULANG FOTO";
            RetakeButton.IsEnabled = _sessionPhotoPaths.Count > 0;
            CameraPreviewImage.Source = null;
            PreviewHintText.Visibility = Visibility.Visible;
            CaptureResultOverlay.Visibility = Visibility.Collapsed;
            CameraCountdownOverlay.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Tidak dapat mengulang foto: {ex.Message}", "HDR BOOTH", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task ShowCapturedPhotoAsync(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(path);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        CameraPreviewImage.Source = bitmap;
        PreviewHintText.Visibility = Visibility.Collapsed;
        CaptureResultText.Text = "FOTO BERHASIL TERSIMPAN";
        CaptureResultOverlay.Visibility = Visibility.Visible;
        PlayCaptureFlash();
        await Task.Delay(850);
        CaptureResultOverlay.Visibility = Visibility.Collapsed;
    }

    private void AnimateCountdownPulse()
    {
        var scale = new System.Windows.Media.ScaleTransform(0.82, 0.82);
        CameraCountdownOverlayText.RenderTransformOrigin = new Point(0.5, 0.5);
        CameraCountdownOverlayText.RenderTransform = scale;
        var animation = new DoubleAnimation(0.82, 1.0, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new System.Windows.Media.Animation.BackEase
            {
                Amplitude = 0.25,
                EasingMode = EasingMode.EaseOut
            }
        };
        scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, animation);
        scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, animation);
    }

    private void PlayCaptureFlash()
    {
        CaptureFlashOverlay.BeginAnimation(OpacityProperty, null);
        CaptureFlashOverlay.Opacity = 0;
        var flash = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(420)
        };
        flash.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        flash.KeyFrames.Add(new LinearDoubleKeyFrame(0.82, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(45))));
        flash.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(420))));
        CaptureFlashOverlay.BeginAnimation(OpacityProperty, flash);
    }

    private void UpdateCaptureProgress()
    {
        CaptureProgressText.Text = $"{_sessionPhotoPaths.Count} dari {TargetPhotoCount} foto diambil";
        ReviewCountText.Text = $"{_sessionPhotoPaths.Count} FOTO";
        if (ArtivivePhotoSelector is not null)
        {
            int previousSelection = ArtivivePhotoSelector.SelectedIndex;
            ArtivivePhotoSelector.Items.Clear();
            for (int i = 0; i < _sessionPhotoPaths.Count; i++)
                ArtivivePhotoSelector.Items.Add($"Foto {i + 1}");
            if (ArtivivePhotoSelector.Items.Count > 0)
                ArtivivePhotoSelector.SelectedIndex = Math.Clamp(previousSelection, 0, ArtivivePhotoSelector.Items.Count - 1);
        }
        CompleteButton.IsEnabled = _sessionPhotoPaths.Count >= TargetPhotoCount;
        RefreshPhotoThumbnails();
    }

    private void RefreshPhotoThumbnails()
    {
        if (PhotoThumbnailPanel is null) return;
        PhotoThumbnailPanel.Children.Clear();
        for (int i = 0; i < _sessionPhotoPaths.Count; i++)
        {
            int index = i;
            string path = _sessionPhotoPaths[i];
            if (!File.Exists(path)) continue;
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(Path.GetFullPath(path));
            image.DecodePixelWidth = 120;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();

            var card = new StackPanel { Width = 132, Margin = new Thickness(5) };
            card.Children.Add(new Image { Source = image, Width = 120, Height = 90, Stretch = System.Windows.Media.Stretch.UniformToFill });
            card.Children.Add(new TextBlock { Text = $"FOTO {index + 1}", Foreground = System.Windows.Media.Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 4) });
            var replaceButton = new Button { Content = "ULANGI FOTO", Padding = new Thickness(7, 4, 7, 4), Tag = index };
            replaceButton.Click += RetakeSpecificPhoto_Click;
            card.Children.Add(replaceButton);
            PhotoThumbnailPanel.Children.Add(card);
        }
    }

    private void RetakeSpecificPhoto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not int index || index < 0 || index >= _sessionPhotoPaths.Count)
            return;
        _retakeIndex = index;
        CaptureButton.IsEnabled = true;
        CaptureButton.Content = $"AMBIL ULANG FOTO {index + 1}";
        RetakeButton.IsEnabled = false;
        CameraPreviewImage.Source = null;
        PreviewHintText.Visibility = Visibility.Visible;
        CountdownText.Text = "";
        ShowPanel("camera");
    }

    private async Task PrepareReviewAsync()
    {
        UpdateCaptureProgress();
        ReviewStatusText.Text = "Menyiapkan preview hasil sesi...";
        FinalPhotoImage.Source = null;
        GifPreviewBrowser.NavigateToString("<html><body style='margin:0;background:#202124;color:#ddd;font-family:Arial;display:flex;align-items:center;justify-content:center;height:100vh'>GIF belum dibuat</body></html>");
        LivePhotoMedia.Stop();
        LivePhotoMedia.Source = null;

        string gifMessage = "";
        if (_event.Settings.EnableGif && _sessionPhotoPaths.Count >= 2 && _sessionId.HasValue)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_gifOutputPath) || !File.Exists(_gifOutputPath))
                {
                    var gifFolder = Path.Combine(_store.GetEventFolder(_event), "gif");
                    Directory.CreateDirectory(gifFolder);
                    var gifPath = Path.Combine(gifFolder, $"HDR_GIF_{DateTime.Now:yyyyMMdd_HHmmss_fff}.gif");
                    _processor.CreateAnimatedGif(_sessionPhotoPaths, gifPath, 700);
                    await _store.RegisterGifOutputAsync(_event.Id, _sessionId.Value, gifPath);
                    _gifOutputPath = gifPath;
                }
                var gifUri = new Uri(Path.GetFullPath(_gifOutputPath)).AbsoluteUri;
                GifPreviewBrowser.NavigateToString($"<html><body style='margin:0;background:#202124;display:flex;align-items:center;justify-content:center;height:100vh'><img src='{gifUri}' style='max-width:100%;max-height:100%;object-fit:contain'/></body></html>");
                gifMessage = " GIF siap dipreview.";
            }
            catch (Exception ex)
            {
                gifMessage = $" GIF gagal dibuat: {ex.Message}";
            }
        }
        else if (_event.Settings.EnableGif)
        {
            gifMessage = " GIF membutuhkan minimal dua foto.";
        }

        var templatePath = _event.Settings.FrameTemplatePath;
        if (!string.IsNullOrWhiteSpace(templatePath) && File.Exists(templatePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(templatePath);
                var template = JsonSerializer.Deserialize<FrameTemplate>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (template is not null && File.Exists(template.OverlayImagePath))
                {
                    if (string.IsNullOrWhiteSpace(_framedOutputPath) || !File.Exists(_framedOutputPath))
                    {
                        var outputFolder = Path.Combine(_store.GetEventFolder(_event), "framed");
                        Directory.CreateDirectory(outputFolder);
                        var outputPath = Path.Combine(outputFolder, $"HDR_FINAL_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
                        _processor.ComposePhotoLayout(_sessionPhotoPaths, template, outputPath);
                        _framedOutputPath = outputPath;
                        await _store.RegisterLayoutOutputAsync(_event.Id, _sessionId!.Value, outputPath);
                    }
                }
            }
            catch (Exception ex)
            {
                ReviewStatusText.Text = $"Pembuatan frame gagal: {ex.Message}";
            }
        }

        string? finalPath = !string.IsNullOrWhiteSpace(_framedOutputPath) && File.Exists(_framedOutputPath)
            ? _framedOutputPath
            : _sessionPhotoPaths.LastOrDefault(p => File.Exists(p));
        if (!string.IsNullOrWhiteSpace(finalPath))
        {
            var finalImage = new BitmapImage();
            finalImage.BeginInit();
            finalImage.UriSource = new Uri(Path.GetFullPath(finalPath));
            finalImage.CacheOption = BitmapCacheOption.OnLoad;
            finalImage.EndInit();
            finalImage.Freeze();
            FinalPhotoImage.Source = finalImage;
        }

        if (!string.IsNullOrWhiteSpace(_liveVideoPath) && File.Exists(_liveVideoPath))
        {
            LivePhotoMedia.Source = new Uri(Path.GetFullPath(_liveVideoPath));
            LivePhotoStatusText.Text = "Video Live Photo terpasang. Tekan Play untuk melihat.";
        }
        else
        {
            LivePhotoStatusText.Text = "Belum ada video. Pilih video MP4 untuk dipasangkan dengan foto final.";
        }

        ReviewStatusText.Text = $"Preview final, GIF, dan Live Photo tersedia. {gifMessage}";
    }

    private async void SelectLiveVideo_Click(object sender, RoutedEventArgs e)
    {
        if (!_sessionId.HasValue || _sessionPhotoPaths.Count == 0)
        {
            MessageBox.Show("Ambil foto terlebih dahulu sebelum menambahkan video Live Photo.", "HDR BOOTH",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Pilih video untuk Live Photo",
            Filter = "Video MP4 (*.mp4)|*.mp4",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var folder = Path.Combine(_store.GetEventFolder(_event), "live-photo",
                _sessionId.Value.ToString("N"));
            Directory.CreateDirectory(folder);
            var videoPath = Path.Combine(folder, "motion.mp4");
            File.Copy(dialog.FileName, videoPath, true);

            string stillPath = !string.IsNullOrWhiteSpace(_framedOutputPath) && File.Exists(_framedOutputPath)
                ? _framedOutputPath
                : _sessionPhotoPaths[^1];
            var stillCopy = Path.Combine(folder, "still.png");
            File.Copy(stillPath, stillCopy, true);

            var manifest = new
            {
                format = "HDR-BOOTH-MOTION-PHOTO",
                version = 1,
                createdAt = DateTime.Now,
                stillImage = "still.png",
                motionVideo = "motion.mp4",
                note = "Paket motion photo HDR BOOTH. Bukan format Apple Live Photo native."
            };
            await File.WriteAllTextAsync(Path.Combine(folder, "manifest.json"),
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

            _liveVideoPath = videoPath;
            _livePhotoPackagePath = folder;
            await _store.RegisterLivePhotoOutputAsync(_event.Id, _sessionId.Value, folder);
            LivePhotoMedia.Source = new Uri(Path.GetFullPath(videoPath));
            LivePhotoStatusText.Text = $"Live Photo siap dipreview: {folder}";
            LivePhotoMedia.Play();
            ReviewStatusText.Text = "Paket Live Photo berhasil dibuat dari foto final dan video MP4.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Gagal membuat paket Live Photo: {ex.Message}", "HDR BOOTH",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LivePhotoPlay_Click(object sender, RoutedEventArgs e)
    {
        if (LivePhotoMedia.Source is null) return;
        LivePhotoMedia.Position = TimeSpan.Zero;
        LivePhotoMedia.Play();
    }

    private void LivePhotoPause_Click(object sender, RoutedEventArgs e) => LivePhotoMedia.Pause();

    private void LivePhotoStop_Click(object sender, RoutedEventArgs e) => LivePhotoMedia.Stop();

    private void LivePhotoMedia_MediaEnded(object sender, RoutedEventArgs e)
    {
        LivePhotoMedia.Position = TimeSpan.Zero;
    }

    private void PrintPhoto_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? imagePath = !string.IsNullOrWhiteSpace(_framedOutputPath) && File.Exists(_framedOutputPath)
                ? _framedOutputPath
                : _sessionPhotoPaths.LastOrDefault(File.Exists);
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                PrintStatusText.Text = "Belum ada foto yang dapat dicetak.";
                return;
            }

            var settings = _event.Settings;
            var printerService = new WindowsPrintService();
            bool printed = printerService.PrintPhoto(this, imagePath, settings.PrinterName,
                settings.PrintSize, settings.PrintCopies);
            PrintStatusText.Text = printed
                ? $"Perintah cetak dikirim. Printer: {(string.IsNullOrWhiteSpace(settings.PrinterName) ? "pilihan dialog" : settings.PrinterName)}."
                : "Pencetakan dibatalkan.";
        }
        catch (Exception ex)
        {
            PrintStatusText.Text = $"Cetak gagal: {ex.Message}";
            MessageBox.Show(PrintStatusText.Text, "HDR BOOTH", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CreateMovingPolaroid_Click(object sender, RoutedEventArgs e)
    {
        if (_sessionPhotoPaths.Count == 0)
        {
            MovingPolaroidStatusText.Text = "Ambil foto terlebih dahulu sebelum mengekspor paket Artivive.";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Pilih video overlay untuk Artivive",
            Filter = "Video MP4 (*.mp4)|*.mp4",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            int selectedPhotoIndex = ArtivivePhotoSelector.SelectedIndex >= 0
                ? ArtivivePhotoSelector.SelectedIndex
                : _sessionPhotoPaths.Count - 1;
            selectedPhotoIndex = Math.Clamp(selectedPhotoIndex, 0, _sessionPhotoPaths.Count - 1);
            string targetPath = _sessionPhotoPaths[selectedPhotoIndex];
            var service = new MovingPolaroidService();
            _movingPolaroidPackagePath = service.CreatePackage(
                _event, _sessionId, selectedPhotoIndex + 1, targetPath, dialog.FileName,
                _store.GetEventFolder(_event));

            MovingPolaroidStatusText.Text =
                $"Paket Artivive untuk Foto {selectedPhotoIndex + 1} berhasil dibuat. Upload trigger image dan video ke Artivive Bridge, atur layer, lalu Publish.";
            MessageBox.Show(
                $"Paket impor Artivive berhasil disiapkan:\\n{_movingPolaroidPackagePath}\\n\\nCatatan: unggah dan Publish masih dilakukan melalui Artivive Bridge.",
                "HDR BOOTH — Artivive", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MovingPolaroidStatusText.Text = $"Gagal menyiapkan paket: {ex.Message}";
            MessageBox.Show(MovingPolaroidStatusText.Text, "HDR BOOTH — Artivive",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SaveArtiviveLink_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_movingPolaroidPackagePath) || !Directory.Exists(_movingPolaroidPackagePath))
        {
            ArtiviveLinkStatusText.Text = "Ekspor paket Artivive terlebih dahulu.";
            return;
        }

        try
        {
            var service = new MovingPolaroidService();
            var qrPath = service.SaveArtworkLinkAndQr(_movingPolaroidPackagePath, ArtiviveArtworkUrlTextBox.Text.Trim());
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(Path.GetFullPath(qrPath));
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            ArtiviveQrImage.Source = bitmap;
            ArtiviveLinkStatusText.Text = $"QR artwork tersimpan: {qrPath}";
        }
        catch (Exception ex)
        {
            ArtiviveLinkStatusText.Text = $"Tautan belum tersimpan: {ex.Message}";
            MessageBox.Show(ArtiviveLinkStatusText.Text, "HDR BOOTH — Artivive",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void CreateShareQr_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_event.Settings.EnableQrSharing)
            {
                ShareStatusText.Text = "QR Sharing dinonaktifkan pada pengaturan event.";
                return;
            }

            var files = new List<string>();
            if (!string.IsNullOrWhiteSpace(_framedOutputPath) && File.Exists(_framedOutputPath))
                files.Add(_framedOutputPath);
            else if (_sessionPhotoPaths.Count > 0)
                files.Add(_sessionPhotoPaths[^1]);

            if (!string.IsNullOrWhiteSpace(_gifOutputPath) && File.Exists(_gifOutputPath))
                files.Add(_gifOutputPath);
            if (!string.IsNullOrWhiteSpace(_livePhotoPackagePath) && Directory.Exists(_livePhotoPackagePath))
            {
                var still = Path.Combine(_livePhotoPackagePath, "still.png");
                var video = Path.Combine(_livePhotoPackagePath, "motion.mp4");
                if (File.Exists(still)) files.Add(still);
                if (File.Exists(video)) files.Add(video);
            }
            files.AddRange(_sessionPhotoPaths.Where(File.Exists));
            if (!string.IsNullOrWhiteSpace(_movingPolaroidPackagePath) && Directory.Exists(_movingPolaroidPackagePath))
                files.AddRange(Directory.GetFiles(_movingPolaroidPackagePath)
                    .Where(p => Path.GetFileName(p) is "target-image.png" or "target-image.jpg" or "overlay-video.mp4" or "manifest.json" or "ARTIVIVE-IMPORT-GUIDE.txt" or "artwork-qr.png" or "ARTWORK-LINK.txt"));

            _shareServer?.Dispose();
            _shareServer = new LocalShareServer(files);
            var qrBytes = _shareServer.CreateQrPng();
            using var memory = new MemoryStream(qrBytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = memory;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            ShareQrImage.Source = bitmap;

            var exportFolder = Path.Combine(_store.GetEventFolder(_event), "exports");
            Directory.CreateDirectory(exportFolder);
            var qrPath = Path.Combine(exportFolder, $"HDR_QR_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            await File.WriteAllBytesAsync(qrPath, qrBytes);
            ShareStatusText.Text = $"Scan QR dengan HP yang terhubung ke Wi-Fi/LAN yang sama.\n{_shareServer.Url}\nQR tersimpan: {qrPath}";
        }
        catch (Exception ex)
        {
            ShareStatusText.Text = $"QR gagal dibuat: {ex.Message}";
            MessageBox.Show(ShareStatusText.Text, "HDR BOOTH", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task ClearGeneratedOutputsAsync()
    {
        if (!_sessionId.HasValue) return;
        foreach (var path in new[] { _gifOutputPath, _framedOutputPath })
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            await _store.DeleteGeneratedOutputAsync(_event.Id, _sessionId.Value, path);
            if (File.Exists(path)) File.Delete(path);
        }
        _gifOutputPath = null;
        _framedOutputPath = null;
        _shareServer?.Dispose();
        _shareServer = null;
        ShareQrImage.Source = null;
        ShareStatusText.Text = "QR perlu dibuat ulang setelah foto diubah.";
        LivePhotoMedia.Stop();
        LivePhotoMedia.Source = null;
        if (!string.IsNullOrWhiteSpace(_livePhotoPackagePath) && Directory.Exists(_livePhotoPackagePath))
            Directory.Delete(_livePhotoPackagePath, true);
        _livePhotoPackagePath = null;
        _liveVideoPath = null;
    }

    private async void Review_Click(object sender, RoutedEventArgs e)
    {
        await PrepareReviewAsync();
        ShowPanel("review");
    }

    private async Task FinishSessionAsync()
    {
        if (_sessionId.HasValue)
        {
            await _store.CompleteSessionAsync(_sessionId.Value);
            _sessionId = null;
        }
    }

    private async void Complete_Click(object sender, RoutedEventArgs e)
    {
        await FinishSessionAsync();
        ShowPanel("welcome");
    }

    private async void Home_Click(object sender, RoutedEventArgs e)
    {
        await FinishSessionAsync();
        ShowPanel("welcome");
    }

    private async void Close_Click(object sender, RoutedEventArgs e)
    {
        await FinishSessionAsync();
        _shareServer?.Dispose();
        _shareServer = null;
        Close();
    }
}
