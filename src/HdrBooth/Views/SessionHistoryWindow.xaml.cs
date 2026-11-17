using System.Windows;
using HdrBooth.Models;
using HdrBooth.Services;

namespace HdrBooth.Views;

public sealed class SessionHistoryRow
{
    public Guid Id { get; set; }
    public string ShortId => Id.ToString("N")[..8].ToUpperInvariant();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int PhotoCount { get; set; }
    public string Status { get; set; } = "";
}

public partial class SessionHistoryWindow : Window
{
    private readonly EventProfile _profile;
    private readonly EventStore _store;

    public SessionHistoryWindow(EventProfile profile, EventStore store)
    {
        InitializeComponent();
        _profile = profile;
        _store = store;
        EventNameText.Text = profile.Name;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var sessions = await _store.LoadSessionSummariesAsync(_profile.Id);
        SessionsGrid.ItemsSource = sessions.Select(s => new SessionHistoryRow
        {
            Id = s.Id, StartedAt = s.StartedAt, CompletedAt = s.CompletedAt,
            PhotoCount = s.PhotoCount, Status = s.Status == "Completed" ? "Selesai" : s.Status
        }).ToList();
        var totalPhotos = sessions.Sum(s => s.PhotoCount);
        SummaryText.Text = $"{sessions.Count} sesi • {totalPhotos} foto asli tercatat";
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
