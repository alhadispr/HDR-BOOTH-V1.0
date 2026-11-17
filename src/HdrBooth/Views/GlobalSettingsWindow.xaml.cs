using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using FormsFolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using FormsDialogResult = System.Windows.Forms.DialogResult;
using HdrBooth.Services;

namespace HdrBooth.Views;

public sealed class GlobalAppSettings
{
    public string AppName { get; set; } = "HDR BOOTH";
    public string Language { get; set; } = "Bahasa Indonesia";
    public string BackupFolder { get; set; } = "";
    public bool ConfirmExit { get; set; } = true;
    public bool AutoOpenDashboard { get; set; } = true;
}

public partial class GlobalSettingsWindow : Window
{
    private readonly string _settingsPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HDR-BOOTH", "global-settings.json");
    private readonly string _dataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HDR-BOOTH");
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public GlobalSettingsWindow()
    {
        InitializeComponent();
        DataFolderBox.Text = _dataFolder;
        LoadSettings();
    }

    private void LoadSettings()
    {
        GlobalAppSettings settings = new();
        try
        {
            if (System.IO.File.Exists(_settingsPath))
                settings = JsonSerializer.Deserialize<GlobalAppSettings>(System.IO.File.ReadAllText(_settingsPath)) ?? new();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Pengaturan global belum dapat dibaca. Nilai default digunakan.\\n{ex.Message}",
                "HDR BOOTH", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        AppNameBox.Text = settings.AppName;
        LanguageBox.SelectedIndex = settings.Language == "English" ? 1 : 0;
        BackupFolderBox.Text = settings.BackupFolder;
        ConfirmExitCheck.IsChecked = settings.ConfirmExit;
        AutoOpenDashboardCheck.IsChecked = settings.AutoOpenDashboard;
    }

    private void BrowseBackup_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FormsFolderBrowserDialog { Description = "Pilih folder untuk backup HDR BOOTH" };
        if (dialog.ShowDialog() == FormsDialogResult.OK)
            BackupFolderBox.Text = dialog.SelectedPath;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            var settings = new GlobalAppSettings
            {
                AppName = string.IsNullOrWhiteSpace(AppNameBox.Text) ? "HDR BOOTH" : AppNameBox.Text.Trim(),
                Language = (LanguageBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Bahasa Indonesia",
                BackupFolder = BackupFolderBox.Text.Trim(),
                ConfirmExit = ConfirmExitCheck.IsChecked == true,
                AutoOpenDashboard = AutoOpenDashboardCheck.IsChecked == true
            };
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, _json));
            DialogResult = true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Pengaturan global gagal disimpan: {ex.Message}", "HDR BOOTH",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
