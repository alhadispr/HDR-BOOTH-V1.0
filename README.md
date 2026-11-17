# HDR BOOTH — Windows x64 Release Candidate 1.0.1

## Isi paket
- Admin dashboard dan manajemen event multi-event.
- Pengaturan event dan pengaturan global terpisah.
- Customer booth flow, frame editor, multi-slot compositor.
- Foto original, foto final, GIF, paket Live Photo, retake per foto.
- Galeri event, riwayat sesi, QR sharing LAN, dan pencetakan Windows.
- Ekspor paket Moving Polaroid untuk Artivive, termasuk tautan/QR setelah publish manual.

## Status dan batasan yang wajib diketahui
Paket ini adalah **source release candidate**, bukan installer yang sudah dikompilasi.
Installer Windows x64 dibuat oleh GitHub Actions menggunakan .NET 8 dan Inno Setup.
Lingkungan penyusunan ini tidak memiliki .NET SDK atau Inno Setup, sehingga belum dapat
menjalankan kompilasi Windows atau menguji installer secara lokal.

Kamera saat ini masih menggunakan `DemoCameraProvider`. Canon EOS 3000D belum mempunyai
driver EDSDK operasional. Artivive Bridge import/publish masih manual. QR sharing yang
sudah ada adalah melalui jaringan lokal; belum sama dengan cloud hosting publik.
Pengaturan pembayaran/cloud belum berarti gateway pembayaran atau layanan cloud sudah
terintegrasi.

## Membuat installer EXE melalui GitHub
1. Upload/commit seluruh isi folder repository ini ke branch `main`.
2. Buka tab **Actions** di GitHub.
3. Pilih workflow **Build HDR BOOTH (Windows)**.
4. Klik **Run workflow** atau tunggu workflow berjalan setelah push.
5. Pastikan job `build` berstatus hijau.
6. Buka hasil run dan unduh artifact **HDR-BOOTH-Installer-x64**.
7. Di dalam artifact terdapat `HDR-BOOTH-Setup-1.0.0-x64.exe`.

Installer memasang aplikasi ke Program Files dan membuat shortcut. Data pengguna di
`%LOCALAPPDATA%/HDR-BOOTH` sengaja dipertahankan ketika aplikasi di-uninstall.

## Build manual di Windows
Persyaratan:
- Windows 10/11 x64
- .NET 8 SDK
- Inno Setup 6

Jalankan PowerShell dari root repository:

```powershell
./scripts/build-windows.ps1
$env:Path += ";${env:ProgramFiles(x86)}\\Inno Setup 6"
ISCC.exe installer.iss
```

Output:
- `artifacts/HDR-BOOTH-win-x64/` — aplikasi publish.
- `artifacts/HDR-BOOTH-win-x64.zip` — paket portable.
- `artifacts/installer/HDR-BOOTH-Setup-1.0.0-x64.exe` — installer.

## Checklist sebelum dipakai untuk acara
- [ ] Aplikasi dapat dipasang, dibuka, dan dihapus.
- [ ] Buat, edit, aktifkan, dan arsipkan event.
- [ ] Pastikan hanya satu event aktif.
- [ ] Pastikan setiap event memiliki frame, galeri, dan konfigurasi sendiri.
- [ ] Uji capture demo, countdown, retake, dan review.
- [ ] Uji GIF, paket Live Photo, dan file output.
- [ ] Uji QR sharing dari HP pada Wi-Fi/LAN yang sama.
- [ ] Uji printer fisik dan ukuran kertas.
- [ ] Uji impor/publish Artivive di Artivive Bridge.
- [ ] Integrasikan dan uji Canon EOS 3000D menggunakan SDK resmi.
- [ ] Uji resolusi layar, Windows scaling, dan izin folder.

Jangan gunakan untuk acara produksi/berbayar sebelum seluruh checklist perangkat yang
relevan dinyatakan lulus.
