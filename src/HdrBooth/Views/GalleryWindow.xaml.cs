using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using HdrBooth.Models;
using HdrBooth.Services;
using System.Text.Json;

namespace HdrBooth.Views;

public sealed class GalleryFileRow
{
    public string Kind { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ArStatus { get; set; } = "";
    public string ArtworkUrl { get; set; } = "";
    public string FullPath { get; set; } = "";
    public DateTime Modified { get; set; }
    public long Size { get; set; }
    public string SizeText => Size < 1024 * 1024 ? $"{Size / 1024.0:0.0} KB" : $"{Size / 1024.0 / 1024:0.0} MB";
}

public partial class GalleryWindow : Window
{
    private readonly string _folder;
    private readonly ObservableCollection<GalleryFileRow> _rows = new();
    private ICollectionView? _view;

    public GalleryWindow(EventProfile profile, EventStore store)
    {
        InitializeComponent();
        _folder = store.GetEventFolder(profile);
        store.EnsureEventFolders(profile);
        TitleText.Text = $"Galeri — {profile.Name}";
        FilesGrid.ItemsSource = _rows;
        _view = CollectionViewSource.GetDefaultView(_rows);
        _view.Filter = FilterRow;
        LoadFiles();
    }

    private bool FilterRow(object item)
    {
        if (item is not GalleryFileRow row) return false;
        var query = SearchBox?.Text?.Trim();
        return string.IsNullOrEmpty(query) ||
               row.FileName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               row.Kind.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void LoadFiles()
    {
        _rows.Clear();
        foreach (var dir in Directory.EnumerateDirectories(_folder))
        {
            var kind = Path.GetFileName(dir);
            foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var info = new FileInfo(file);
                    var row = new GalleryFileRow { Kind = kind, FileName = info.Name, FullPath = info.FullName,
                        Modified = info.LastWriteTime, Size = info.Length };
                    if (kind.Equals("artivive-export", StringComparison.OrdinalIgnoreCase) &&
                        info.Name.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            using var document = JsonDocument.Parse(File.ReadAllText(info.FullName));
                            var data = document.RootElement;
                            if (data.TryGetProperty("Status", out var status) || data.TryGetProperty("status", out status))
                                row.ArStatus = status.GetString() ?? "";
                            if (data.TryGetProperty("ArtworkUrl", out var url) || data.TryGetProperty("artworkUrl", out url))
                                row.ArtworkUrl = url.GetString() ?? "";
                            row.FileName = info.Directory?.Name + " — manifest.json";
                        }
                        catch { row.ArStatus = "Manifest tidak terbaca"; }
                    }
                    _rows.Add(row);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        _view?.Refresh();
        UpdateCount();
    }

    private void UpdateCount()
    {
        var visible = _view?.Cast<object>().Count() ?? _rows.Count;
        CountText.Text = $"{visible} dari {_rows.Count} file";
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _view?.Refresh();
        UpdateCount();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadFiles();

    private void OpenSelected_Click(object sender, RoutedEventArgs e)
    {
        if (FilesGrid.SelectedItem is not GalleryFileRow row)
        {
            MessageBox.Show("Pilih file yang ingin dibuka.", "Galeri HDR BOOTH", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        OpenFile(row.FullPath);
    }

    private void FilesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FilesGrid.SelectedItem is GalleryFileRow row) OpenFile(row.FullPath);
    }

    private static void OpenFile(string path)
    {
        if (!File.Exists(path))
        {
            MessageBox.Show("File tidak ditemukan. Muat ulang galeri.", "Galeri HDR BOOTH", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex)
        {
            MessageBox.Show($"File tidak dapat dibuka: {ex.Message}", "Galeri HDR BOOTH", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_folder);
        Process.Start(new ProcessStartInfo("explorer.exe", _folder) { UseShellExecute = true });
    }
}
