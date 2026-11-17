using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using QRCoder;

namespace HdrBooth.Services;

/// <summary>
/// Temporary LAN-only HTTP sharing endpoint. Only explicitly registered files are served.
/// The random session token is required for every request.
/// </summary>
public sealed class LocalShareServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _stop = new();
    private readonly Dictionary<string, string> _files;
    private readonly string _token;
    private readonly Task _loop;
    public string Url { get; }

    public LocalShareServer(IEnumerable<string> files)
    {
        _files = files.Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(Path.GetFileName, p => p, StringComparer.OrdinalIgnoreCase);
        if (_files.Count == 0) throw new InvalidOperationException("Belum ada file hasil yang bisa dibagikan.");

        _token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(18)).ToLowerInvariant();
        var ip = GetLanAddress() ?? throw new InvalidOperationException("Alamat IPv4 jaringan lokal tidak ditemukan. Pastikan komputer terhubung ke Wi-Fi/LAN.");
        _listener = new TcpListener(IPAddress.Any, 0);
        _listener.Start();
        int port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        Url = $"http://{ip}:{port}/{_token}/";
        _loop = Task.Run(() => ListenLoopAsync(_stop.Token));
    }

    public byte[] CreateQrPng()
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(Url, QRCodeGenerator.ECCLevel.M);
        var qr = new PngByteQRCode(data);
        return qr.GetGraphic(8);
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(cancellationToken); }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            try
            {
                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
                var requestLine = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(requestLine)) return;
                string[] parts = requestLine.Split(' ');
                if (parts.Length < 2 || parts[0] != "GET")
                {
                    await WriteTextAsync(stream, "405 Method Not Allowed", "Only GET is supported.", "text/plain");
                    return;
                }
                string path = Uri.UnescapeDataString(parts[1].Split('?')[0]);
                string prefix = "/" + _token + "/";
                if (!path.StartsWith(prefix, StringComparison.Ordinal))
                {
                    await WriteTextAsync(stream, "404 Not Found", "Not found.", "text/plain");
                    return;
                }
                string requested = path[prefix.Length..].Trim('/');
                if (requested.Length == 0)
                {
                    var html = BuildIndexHtml();
                    await WriteTextAsync(stream, "200 OK", html, "text/html; charset=utf-8");
                    return;
                }
                if (!_files.TryGetValue(requested, out var filePath) || !File.Exists(filePath))
                {
                    await WriteTextAsync(stream, "404 Not Found", "File not found.", "text/plain");
                    return;
                }
                var info = new FileInfo(filePath);
                string mime = Path.GetExtension(filePath).ToLowerInvariant() switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".mp4" => "video/mp4",
                    _ => "application/octet-stream"
                };
                var header = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: {mime}\r\nContent-Length: {info.Length}\r\nContent-Disposition: inline; filename=\"{Path.GetFileName(filePath)}\"\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(header, cancellationToken);
                await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await fs.CopyToAsync(stream, cancellationToken);
            }
            catch { /* Client disconnects and cancelled requests are expected. */ }
        }
    }

    private string BuildIndexHtml()
    {
        var b = new StringBuilder("<!doctype html><html><head><meta name='viewport' content='width=device-width,initial-scale=1'><meta charset='utf-8'><title>HDR BOOTH</title><style>body{font-family:Arial;background:#f7f7f8;margin:0;padding:24px;color:#222}h1{color:#d92d3a}a{display:block;background:white;padding:16px;margin:10px 0;border-radius:10px;color:#222;text-decoration:none;border:1px solid #eee}small{color:#777}</style></head><body><h1>HDR BOOTH</h1><p>Hasil sesi foto Anda</p>");
        foreach (var name in _files.Keys.OrderBy(x => x))
            b.Append("<a href=\"").Append(WebUtility.HtmlEncode(Uri.EscapeDataString(name))).Append("\">")
             .Append(WebUtility.HtmlEncode(name)).Append("</a>");
        b.Append("<small>Terhubung melalui jaringan Wi-Fi/LAN yang sama.</small></body></html>");
        return b.ToString();
    }

    private static async Task WriteTextAsync(NetworkStream stream, string status, string body, string contentType)
    {
        byte[] data = Encoding.UTF8.GetBytes(body);
        byte[] header = Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Type: {contentType}\r\nContent-Length: {data.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(header);
        await stream.WriteAsync(data);
    }

    private static IPAddress? GetLanAddress()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                 n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        {
            var address = ni.GetIPProperties().UnicastAddresses
                .Select(x => x.Address)
                .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork &&
                                     !IPAddress.IsLoopback(a) &&
                                     !a.ToString().StartsWith("169.254.", StringComparison.Ordinal));
            if (address is not null) return address;
        }
        return null;
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Stop();
        _stop.Dispose();
    }
}
