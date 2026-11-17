# HDR BOOTH — Source Project v0.25

## STATUS PROYEK — v0.25 (pra-build Windows)

HDR BOOTH kini memiliki fondasi Admin Dashboard, multi-event, customer booth flow,
pengolahan foto, GIF, paket Live Photo, galeri, riwayat sesi, QR sharing LAN,
pencetakan Windows, Frame Editor, dan ekspor aset Artivive per foto.

**Status penting:** paket ini masih berupa source code. Build/compile Windows belum
dijalankan dari lingkungan penyusunan ini. Kamera masih memakai DemoCameraProvider;
Canon EOS 3000D belum terintegrasi. Impor dan Publish Artivive masih manual melalui
Artivive Bridge. QR Artivive hanya dibuat dari URL yang dimasukkan operator setelah
artwork diterbitkan.

### Tahap menuju EXE
1. Ekstrak source ZIP di komputer Windows.
2. Instal .NET 8 SDK atau Visual Studio 2022 dengan workload .NET desktop development.
3. Jalankan `scripts/build-windows.ps1` dari PowerShell.
4. Jika publish berhasil, file ada di `artifacts/HDR-BOOTH-win-x64/HDR-BOOTH.exe`
   dan paket distribusi ada di `artifacts/HDR-BOOTH-win-x64.zip`.
5. Jalankan aplikasi dan lakukan checklist pengujian Windows di bawah.

Alternatif: push repository ke GitHub, lalu jalankan workflow
**Actions → Build HDR BOOTH (Windows) → Run workflow**. Artifact build akan tersedia
pada hasil workflow.

### Checklist pengujian Windows sebelum dipakai di acara
- [ ] Aplikasi dapat dibuka dan ditutup tanpa error.
- [ ] Buat, edit, aktifkan, dan arsipkan event; pastikan hanya satu event aktif.
- [ ] Simpan pengaturan, frame, dan output; pastikan setiap event terpisah.
- [ ] Uji Frame Editor dan komposisi beberapa slot.
- [ ] Uji capture demo, countdown, retake foto tertentu, dan review.
- [ ] Uji GIF dan paket Live Photo pada Windows.
- [ ] Uji QR Sharing pada dua perangkat dalam Wi-Fi/LAN yang sama.
- [ ] Uji printer yang benar-benar terpasang.
- [ ] Uji ekspor paket Artivive, impor di Artivive Bridge, Publish, lalu scan di HP.
- [ ] Uji Canon EOS 3000D setelah integrasi EDSDK resmi ditambahkan.
- [ ] Uji resolusi layar, scaling Windows, serta jalur penyimpanan dengan izin terbatas.

**Jangan gunakan untuk acara berbayar/produksi sebelum checklist perangkat selesai.**


Fondasi aplikasi PhotoBooth Windows dengan Admin Dashboard, multi-event, pengaturan independen per event, SQLite, dan folder output terpisah.

## Persyaratan
- Windows 10/11
- Visual Studio 2022 dengan workload **.NET desktop development**
- .NET 8 SDK
- Koneksi internet saat restore NuGet pertama kali (untuk `Microsoft.Data.Sqlite`)

## Menjalankan
1. Ekstrak ZIP.
2. Buka `src/HdrBooth/HdrBooth.csproj` di Visual Studio 2022.
3. Tunggu NuGet restore selesai.
4. Pilih konfigurasi `Debug` dan target `Any CPU`.
5. Tekan F5.

## Fitur pada v0.3
- Dashboard Admin tema putih dengan aksen merah.
- Membuat event dan menyimpan pengaturan per event.
- Hanya satu event aktif pada satu komputer.
- SQLite database lokal (`hdr-booth.db`).
- Migrasi otomatis profil JSON dari versi sebelumnya ke SQLite; file lama diberi ekstensi `.migrated` dan tidak dihapus.
- Edit nama dan tanggal event.
- Arsip event tanpa menghapus hasil foto.
- Struktur folder output otomatis: `original`, `framed`, `gif`, `live-photo`, `exports`, dan `sessions`.
- Galeri event yang memindai file output pada folder event.
- Tabel metadata sesi dan foto, serta method repository untuk membuat sesi, mencatat hasil foto, dan menyelesaikan sesi.
- Pengaturan event: welcome/closing, layout, timer, kamera, printer, QR/cloud, pembayaran/voucher, dan output tambahan HDR BOOTH.

## Lokasi data
`%LOCALAPPDATA%/HDR-BOOTH/`

- `hdr-booth.db` — database SQLite.
- `events/<event-id>/` — folder output setiap event.
- `events/*.json` — file konfigurasi versi lama yang belum termigrasi atau gagal dibaca.

## Catatan galeri
Galeri saat ini menampilkan file yang sudah ada di folder output event. Alur capture belum terhubung, jadi file belum otomatis dibuat oleh aplikasi. Tabel `Sessions` dan `Photos` sudah disiapkan untuk pencatatan metadata ketika pipeline kamera dibangun.

## Belum diimplementasikan
Integrasi Canon EOS 3000D, live view, capture, frame compositor/editor, GIF, Live Photo, Moving Polaroid AR, QR sharing, pembayaran, cloud upload, dan pencetakan. Preferensi di halaman pengaturan saat ini menyimpan konfigurasi; belum mengoperasikan perangkat.

## Struktur utama
- `Models/EventProfile.cs` — model event dan konfigurasi.
- `Services/EventStore.cs` — repository SQLite, migrasi JSON, folder output, metadata sesi/foto.
- `MainWindow.xaml` — dashboard Admin.
- `Views/EventSettingsWindow.xaml` — konfigurasi event.
- `Views/EditEventWindow.xaml` — edit nama/tanggal event.
- `Views/GalleryWindow.xaml` — galeri file per event.

## Milestone 0.4 — Customer Booth Display (UI awal)
- Jendela Customer Display terpisah dari Admin Dashboard.
- Membaca nama event aktif dan konfigurasi welcome screen, layout, serta jumlah foto.
- Alur antarmuka: Welcome → Layout → Camera Preview placeholder → kembali/selesai.
- Tombol `Buka Booth Display` hanya membuka display untuk event yang sedang aktif.
- Area kamera masih placeholder; belum mengambil foto dan belum menjalankan countdown karena integrasi kamera Canon belum dibuat.

## Batasan versi ini
Customer Display pada v0.4 adalah kerangka alur UI yang bisa dinavigasi. Tidak ada klaim live view atau capture yang berfungsi sampai modul kamera diuji dan diintegrasikan.


## Milestone 0.5 — Camera pipeline foundation (v0.5)
- Added `ICameraProvider` abstraction so physical camera drivers can be added without coupling UI code to one SDK.
- Added `CameraCaptureService` to save original PNG captures into the active event's `original` folder.
- Added session/photo metadata registration through the existing SQLite repository.
- Added a clearly labeled **Demo Camera** provider for testing the capture → save → gallery metadata flow without hardware.
- Booth Display now shows the latest test capture and capture count.

### Important hardware limitation
The current provider is a **demo image generator**, not a Canon camera driver. It does not connect to Canon EOS 3000D, does not provide real Live View, and must not be treated as a real camera test. Canon support requires the official Canon EDSDK package and its matching native DLLs/runtime, which must be obtained and installed separately under Canon's SDK terms. The next hardware integration step is to implement an EDSDK-backed `ICameraProvider`, then test USB detection, remote capture, image download, and Live View on the user's Windows PC with the actual camera.

## Current version status
- v0.5 is source-code work and has not been compiled or hardware-tested on Windows in this environment.
- The capture button is explicitly labeled `UJI CAPTURE (MODE DEMO)` to avoid implying that a physical camera is connected.


## Milestone 0.6 — Photo processing foundation (v0.6)
- Added `PhotoProcessingService` for PNG frame compositing using a transparent PNG overlay.
- Added animated GIF encoding from a sequence of still images using WPF `GifBitmapEncoder`.
- Frame overlay currently scales to the full photo canvas; production frame editor still needs configurable photo-slot coordinates, fit/crop rules, preview, and frame asset selection UI.
- GIF generation utility is implemented but is not yet wired into the customer display workflow or automatic session completion.
- Live Photo and Moving Polaroid AR remain future modules.

## GitHub Actions Windows build
- Added `.github/workflows/build-windows.yml`.
- After the project is pushed to a GitHub repository, open **Actions → Build HDR BOOTH (Windows) → Run workflow**.
- The workflow publishes a self-contained Windows x64 build and uploads it as an artifact named `HDR-BOOTH-win-x64`.
- This workflow has not been run from this environment; GitHub repository setup and the first remote build are still pending.


## Milestone 0.7 — Frame Editor (fondasi)
- Editor frame PNG transparan per event.
- Slot foto dapat ditambah/dihapus dan diatur menggunakan koordinat persentase X, Y, lebar, dan tinggi.
- Preview menampilkan batas area slot di atas frame.
- Template disimpan sebagai `frame-template.json` di folder event masing-masing, sehingga konfigurasi antar-event terpisah.
- Frame Editor dibuka dari tab Layout & Timer pada Pengaturan Event.

Catatan: milestone ini menyimpan definisi slot dan preview. Mesin komposisi multi-slot yang mengisi setiap area foto dari hasil jepretan, editor drag-and-drop, serta uji runtime Windows masih merupakan pekerjaan lanjutan. Ukuran canvas template saat ini menggunakan default 1200×1800 dan belum menjadi kontrol UI.


## Milestone 0.8 — Multi-slot frame compositor
- Mengisi slot template dengan foto hasil capture secara berurutan.
- Setiap foto di-crop dari tengah agar memenuhi rasio slot (mode fill).
- PNG frame transparan digambar paling akhir sebagai lapisan teratas.
- Hasil akhir PNG disimpan di folder `framed` milik event dan diregistrasikan di galeri/database.
- Customer Display menjalankan proses komposisi otomatis setelah jumlah foto sesuai pengaturan event tercapai.

Batasan saat ini: capture masih menggunakan DemoCameraProvider (foto simulasi), bukan Canon EOS 3000D. Jika template tidak ada atau gagal, foto original tetap tersimpan. Live View, countdown otomatis, retake, printer, QR, GIF/Live Photo dan AR belum menjadi bagian dari alur capture ini.


## Milestone 0.9 — Alur sesi foto (fondasi)
- Countdown per jepretan mengikuti pengaturan event.
- Jumlah jepretan dibatasi sesuai pengaturan event (1–12).
- Tombol ulangi foto terakhir menghapus file original terakhir dan catatan fotonya sebelum pengambilan ulang.
- Layar review menampilkan hasil akhir ber-frame jika template tersedia.
- Sesi dapat diselesaikan dan status sesi ditandai selesai.
- Foto original tetap disimpan meskipun template frame tidak tersedia atau proses render gagal.

Catatan pengujian: alur ini masih memakai DemoCameraProvider, bukan Canon EOS 3000D. Tombol review dapat dibuka sebelum jumlah foto lengkap; validasi jumlah minimum/target untuk penyelesaian sesi perlu disempurnakan. Countdown memakai jeda UI sederhana. Belum ada live view, retake untuk foto tertentu, printer, QR, atau sinkronisasi cloud.


## Milestone 0.10 — GIF otomatis
- GIF dibuat dari semua foto original pada sesi ketika layar review dibuka.
- Urutan frame mengikuti urutan pengambilan foto.
- Jeda antar-frame diatur 700 ms dan metadata delay GIF ditambahkan.
- File disimpan ke folder `gif` milik event dan diregistrasikan ke database/galeri.
- GIF hanya dibuat jika opsi Enable GIF aktif dan minimal dua foto tersedia.
- Jika foto terakhir di-retake, GIF dan output layout lama pada sesi tersebut dihapus sebelum hasil baru dibuat.
- Kegagalan membuat GIF tidak menghapus atau mengganggu foto original.

Catatan: aplikasi belum menampilkan pemutar GIF animasi pada layar review; status dan lokasi file GIF ditampilkan. Uji kompatibilitas encoder dan playback tetap perlu dilakukan di Windows. GIF menggunakan foto original, bukan hasil foto ber-frame.


## v0.11 — Preview Final, GIF, dan Live Photo

- Halaman review sekarang memiliki tab **Foto Final**, **GIF**, dan **Live Photo**.
- Preview foto final menampilkan hasil layout/frame; jika template frame belum tersedia, preview memakai foto original terakhir.
- Preview GIF menggunakan browser control internal agar animasi GIF dapat bergerak.
- Live Photo tahap ini berupa paket motion photo: foto still (menggunakan foto final ber-frame jika tersedia) + video MP4 + `manifest.json`.
- Video MP4 dipilih melalui tombol **Pilih Video Live Photo** dan dapat diputar, pause, serta stop di tab Live Photo.
- Paket Live Photo disimpan pada folder event `live-photo/<session-id>/` dan jalurnya dicatat di SQLite.

**Batasan penting:** tahap ini belum merekam video secara otomatis dari kamera. Video MP4 masih dipilih/import manual. Paket ini adalah format motion photo milik HDR BOOTH, bukan Apple Live Photo native. Integrasi perekaman video otomatis dan pengujian Canon EOS 3000D menyusul setelah provider kamera/video siap.


## v0.12 — Preview foto individual & retake pilihan
- Review menampilkan thumbnail seluruh foto dalam sesi.
- Setiap thumbnail memiliki tombol **Ulangi Foto**.
- Foto yang diulang menggantikan posisi foto terpilih, bukan menambah foto baru.
- Setelah penggantian, output final/GIF/Live Photo sebelumnya dibersihkan dan dapat dibuat ulang.
- Pengujian runtime Windows masih diperlukan.


## v0.13 — QR Code & berbagi melalui Wi-Fi lokal
- Tombol **Buat QR & Bagikan** tersedia di halaman review.
- QR membuka halaman hasil sesi yang di-host sementara oleh aplikasi di komputer booth.
- File yang dibagikan dibatasi pada hasil sesi yang didaftarkan: foto final/original, GIF, serta still/video Live Photo jika tersedia.
- QR PNG disimpan di folder `exports` pada event.
- Server hanya berjalan selama jendela booth masih terbuka; pelanggan harus memakai jaringan Wi-Fi/LAN yang sama.
- Tidak memerlukan layanan cloud atau upload internet.

**Batasan:** fitur ini berbagi melalui jaringan lokal, bukan tautan publik. Firewall Windows dapat meminta izin jaringan; akses HP juga bergantung pada isolasi klien/router. Untuk berbagi dari jaringan seluler atau jarak jauh, perlu integrasi cloud pada milestone terpisah.


## v0.14 — Windows printer integration foundation
- Daftar printer Windows dapat dimuat ke pengaturan event.
- Nama printer, ukuran kertas (2x6, 4x6, 5x7, A4), dan jumlah salinan disimpan per event.
- Tombol **Cetak Foto** pada halaman review membuka dialog cetak Windows.
- Foto final ber-frame diprioritaskan; jika belum ada frame, foto original terakhir digunakan.
- Ukuran media dan jumlah salinan diterapkan sebagai nilai awal pada print ticket.

**Batasan:** hasil cetak bergantung pada driver printer, ukuran media yang benar-benar didukung, border/margin printer, dan pilihan pada dialog Windows. Belum dilakukan pengujian printer fisik.

\n## v0.15 — Fondasi diagnostik kamera Canon\n\n- Menambahkan pemeriksaan keberadaan `EDSDK.dll` pada folder aplikasi, folder Canon umum, dan PATH Windows.\n- Menambahkan pilihan mode `Canon EOS EDSDK` dan `Demo / Pengujian` pada pengaturan event.\n- Menampilkan hasil diagnostik SDK pada pengaturan event dan layar booth.\n- Kamera yang dipakai booth masih provider Demo; integrasi Live View/shutter Canon belum aktif karena membutuhkan SDK resmi, arsitektur DLL yang sesuai, dan pengujian pada Windows dengan kamera.\n
\n## v0.16 — Galeri event lebih lengkap\n\n- Galeri kini memiliki pencarian berdasarkan nama file dan jenis output.\n- Tombol muat ulang untuk menampilkan file terbaru setelah sesi selesai.\n- File dapat dibuka dengan klik dua kali atau tombol Buka File Terpilih menggunakan aplikasi default Windows.\n- Menampilkan jumlah file yang cocok dengan pencarian dibanding total file event.\n
\n## v0.17 — Riwayat sesi per event\n\n- Menambahkan jendela Riwayat Sesi yang dibuka dari Dashboard Admin setelah memilih event.\n- Riwayat menampilkan ID sesi singkat, waktu mulai, waktu selesai, jumlah foto original, dan status sesi.\n- Ringkasan total sesi dan total foto original dihitung khusus untuk event terpilih.\n- Data dibaca dari tabel SQLite Sessions dan Photos; tidak membuat data contoh.\n
\n## v0.18 — Customer layout preview\n\n- Layar pelanggan kini membangun preview slot berdasarkan jumlah foto dan nama layout pada pengaturan event, bukan selalu menampilkan empat slot statis.\n- Layout Strip ditampilkan vertikal; Kolase 2x2 ditampilkan dua kolom; layout lain menyesuaikan jumlah slot.\n- Jika event memiliki Frame Template tersimpan dan PNG overlay tersedia, preview PNG frame ditampilkan di layar pemilihan layout.\n- Jika frame belum tersedia atau file tidak dapat dibaca, preview slot tetap ditampilkan dengan keterangan yang sesuai.\n

## Update v0.19 — Camera Capture Experience
- Countdown besar di tengah area preview, dengan animasi pulse setiap detik.
- Efek flash visual setelah foto berhasil diambil.
- Label konfirmasi foto tersimpan.
- Tombol dan alur sesi yang sudah ada tetap dipertahankan.
- Mode kamera masih DEMO; preview adalah foto hasil jepretan simulasi, bukan live view Canon.


## Update v0.20 — Pemeriksaan Kesiapan Sesi
- Sebelum sesi dimulai, aplikasi memeriksa nama event, jumlah foto, keberadaan file frame, dan kemampuan menulis ke folder event.
- Jika frame belum tersedia, pengguna diberi tahu bahwa sesi masih dapat dilanjutkan tanpa frame.
- Aplikasi secara eksplisit memperingatkan bahwa kamera masih MODE DEMO dan belum mengambil foto dari Canon EOS 3000D.
- Pemeriksaan folder penyimpanan melakukan uji tulis sementara dan menghapus file uji setelah berhasil.

## Status Moving Polaroid AR
Moving Polaroid AR otomatis **belum diimplementasikan** pada v0.20. Fitur ini memerlukan pipeline terpisah: video/animasi per foto, image-target/marker yang dicetak, pembuatan paket AR, QR/deep link untuk membuka pengalaman AR di ponsel, dan aplikasi/web AR dengan image tracking. Paket Live Photo HDR BOOTH yang sudah ada bukan fitur AR.


## Update v0.21 — Moving Polaroid AR Asset Foundation
- Added a per-event/session/photo Moving Polaroid manifest.
- Review screen can pair the latest captured (or framed) photo with a selected MP4 video.
- Package includes target image, overlay video, `manifest.json`, and `README-AR.txt`.
- Package assets can be included in the existing local-LAN QR sharing list.
- Important: this milestone prepares and pairs AR assets only. Automatic image recognition, camera tracking, and video anchoring on a phone are NOT implemented yet.


## Update v0.22 — Artivive Export Workflow
- Moving Polaroid asset export is now explicitly an Artivive Bridge import package.
- The package contains a trigger image, MP4 video layer, metadata manifest, and step-by-step import guide.
- Added an early file-size check for MP4 videos above 100 MB.
- The local QR share can include the Artivive import assets and guide.
- HDR BOOTH does not yet upload/publish to Artivive automatically. No official API integration has been configured; publishing and WebAR QR generation remain in Artivive Bridge.


## Update v0.23 — Artivive Artwork Link & QR
- Added a field to paste the published Artivive artwork/WebAR URL.
- Validates that the link is an absolute HTTP/HTTPS URL.
- Saves the URL, timestamp, and QR filename in the per-photo package manifest.
- Generates a dedicated `artwork-qr.png` for the Artivive URL.
- The URL must be copied from Artivive after publishing; HDR BOOTH does not publish artwork or verify the URL's ownership/status.


## Update v0.24 — Per-photo Artivive Mapping & Gallery
- Review screen lets the operator choose an individual captured photo as the Artivive Trigger Image.
- Each exported package records the selected photo index, event, session, trigger image, and overlay video.
- Artivive export gallery rows display package status and saved artwork URL from the manifest.
- The existing gallery search and open-file workflow remains available.
- The selected original photo is used as the trigger target to avoid using the same composite layout image for every slot.
