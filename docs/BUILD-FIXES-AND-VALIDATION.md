# HDR BOOTH — Build fixes and validation

## Fixes applied to this source package

1. Removed the accidental extra closing brace after `ArchiveEvent_Click` in `MainWindow.xaml.cs`.
2. Removed the Windows Forms framework dependency from the WPF application project. This prevents namespace collisions between WPF and Windows Forms (`MessageBox`, `Color`, `Brushes`, `Point`, `FlowDirection`, and print/open dialog types).
3. Replaced the Global Settings folder picker with the native .NET WPF `Microsoft.Win32.OpenFolderDialog`.
4. Added the WPF Controls namespace to `BoothDisplayWindow.xaml.cs`, where controls are constructed dynamically.
5. Simplified GIF frame creation to the supported `BitmapFrame.Create(BitmapSource)` overload. The public WPF GIF encoder does not provide a dependable per-frame delay API; the existing delay argument is retained for compatibility with callers.

## Static checks performed

- All XAML files were parsed as XML.
- XAML event-handler names were checked against code-behind methods.
- Source files were scanned for remaining `System.Windows.Forms` references.
- ZIP package integrity was checked after packaging.

## Important build status

This environment does not have the .NET SDK installed, so this source package has **not** been compiled or run here. A successful GitHub Actions Windows build is still required before treating the EXE as verified. Camera, printer, QR sharing, and Artivive workflows also require device-level tests on Windows.

## Build

From the repository root on Windows with .NET 8 SDK installed:

```powershell
./scripts/build-windows.ps1
```

GitHub Actions can also be run from the Actions tab using the `Build HDR BOOTH (Windows)` workflow.

## Perbaikan CI tambahan
- Menambahkan global usings eksplisit untuk System.IO, LINQ, collections, threading, dan tasks.
- Menambahkan NuGet.Config dengan nuget.org sebagai package source.
- Memastikan PackageReference QRCoder 1.6.0 ada di project.
- Menambahkan tahap `dotnet build --no-restore` setelah restore agar error compile terlihat sebelum publish.
- Mengubah selector nama file pada LocalShareServer menjadi lambda eksplisit agar inferensi tipe nullable tidak ambigu.
