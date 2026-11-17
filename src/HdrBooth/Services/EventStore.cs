using System.IO;
using System.Text.Json;
using HdrBooth.Models;
using Microsoft.Data.Sqlite;

namespace HdrBooth.Services;

/// <summary>SQLite-backed event repository. Imports legacy per-event JSON profiles once.</summary>
public sealed class EventStore
{
    private readonly string _appRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HDR-BOOTH");
    private readonly string _eventRoot;
    private readonly string _databasePath;
    private readonly string _connectionString;
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public EventStore()
    {
        _eventRoot = Path.Combine(_appRoot, "events");
        _databasePath = Path.Combine(_appRoot, "hdr-booth.db");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, ForeignKeys = true }.ToString();
        Directory.CreateDirectory(_appRoot);
        Directory.CreateDirectory(_eventRoot);
        InitializeAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeAsync()
    {
        await using var db = new SqliteConnection(_connectionString);
        await db.OpenAsync();
        var cmd = db.CreateCommand();
        cmd.CommandText = @"
PRAGMA journal_mode=WAL;
CREATE TABLE IF NOT EXISTS Events (
 Id TEXT PRIMARY KEY, Name TEXT NOT NULL, CreatedAt TEXT NOT NULL, EventDate TEXT NULL,
 IsActive INTEGER NOT NULL DEFAULT 0, IsArchived INTEGER NOT NULL DEFAULT 0,
 SettingsJson TEXT NOT NULL, OutputFolder TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS Sessions (
 Id TEXT PRIMARY KEY, EventId TEXT NOT NULL, StartedAt TEXT NOT NULL, CompletedAt TEXT NULL,
 Status TEXT NOT NULL DEFAULT 'Completed', FOREIGN KEY(EventId) REFERENCES Events(Id) ON DELETE CASCADE);
CREATE TABLE IF NOT EXISTS Photos (
 Id TEXT PRIMARY KEY, SessionId TEXT NOT NULL, EventId TEXT NOT NULL, CapturedAt TEXT NOT NULL,
 OriginalPath TEXT NULL, FramedPath TEXT NULL, GifPath TEXT NULL, LivePhotoPath TEXT NULL,
 FOREIGN KEY(SessionId) REFERENCES Sessions(Id) ON DELETE CASCADE,
 FOREIGN KEY(EventId) REFERENCES Events(Id) ON DELETE CASCADE);
CREATE INDEX IF NOT EXISTS IX_Events_Archived ON Events(IsArchived, CreatedAt);
CREATE INDEX IF NOT EXISTS IX_Sessions_Event ON Sessions(EventId, StartedAt);
CREATE INDEX IF NOT EXISTS IX_Photos_Event ON Photos(EventId, CapturedAt);";
        await cmd.ExecuteNonQueryAsync();
        await ImportLegacyJsonAsync(db);
    }

    private async Task ImportLegacyJsonAsync(SqliteConnection db)
    {
        foreach (var file in Directory.EnumerateFiles(_eventRoot, "*.json"))
        {
            try
            {
                var profile = JsonSerializer.Deserialize<EventProfile>(await File.ReadAllTextAsync(file), _json);
                if (profile is null) continue;
                var check = db.CreateCommand(); check.CommandText = "SELECT COUNT(*) FROM Events WHERE Id=$id"; check.Parameters.AddWithValue("$id", profile.Id.ToString("D"));
                if (Convert.ToInt64(await check.ExecuteScalarAsync()) > 0) continue;
                await SaveToDatabaseAsync(db, profile, false);
                EnsureEventFolders(profile);
                var migrated = file + ".migrated";
                if (!File.Exists(migrated)) File.Move(file, migrated);
            }
            catch { /* Preserve unreadable legacy files for manual recovery. */ }
        }
    }

    public async Task<List<EventProfile>> LoadAsync(bool includeArchived = false)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync();
        var cmd = db.CreateCommand();
        cmd.CommandText = $"SELECT Id,Name,CreatedAt,EventDate,IsActive,SettingsJson FROM Events {(includeArchived ? "" : "WHERE IsArchived=0")} ORDER BY CreatedAt DESC";
        var result = new List<EventProfile>(); await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var item = new EventProfile {
                Id = Guid.Parse(reader.GetString(0)), Name = reader.GetString(1),
                CreatedAt = DateTime.Parse(reader.GetString(2)), EventDate = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3)),
                IsActive = reader.GetInt64(4) == 1,
                Settings = JsonSerializer.Deserialize<EventSettings>(reader.GetString(5), _json) ?? new EventSettings()
            };
            result.Add(item);
        }
        return result;
    }

    public async Task SaveAsync(EventProfile item)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync();
        await SaveToDatabaseAsync(db, item, true);
        EnsureEventFolders(item);
    }

    private async Task SaveToDatabaseAsync(SqliteConnection db, EventProfile item, bool unarchive)
    {
        var cmd = db.CreateCommand();
        cmd.CommandText = @"INSERT INTO Events(Id,Name,CreatedAt,EventDate,IsActive,IsArchived,SettingsJson,OutputFolder)
VALUES($id,$name,$created,$date,$active,0,$settings,$folder)
ON CONFLICT(Id) DO UPDATE SET Name=excluded.Name, EventDate=excluded.EventDate, IsActive=excluded.IsActive,
SettingsJson=excluded.SettingsJson, IsArchived=CASE WHEN $unarchive=1 THEN 0 ELSE Events.IsArchived END, OutputFolder=excluded.OutputFolder";
        cmd.Parameters.AddWithValue("$id", item.Id.ToString("D")); cmd.Parameters.AddWithValue("$name", item.Name);
        cmd.Parameters.AddWithValue("$created", item.CreatedAt.ToString("O"));
        object eventDateValue = item.EventDate.HasValue ? item.EventDate.Value.ToString("O") : DBNull.Value;
        cmd.Parameters.AddWithValue("$date", eventDateValue);
        cmd.Parameters.AddWithValue("$active", item.IsActive ? 1 : 0); cmd.Parameters.AddWithValue("$settings", JsonSerializer.Serialize(item.Settings, _json));
        cmd.Parameters.AddWithValue("$folder", GetEventFolder(item)); cmd.Parameters.AddWithValue("$unarchive", unarchive ? 1 : 0);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SetActiveAsync(IEnumerable<EventProfile> all, EventProfile selected)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync();
        var off = db.CreateCommand(); off.Transaction = (SqliteTransaction)tx; off.CommandText = "UPDATE Events SET IsActive=0"; await off.ExecuteNonQueryAsync();
        var on = db.CreateCommand(); on.Transaction = (SqliteTransaction)tx; on.CommandText = "UPDATE Events SET IsActive=1 WHERE Id=$id AND IsArchived=0";
        on.Parameters.AddWithValue("$id", selected.Id.ToString("D")); await on.ExecuteNonQueryAsync(); await tx.CommitAsync();
    }

    public async Task ArchiveAsync(EventProfile item)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync();
        var cmd = db.CreateCommand(); cmd.CommandText = "UPDATE Events SET IsArchived=1, IsActive=0 WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", item.Id.ToString("D")); await cmd.ExecuteNonQueryAsync();
    }

    public async Task<Guid> CreateSessionAsync(Guid eventId)
    {
        var sessionId = Guid.NewGuid();
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync();
        var cmd = db.CreateCommand();
        cmd.CommandText = "INSERT INTO Sessions(Id,EventId,StartedAt,Status) VALUES($id,$event,$started,'InProgress')";
        cmd.Parameters.AddWithValue("$id", sessionId.ToString("D")); cmd.Parameters.AddWithValue("$event", eventId.ToString("D"));
        cmd.Parameters.AddWithValue("$started", DateTime.Now.ToString("O")); await cmd.ExecuteNonQueryAsync();
        return sessionId;
    }

    public async Task RegisterPhotoAsync(Guid eventId, Guid sessionId, string? originalPath, string? framedPath,
        string? gifPath = null, string? livePhotoPath = null)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync();
        var cmd = db.CreateCommand();
        cmd.CommandText = @"INSERT INTO Photos(Id,SessionId,EventId,CapturedAt,OriginalPath,FramedPath,GifPath,LivePhotoPath)
VALUES($id,$session,$event,$captured,$original,$framed,$gif,$live)";
        cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("D")); cmd.Parameters.AddWithValue("$session", sessionId.ToString("D"));
        cmd.Parameters.AddWithValue("$event", eventId.ToString("D")); cmd.Parameters.AddWithValue("$captured", DateTime.Now.ToString("O"));
        cmd.Parameters.AddWithValue("$original", (object?)originalPath ?? DBNull.Value); cmd.Parameters.AddWithValue("$framed", (object?)framedPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$gif", (object?)gifPath ?? DBNull.Value); cmd.Parameters.AddWithValue("$live", (object?)livePhotoPath ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeletePhotoByOriginalPathAsync(Guid eventId, Guid sessionId, string originalPath)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync();
        var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM Photos WHERE EventId=$event AND SessionId=$session AND OriginalPath=$path";
        cmd.Parameters.AddWithValue("$event", eventId.ToString("D"));
        cmd.Parameters.AddWithValue("$session", sessionId.ToString("D"));
        cmd.Parameters.AddWithValue("$path", originalPath);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RegisterLayoutOutputAsync(Guid eventId, Guid sessionId, string framedPath)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync();
        var cmd = db.CreateCommand();
        cmd.CommandText = @"INSERT INTO Photos(Id,SessionId,EventId,CapturedAt,FramedPath)
VALUES($id,$session,$event,$captured,$framed)";
        cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("D"));
        cmd.Parameters.AddWithValue("$session", sessionId.ToString("D"));
        cmd.Parameters.AddWithValue("$event", eventId.ToString("D"));
        cmd.Parameters.AddWithValue("$captured", DateTime.Now.ToString("O"));
        cmd.Parameters.AddWithValue("$framed", framedPath);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteGeneratedOutputAsync(Guid eventId, Guid sessionId, string path)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync();
        var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM Photos WHERE EventId=$event AND SessionId=$session AND (FramedPath=$path OR GifPath=$path OR LivePhotoPath=$path)";
        cmd.Parameters.AddWithValue("$event", eventId.ToString("D"));
        cmd.Parameters.AddWithValue("$session", sessionId.ToString("D"));
        cmd.Parameters.AddWithValue("$path", path);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RegisterGifOutputAsync(Guid eventId, Guid sessionId, string gifPath)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync();
        var cmd = db.CreateCommand();
        cmd.CommandText = @"INSERT INTO Photos(Id,SessionId,EventId,CapturedAt,GifPath)
VALUES($id,$session,$event,$captured,$gif)";
        cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("D"));
        cmd.Parameters.AddWithValue("$session", sessionId.ToString("D"));
        cmd.Parameters.AddWithValue("$event", eventId.ToString("D"));
        cmd.Parameters.AddWithValue("$captured", DateTime.Now.ToString("O"));
        cmd.Parameters.AddWithValue("$gif", gifPath);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RegisterLivePhotoOutputAsync(Guid eventId, Guid sessionId, string livePhotoPath)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync();
        var cmd = db.CreateCommand();
        cmd.CommandText = @"INSERT INTO Photos(Id,SessionId,EventId,CapturedAt,LivePhotoPath)
VALUES($id,$session,$event,$captured,$live)";
        cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("D"));
        cmd.Parameters.AddWithValue("$session", sessionId.ToString("D"));
        cmd.Parameters.AddWithValue("$event", eventId.ToString("D"));
        cmd.Parameters.AddWithValue("$captured", DateTime.Now.ToString("O"));
        cmd.Parameters.AddWithValue("$live", livePhotoPath);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task CompleteSessionAsync(Guid sessionId)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync();
        var cmd = db.CreateCommand(); cmd.CommandText = "UPDATE Sessions SET CompletedAt=$completed, Status='Completed' WHERE Id=$id";
        cmd.Parameters.AddWithValue("$completed", DateTime.Now.ToString("O")); cmd.Parameters.AddWithValue("$id", sessionId.ToString("D"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<GalleryItem>> LoadGalleryAsync(Guid eventId)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync();
        var cmd = db.CreateCommand();
        cmd.CommandText = @"SELECT p.Id,p.CapturedAt,p.OriginalPath,p.FramedPath,p.GifPath,p.LivePhotoPath,s.Id
FROM Photos p JOIN Sessions s ON s.Id=p.SessionId WHERE p.EventId=$id ORDER BY p.CapturedAt DESC";
        cmd.Parameters.AddWithValue("$id", eventId.ToString("D"));
        var items = new List<GalleryItem>(); await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) items.Add(new GalleryItem {
            Id = Guid.Parse(r.GetString(0)), CapturedAt = DateTime.Parse(r.GetString(1)),
            OriginalPath = r.IsDBNull(2) ? "" : r.GetString(2), FramedPath = r.IsDBNull(3) ? "" : r.GetString(3),
            GifPath = r.IsDBNull(4) ? "" : r.GetString(4), LivePhotoPath = r.IsDBNull(5) ? "" : r.GetString(5), SessionId = Guid.Parse(r.GetString(6))
        });
        return items;
    }


    public async Task<List<SessionSummary>> LoadSessionSummariesAsync(Guid eventId)
    {
        await using var db = new SqliteConnection(_connectionString);
        await db.OpenAsync();
        var cmd = db.CreateCommand();
        cmd.CommandText = @"
SELECT s.Id, s.StartedAt, s.CompletedAt, s.Status,
       COUNT(DISTINCT CASE WHEN p.OriginalPath IS NOT NULL THEN p.OriginalPath END) AS PhotoCount
FROM Sessions s
LEFT JOIN Photos p ON p.SessionId = s.Id
WHERE s.EventId = $event
GROUP BY s.Id, s.StartedAt, s.CompletedAt, s.Status
ORDER BY s.StartedAt DESC";
        cmd.Parameters.AddWithValue("$event", eventId.ToString("D"));
        var result = new List<SessionSummary>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new SessionSummary
            {
                Id = Guid.Parse(reader.GetString(0)),
                StartedAt = DateTime.Parse(reader.GetString(1)),
                CompletedAt = reader.IsDBNull(2) ? null : DateTime.Parse(reader.GetString(2)),
                Status = reader.GetString(3),
                PhotoCount = Convert.ToInt32(reader.GetInt64(4))
            });
        }
        return result;
    }

    public string GetEventFolder(EventProfile item) => Path.Combine(_appRoot, "events", item.Id.ToString("N"));
    public void EnsureEventFolders(EventProfile item)
    {
        var root = GetEventFolder(item);
        foreach (var name in new[] { "original", "framed", "gif", "live-photo", "exports", "sessions" }) Directory.CreateDirectory(Path.Combine(root, name));
    }
}

public sealed class GalleryItem
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public DateTime CapturedAt { get; set; }
    public string OriginalPath { get; set; } = "";
    public string FramedPath { get; set; } = "";
    public string GifPath { get; set; } = "";
    public string LivePhotoPath { get; set; } = "";
}

public sealed class SessionSummary
{
    public Guid Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "";
    public int PhotoCount { get; set; }
}
