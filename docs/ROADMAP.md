# HDR BOOTH Roadmap

## Milestone 0.1 — Admin shell
- [x] Dashboard Admin
- [x] Buat event
- [x] Daftar event
- [x] Aktivasi satu event
- [x] Penyimpanan profil event terpisah

## Milestone 0.2 — Event settings UI
- [x] Welcome Screen dan layar penutup
- [x] Layout foto, jumlah jepretan, countdown, retake
- [x] Preferensi kamera dan printer
- [x] Cloud, QR, pembayaran, voucher
- [x] Preferensi foto frame, original, GIF, Live Photo, Moving Polaroid
- [x] Simpan pengaturan ke profil event masing-masing

## Milestone 0.3 — Event lifecycle and data
- [x] SQLite repository dan migrasi JSON
- [x] Edit nama/tanggal event dan arsip event
- [x] Folder output otomatis per event
- [x] Galeri file per event
- [x] Tabel metadata sesi dan foto serta API pencatatan

## Milestone 0.4 — Booth display
- [ ] Welcome screen customer
- [ ] Pilih layout/frame
- [ ] Countdown dan capture flow
- [ ] Review, retake, closing screen

## Milestone 0.5 — Camera and image pipeline
- [ ] Camera adapter abstraction
- [ ] Canon EOS 3000D integration and hardware test
- [ ] Original photo preservation
- [ ] Frame compositor and layout editor
- [ ] GIF generation
- [ ] Live Photo output

## Milestone 0.6 — Output and devices
- [ ] QR code and local sharing
- [ ] Windows printer integration
- [ ] Print sizing/copies
- [ ] Cloud upload (optional)

## Milestone 0.7 — Moving Polaroid AR
- [ ] Define image/video pairing format
- [ ] Image target generation and tracking
- [ ] Smartphone AR viewer
- [ ] Test printed target reliability

## Milestone 0.8 — Release
- [ ] End-to-end testing
- [ ] Backup/restore
- [ ] Installer and Windows EXE publish


## v0.5 — Camera pipeline foundation
- Camera provider abstraction and demo provider.
- Original image file output per event.
- Session/photo metadata registration.
- Physical Canon EDSDK driver remains pending SDK installation and Windows hardware test.


## v0.6 — Photo processing foundation
- PNG transparent frame compositing utility.
- Animated GIF encoding utility from captured still images.
- GitHub Actions Windows x64 publish workflow.
- Frame editor UI, slot positioning, live photo, AR, and end-to-end automation remain pending.


## 0.7 — Frame Editor
- [x] Pilih PNG frame transparan.
- [x] Tambah dan hapus slot foto.
- [x] Atur posisi dan ukuran slot dengan persentase canvas.
- [x] Preview batas slot di atas frame.
- [x] Simpan template secara terpisah per event.
- [ ] Isi setiap slot secara otomatis dari hasil jepretan (multi-slot compositor).
- [ ] Drag-and-drop slot dan pengaturan ukuran canvas dari UI.


## 0.8 — Multi-slot frame compositor
- [x] Isi slot foto dari hasil capture berdasarkan urutan.
- [x] Crop tengah otomatis mengikuti rasio setiap slot.
- [x] Lapiskan PNG frame transparan di atas foto.
- [x] Simpan hasil akhir ke folder framed dan database event.
- [ ] Uji hasil render di Windows dengan beragam frame PNG dan rasio foto.


## 0.9 — Alur sesi foto
- [x] Countdown sesuai pengaturan event.
- [x] Batasi jumlah jepretan berdasarkan pengaturan event.
- [x] Ulangi foto terakhir dengan menghapus file dan metadata capture sebelumnya.
- [x] Layar review hasil.
- [x] Selesaikan sesi dan tandai selesai di database.
- [ ] Validasi sesi harus mencapai jumlah foto target sebelum dapat diselesaikan.
- [ ] Preview seluruh jepretan individual dan retake foto pilihan.
- [ ] Uji runtime Windows dan kamera fisik.


## 0.10 — GIF otomatis
- [x] Gabungkan seluruh foto original sesi menjadi GIF.
- [x] Urutan frame sesuai urutan capture.
- [x] Atur jeda frame 700 ms melalui metadata GIF.
- [x] Simpan ke folder gif per event dan catat ke database.
- [x] Hapus output GIF lama jika foto terakhir diulang.
- [ ] Preview animasi GIF di layar review.
- [ ] Uji kompatibilitas GIF di Windows dan perangkat smartphone.


## v0.12 — Preview setiap foto & retake pilihan
- [x] Thumbnail seluruh foto sesi tampil pada panel review.
- [x] Tombol ulangi tersedia untuk setiap foto, bukan hanya foto terakhir.
- [x] Foto yang diulang menggantikan urutan/slot yang dipilih.
- [x] Output final dan GIF lama dibersihkan agar dibuat ulang berdasarkan foto terbaru.
- [ ] Uji runtime dan alur retake di Windows.


## v0.13 — QR sharing lokal
- [x] QR code PNG dibuat dari URL sesi.
- [x] HTTP server sementara untuk jaringan lokal.
- [x] Halaman hasil sesi menampilkan tautan file yang diizinkan.
- [x] Foto final/original, GIF, dan file Live Photo yang tersedia dapat diunduh/dibuka.
- [x] QR disimpan ke folder exports event.
- [ ] Uji akses lintas perangkat dan izin firewall Windows.
- [ ] Cloud sharing publik (opsional, milestone berikutnya).


## v0.14 — Printer Windows
- [x] Muat daftar printer terpasang di Windows.
- [x] Simpan nama printer per event.
- [x] Pengaturan ukuran media 2x6, 4x6, 5x7, A4.
- [x] Pengaturan jumlah salinan.
- [x] Tombol cetak dari review menggunakan Windows PrintDialog.
- [ ] Uji ukuran aktual, margin, kualitas dan printer dye-sub/photobooth.

- [x] v0.19 — Countdown overlay, pulse animation, capture flash, dan konfirmasi hasil jepretan.
- [ ] v0.20 — Perapian alur customer display dan validasi kesiapan sesi sebelum mulai.

- [x] v0.20 — Pemeriksaan kesiapan sesi dan peringatan mode kamera demo.
- [ ] v0.21 — Fondasi Moving Polaroid AR: model aset per foto, image target/marker, video overlay, dan struktur paket AR.
- [ ] v0.22 — Halaman AR mobile berbasis image tracking dan QR untuk membuka pengalaman AR (butuh uji perangkat nyata dan koneksi jaringan).
- [ ] v0.23 — Integrasi alur AR ke event, galeri, dan ekspor.

- [x] v0.21 — Moving Polaroid asset package (image target + MP4 + manifest + LAN-share inclusion).
- [ ] v0.22 — Mobile image-tracking viewer integration; choose and validate a WebAR/native SDK approach.
- [ ] v0.23 — QR deep-link launches the AR viewer and binds each target to its overlay video.
- [ ] v0.24 — Multi-photo AR mapping, per-photo video assignment, and end-to-end device testing.

- [x] v0.22 — Artivive-ready export bundle with trigger image, MP4 layer, manifest, import guide, and basic file-size validation.
- [ ] v0.23 — Import/publish workflow refinement; investigate official Artivive API or supported partner integration before attempting automation.
- [ ] v0.24 — Store Artivive artwork URL/WebAR QR per photo/session and expose customer scan instructions.
- [ ] v0.25 — End-to-end test: HDR BOOTH photo → Artivive Bridge publish → printed Polaroid → Artivive app/WebAR scan.

- [x] v0.23 — Save published Artivive URL, persist it in manifest, and generate dedicated customer QR.
- [ ] v0.24 — Improve per-photo AR assignment and gallery/history management for multiple photos per session.
- [ ] v0.25 — Final integration review, build configuration, and Windows hardware test checklist.
- [ ] v1.0 — Compile Windows EXE, smoke test, and package installer/release.

- [x] v0.24 — Per-photo Artivive target selection and gallery metadata visibility.
- [ ] v0.25 — Final integration review, build configuration, and Windows hardware test checklist.
- [ ] v1.0 — Compile Windows EXE, smoke test, and package installer/release.


## v0.25 — Finalisasi source dan persiapan build Windows
- Added Windows x64 self-contained single-file publish profile.
- Added PowerShell build script that restores, publishes, verifies the EXE, and creates a ZIP distribution.
- Updated GitHub Actions workflow to use the same build script and upload the Windows artifact.
- Added a Windows hardware/feature test checklist.
- Build and runtime tests are intentionally pending until the project is opened on a Windows PC.

## Status setelah v0.25
- [x] Source-code milestones through v0.25.
- [ ] Windows .NET restore and compile.
- [ ] Run-time smoke test on Windows.
- [ ] Canon EOS 3000D EDSDK integration and camera test.
- [ ] Printer and LAN QR test.
- [ ] Artivive Bridge import, publish, and phone scan test.
- [ ] Release EXE after test issues are resolved.
