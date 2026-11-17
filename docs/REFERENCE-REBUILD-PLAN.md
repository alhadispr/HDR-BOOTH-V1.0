# HDR BOOTH — Rebuild plan based on supplied Fotobu references

## Source review
- `REFERENSI HDR BOOTH.zip` contains 30 screenshots.
- Screenshots show a minimal white/light-gray product UI with warm orange accent, left-side administration navigation, event dashboard, customer welcome screen, camera preview, template selection, output/QR result, gallery and settings pages.
- HDR BOOTH keeps its own red brand accent as previously agreed; do not copy Fotobu logos, proprietary source, or bundled media assets.

## Required parity work (preserve existing agreed feature scope)
1. Admin shell: persistent left navigation, dashboard, event list, active-event state, gallery, global settings.
2. Event management: create/edit/archive/activate; one active event at a time; settings isolated per event.
3. Event settings: Welcome/closing, layout/template selection, timer and retake limits, camera, printer, cloud/sharing, payment/gatekeeper, voucher, HDR BOOTH output extras.
4. Template workflow: visual template browser, preview, frame editor, slot editing and per-event template storage.
5. Customer flow: welcome -> template selection -> camera preview -> countdown/capture -> review/retake -> outputs -> closing.
6. Output pipeline: framed composite, all originals, animated GIF, Live Photo package, QR sharing, printing, Artivive Moving Polaroid export/link/QR.
7. Gallery/history: filter, inspect, open outputs and session history.
8. Device validation: Canon EOS 3000D via supported camera provider/SDK, printer, LAN QR access, Artivive import/publish.

## Important implementation notes
- This pass improves the settings navigation from horizontal tabs to a left-side navigation and refines sizing/spacing in the admin and customer windows. Existing control names and event handlers are preserved.
- The existing code still contains a DEMO camera provider; Canon EDSDK diagnostics are not a camera integration. Do not advertise Canon capture as operational until tested with the camera and supported SDK.
- Cloud upload/payment processing require actual provider credentials and integration; checkboxes/settings alone do not implement a payment gateway or cloud service.
- Artivive export is a manual Bridge import/publish workflow; the application does not automatically publish to Artivive.
- Do not remove or silently downgrade any of the features listed above.

## Installer
GitHub Actions compiles a self-contained x64 publish and then Inno Setup creates `HDR-BOOTH-Setup-1.0.0-x64.exe`. The installer installs under Program Files and preserves user data in `%LOCALAPPDATA%\HDR-BOOTH` when uninstalled.
