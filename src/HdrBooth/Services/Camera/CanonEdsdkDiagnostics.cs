using System;
using System.IO;
using System.Linq;

namespace HdrBooth.Services.Camera;

public sealed record CanonSdkDiagnostic(bool IsAvailable, string? DllPath, string Message);

public static class CanonEdsdkDiagnostics
{
    public static CanonSdkDiagnostic Check()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "EDSDK.dll"),
            Path.Combine(AppContext.BaseDirectory, "Canon", "EDSDK.dll"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Canon", "EDSDK.dll"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Canon", "EDSDK.dll")
        }.Distinct(StringComparer.OrdinalIgnoreCase);

        var found = candidates.FirstOrDefault(File.Exists);
        if (found is not null)
            return new CanonSdkDiagnostic(true, found, $"Canon EDSDK ditemukan: {found}. Kamera tetap perlu diinisialisasi dan diuji.");

        var pathDirs = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        found = pathDirs.Select(dir => Path.Combine(dir.Trim(), "EDSDK.dll")).FirstOrDefault(File.Exists);
        if (found is not null)
            return new CanonSdkDiagnostic(true, found, $"Canon EDSDK ditemukan melalui PATH: {found}. Kamera tetap perlu diinisialisasi dan diuji.");

        return new CanonSdkDiagnostic(false, null,
            "Canon EDSDK belum ditemukan. Pasang SDK resmi Canon dan letakkan DLL sesuai arsitektur aplikasi sebelum integrasi kamera diaktifkan.");
    }
}
