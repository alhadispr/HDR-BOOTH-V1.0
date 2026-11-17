using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using HdrBooth.Models;
using HdrBooth.Services;
using HdrBooth.Views;

namespace HdrBooth;

public partial class MainWindow : Window
{
    private readonly EventStore _store = new();
    private readonly ObservableCollection<EventProfile> _events = new();

    public MainWindow()
    {
        InitializeComponent();
        EventsGrid.ItemsSource = _events;
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        _events.Clear();
        foreach (var item in await _store.LoadAsync()) _events.Add(item);
        TotalEventsText.Text = _events.Count.ToString();
        ActiveEventText.Text = _events.FirstOrDefault(e => e.IsActive)?.Name ?? "Belum ada";
    }


    private async void DashboardNav_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private void GalleryNav_Click(object sender, RoutedEventArgs e)
    {
        if (EventsGrid.SelectedItem is not EventProfile selected)
        {
            System.Windows.MessageBox.Show("Pilih event terlebih dahulu. Galeri ditampilkan berdasarkan event yang dipilih.",
                "Galeri HDR BOOTH", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        new GalleryWindow(selected, _store) { Owner = this }.ShowDialog();
    }

    private void GlobalSettingsNav_Click(object sender, RoutedEventArgs e)
    {
        new GlobalSettingsWindow() { Owner = this }.ShowDialog();
    }

    private void EventsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (EventsGrid.SelectedItem is EventProfile selected)
        {
            var dialog = new EventSettingsWindow(selected) { Owner = this };
            if (dialog.ShowDialog() == true)
                _ = SaveSelectedEventAsync(selected);
        }
    }

    private async Task SaveSelectedEventAsync(EventProfile selected)
    {
        await _store.SaveAsync(selected);
        await RefreshAsync();
        SelectedEventText.Text = $"Pengaturan tersimpan: {selected.Name}";
    }

    private async void CreateEvent_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Window { Title = "Buat Acara Baru", Width = 420, Height = 210,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this,
            ResizeMode = ResizeMode.NoResize, Background = System.Windows.Media.Brushes.White };
        var panel = new System.Windows.Controls.StackPanel
{
    Margin = new System.Windows.Thickness(22)
};

panel.Children.Add(new System.Windows.Controls.TextBlock
{
    Text = "Nama acara",
    FontWeight = System.Windows.FontWeights.SemiBold,
    Margin = new System.Windows.Thickness(0, 0, 0, 8)
});

var nameBox = new System.Windows.Controls.TextBox
{
    Height = 34,
    Padding = new System.Windows.Thickness(8),
    FontSize = 14
};

panel.Children.Add(nameBox);

var save = new System.Windows.Controls.Button
{
    Content = "Simpan Acara",
    Background = System.Windows.Media.Brushes.IndianRed,
    Foreground = System.Windows.Media.Brushes.White,
    Margin = new System.Windows.Thickness(0, 18, 0, 0),
    HorizontalAlignment = System.Windows.HorizontalAlignment.Right
};

save.Click += (_, _) =>
{
    if (!string.IsNullOrWhiteSpace(nameBox.Text))
        dialog.DialogResult = true;
};

panel.Children.Add(save);
dialog.Content = panel;
        if (dialog.ShowDialog() == true)
        {
            var profile = new EventProfile { Name = nameBox.Text.Trim() };
            await _store.SaveAsync(profile);
            await RefreshAsync();
        }
    }

    private void EventsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EventsGrid.SelectedItem is EventProfile item) SelectedEventText.Text = $"Dipilih: {item.Name}";
    }

    private async void EventSettings_Click(object sender, RoutedEventArgs e)
    {
        if (EventsGrid.SelectedItem is not EventProfile selected)
        {
            System.Windows.MessageBox.Show("Pilih acara yang ingin diatur terlebih dahulu.", "HDR BOOTH", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dialog = new EventSettingsWindow(selected) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            await _store.SaveAsync(selected);
            await RefreshAsync();
            SelectedEventText.Text = $"Pengaturan tersimpan: {selected.Name}";
        }
    }

    private async void EditEvent_Click(object sender, RoutedEventArgs e)
    {
        if (EventsGrid.SelectedItem is not EventProfile selected) { System.Windows.MessageBox.Show("Pilih acara terlebih dahulu.", "HDR BOOTH"); return; }
        var dialog = new EditEventWindow(selected) { Owner = this };
        if (dialog.ShowDialog() == true) { await _store.SaveAsync(selected); await RefreshAsync(); }
    }

   private async void ArchiveEvent_Click(object sender, RoutedEventArgs e)
{
    if (EventsGrid.SelectedItem is not EventProfile selected)
    {
        System.Windows.MessageBox.Show(
            "Pilih acara yang ingin diarsipkan.",
            "HDR BOOTH");
        return;
    }

    var answer = System.Windows.MessageBox.Show(
        $"Arsipkan event '{selected.Name}'? File foto tidak akan dihapus.",
        "Konfirmasi Arsip",
        System.Windows.MessageBoxButton.YesNo,
        System.Windows.MessageBoxImage.Warning);

    if (answer != System.Windows.MessageBoxResult.Yes)
        return;

    await _store.ArchiveAsync(selected);
    await RefreshAsync();
    SelectedEventText.Text = "Event berhasil diarsipkan. File tetap tersimpan.";
    }

    private void SessionHistory_Click(object sender, RoutedEventArgs e)
    {
        if (EventsGrid.SelectedItem is not EventProfile selected)
        {
            System.Windows.MessageBox.Show("Pilih event untuk melihat riwayat sesinya.", "HDR BOOTH", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        new SessionHistoryWindow(selected, _store) { Owner = this }.ShowDialog();
    }

    private void Gallery_Click(object sender, RoutedEventArgs e)
    {
        if (EventsGrid.SelectedItem is not EventProfile selected) { System.Windows.MessageBox.Show("Pilih event untuk membuka galerinya.", "HDR BOOTH"); return; }
        new GalleryWindow(selected, _store) { Owner = this }.ShowDialog();
    }

    private async void ActivateEvent_Click(object sender, RoutedEventArgs e)
    {
        if (EventsGrid.SelectedItem is not EventProfile selected)
        {
            System.Windows.MessageBox.Show("Pilih acara terlebih dahulu.", "HDR BOOTH", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        await _store.SetActiveAsync(_events, selected);
        await RefreshAsync();
        System.Windows.MessageBox.Show($"{selected.Name} sekarang menjadi event aktif.", "HDR BOOTH", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void OpenBoothDisplay_Click(object sender, RoutedEventArgs e)
    {
        var active = (await _store.LoadAsync()).FirstOrDefault(item => item.IsActive);
        if (active is null)
        {
            System.Windows.MessageBox.Show("Aktifkan satu event terlebih dahulu sebelum membuka Booth Display.", "HDR BOOTH", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        new BoothDisplayWindow(active, _store).ShowDialog();
    }
}
