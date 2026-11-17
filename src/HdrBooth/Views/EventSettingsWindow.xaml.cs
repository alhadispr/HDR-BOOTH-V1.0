using HdrBooth.Services;
using HdrBooth.Services.Camera;
using System.Windows;
using System.Windows.Controls;
using HdrBooth.Models;

namespace HdrBooth.Views;

public partial class EventSettingsWindow : Window
{
    private readonly EventProfile _profile;
    public EventSettingsWindow(EventProfile profile)
    {
        InitializeComponent();
        _profile = profile;
        LoadPrinterList();
        LoadSettings();
    }

    private void LoadPrinterList()
    {
        var saved = _profile.Settings.PrinterName;
        PrinterNameBox.Items.Clear();
        foreach (var printer in new WindowsPrintService().GetInstalledPrinters())
            PrinterNameBox.Items.Add(printer);
        PrinterNameBox.Text = saved;
    }

    private void RefreshPrinters_Click(object sender, RoutedEventArgs e) => LoadPrinterList();

    private void CheckCanonSdk_Click(object sender, RoutedEventArgs e)
    {
        var result = CanonEdsdkDiagnostics.Check();
        CanonSdkStatusText.Text = result.Message;
    }


    private void OpenFrameEditor_Click(object sender, RoutedEventArgs e)
    {
        var editor = new FrameEditorWindow(_profile) { Owner = this };
        editor.ShowDialog();
    }

    private void LoadSettings()
    {
        var s = _profile.Settings;
        EventNameText.Text = _profile.Name;
        WelcomeTitleBox.Text = s.WelcomeTitle; WelcomeSubtitleBox.Text = s.WelcomeSubtitle;
        ClosingTitleBox.Text = s.ClosingTitle; ClosingSubtitleBox.Text = s.ClosingSubtitle;
        SelectCombo(LayoutBox, s.LayoutName); PhotoCountBox.Text = s.PhotoCount.ToString();
        CountdownBox.Text = s.CountdownSeconds.ToString(); RetakeBox.Text = s.RetakeLimit.ToString(); FrameFolderBox.Text = s.FrameFolder;
        CameraIdBox.Text = s.CameraId; SelectCombo(CameraModeBox, s.CameraMode);
        PrinterNameBox.Text = s.PrinterName; SelectCombo(PrintSizeBox, s.PrintSize); PrintCopiesBox.Text = s.PrintCopies.ToString();
        QrCheck.IsChecked = s.EnableQrSharing; CloudCheck.IsChecked = s.EnableCloudUpload;
        PaymentCheck.IsChecked = s.RequirePayment; VoucherCheck.IsChecked = s.EnableVoucher; VoucherCodeBox.Text = s.VoucherCode;
        FramedCheck.IsChecked = s.SaveFramedPhotos; OriginalCheck.IsChecked = s.SaveOriginalPhotos;
        GifCheck.IsChecked = s.EnableGif; LivePhotoCheck.IsChecked = s.EnableLivePhoto; MovingPolaroidCheck.IsChecked = s.EnableMovingPolaroid;
    }

    private static void SelectCombo(System.Windows.Controls.ComboBox combo, string value)
{
    foreach (System.Windows.Controls.ComboBoxItem item in combo.Items)
    {
        if (string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
        {
            combo.SelectedItem = item;
            return;
        }
    }

    combo.SelectedIndex = 0;
}

private static string ComboValue(System.Windows.Controls.ComboBox combo) =>
    (combo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "";
    private static int Number(string value, int fallback, int min, int max) => int.TryParse(value, out var n) ? Math.Clamp(n, min, max) : fallback;

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var s = _profile.Settings;
        s.WelcomeTitle = WelcomeTitleBox.Text.Trim(); s.WelcomeSubtitle = WelcomeSubtitleBox.Text.Trim();
        s.ClosingTitle = ClosingTitleBox.Text.Trim(); s.ClosingSubtitle = ClosingSubtitleBox.Text.Trim();
        s.LayoutName = ComboValue(LayoutBox); s.PhotoCount = Number(PhotoCountBox.Text, 4, 1, 12);
        s.CountdownSeconds = Number(CountdownBox.Text, 3, 0, 30); s.RetakeLimit = Number(RetakeBox.Text, 1, 0, 20); s.FrameFolder = FrameFolderBox.Text.Trim();
        s.CameraId = CameraIdBox.Text.Trim(); s.CameraMode = ComboValue(CameraModeBox);
        s.PrinterName = PrinterNameBox.Text.Trim(); s.PrintSize = ComboValue(PrintSizeBox); s.PrintCopies = Number(PrintCopiesBox.Text, 1, 1, 20);
        s.EnableQrSharing = QrCheck.IsChecked == true; s.EnableCloudUpload = CloudCheck.IsChecked == true;
        s.RequirePayment = PaymentCheck.IsChecked == true; s.EnableVoucher = VoucherCheck.IsChecked == true; s.VoucherCode = VoucherCodeBox.Text.Trim();
        s.SaveFramedPhotos = FramedCheck.IsChecked == true; s.SaveOriginalPhotos = OriginalCheck.IsChecked == true;
        s.EnableGif = GifCheck.IsChecked == true; s.EnableLivePhoto = LivePhotoCheck.IsChecked == true; s.EnableMovingPolaroid = MovingPolaroidCheck.IsChecked == true;
        DialogResult = true;
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
