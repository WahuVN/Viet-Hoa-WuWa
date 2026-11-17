# v3.0.22 release prep

- Public asset contract: `VietHoa-WuWa-v3.0.22.zip`.
- Update endpoint: GitHub Releases `/releases/latest`.
- Auto-check: enabled by default for new settings; existing opt-out is respected.
- Update notification: startup popup + Home update card + Settings controls.
- Full update: GitHub asset digest SHA-256 -> download verification -> bundled updater -> backup/rollback -> startup-health.
- R29 font default: Lexend 660 using separate `wahufont` mount.
- R29 character-name mode: English; HanViet remains unavailable until separately QA'd.
- QA: Core 62/62 + Infrastructure 144/144 = 206/206 PASS.
- Package verification: 90 files, ZIP byte-identical to stage, VerifyOnly PASS.
- Live WIN3 install: 3.0.22.0, startup-health PASS, user config/R29/font unchanged.
- Remote status at prep time: latest public v3.0.21; v3.0.22 tag/release absent.
- No GitHub tag/push/release was performed during preparation.
