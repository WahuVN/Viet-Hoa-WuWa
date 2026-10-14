# Danh mục thay đổi source → chức năng → test — chuẩn bị v3.0.17

**Mốc so sánh:** HEAD `724e57e` (source GitHub PC v3.0.16), branch `fix/mod-clean-safe-20261013`. Đây là file theo dõi thay đổi *chưa commit*. `git diff --stat`: 14 file tracked thay đổi (699 thêm / 84 xóa) tại thời điểm rà soát; còn các file font/tool untracked. Con số sẽ thay đổi nếu có sửa mã tiếp.

| File / nhóm | Các cập nhật cần đưa vào release note | Bằng chứng / ghi chú |
| --- | --- | --- |
| `src/VHWuWa.Infrastructure/ViethoaInstaller.cs` | Auto-clean hardening, quét nested Mods/_mods, bỏ qua backup/symlink, marker fail-closed, preflight lock, bản sao version.dll, hậu kiểm toàn bộ marker SHA, nhận diện bản cài WAHU chặt hơn | Có thay đổi trong Git diff; test Infrastructure |
| `src/VHWuWa.App/Views/InstallPage.xaml` | InfoBar mod xung đột, nút Xóa, Cách ly, Mở bản sao, Quét lại | XAML diff |
| `src/VHWuWa.App/ViewModels/InstallViewModel.cs` | Dialog xóa Yes/No với preview, hiển thị số file dọn, hạn chế nút khi Busy | ViewModel diff |
| `src/VHWuWa.Core/Abstractions/Interfaces.cs` | API LastQuarantineDirectory và LastAutoDeletedConflictCount | Interface diff |
| `src/VHWuWa.App/ViewModels/FontBackupViewModels.cs` | Chọn Lexend ưu tiên, weight 650, slider min/max, render preview, instantiate TTF, áp dụng qua pipeline font | ViewModel diff |
| `src/VHWuWa.App/Views/FontPage.xaml` | Thẻ slider weight và nhãn weight, khóa font tĩnh | XAML diff |
| `src/VHWuWa.App/Services/FontWeightTools.cs` | Parse OpenType fvar/OS2, tạo instance biến thiên, bộ xuất EXE hoặc Python fallback, cache, chống input ngoài dải | File source mới, chưa commit |
| `Fonts/fonts.json` | Thêm Lexend Variable thành font thứ 73 (72 cũ giữ nguyên) | Catalog diff |
| `Fonts/Lexend-Variable.ttf` | Tài nguyên font nguồn; `OFL-Lexend.txt` license | File mới |
| `Fonts/Lexend-650.ttf` / `Lexend-700-proof.ttf` | Bản thử nghiệm xuất weight, không bắt buộc đóng gói user release | File proof mới |
| `tools/font-weight-instance.exe`, `scripts/font-weight-instance.py` | Trình xuất instance weight offline + script tham khảo, cần đóng gói cùng app | Binaries/scripts mới |
| `scripts/package-lexend-preview.py` | Script tạo preview QA, không nhầm với build release chính | File mới |
| `src/VHWuWa.App/VHWuWa.App.csproj` | Copy font license/compiler vào publish, không nuốt vào single-file | csproj diff |
| `scripts/build-dist.ps1` | Bundle 73 font + Lexend offline/compiler, fail khi thiếu compiler; hai bản dịch/loader giữ luồng build hiện hành | Build diff |
| `Guides/vi-VN/06-doi-font.md` | Bổ sung hướng dẫn Lexend/weight | Markdown diff |
| `tests/VHWuWa.Infrastructure.Tests/ViethoaInstallerTests.cs` | Regression auto-clean/backup/proxy/marker/file lock/quarantine/game-running | Test diff |
| `AGENTS.md`, `CLAUDE.md`, `BUILD_AND_RELEASE_RULES.md` | Sửa thư mục WAHU `~WuWaMods` (khác `~mods` ngoài) | Markdown diff |

## Đã tồn tại từ 3.0.16 — không quảng bá là chức năng mới hoàn toàn
- Cài phiên bản Wuthering Waves 3.7, mở launcher Kuro thay vì Client trực tiếp.
- AUTO CLEAN nền tảng đã dọn mod/VH ngoài khi cài/cập nhật nhanh.
- Kiểm asset PAK, SHA, loader, cơ chế updater với rollback thuộc các đợt trước; 3.0.17 chủ yếu gia cố.

## Tuyên bố chưa có bằng chứng phát hành
- Không có file Player 3.0.17 đã build, tag v3.0.17, PAK EN/Hán Việt 3.0.17 đã nghiệm thu.
- Không tuyên bố các bản dịch 3.7 sửa thêm bao nhiêu dòng nếu chưa có diff authority.
- Không tuyên bố game chạy không crash, font luôn hiển thị đúng, mobile update cùng PC.

## Chức năng có rủi ro cần nêu rõ cho người dùng
- AUTO CLEAN của installer xóa **vĩnh viễn** file mod xung đột đã xác định; **không** tự quarantine trước. Muốn có bản sao phải chọn nút Cách ly riêng.
- Backup proxy được giữ, nhưng nếu đó là proxy của mod ngoài thì không tự restore chỉ dựa vào tên `version_goc.dll`.
- 73 font là **danh mục lựa chọn**, không phải tất cả 73 font đóng gói offline; Lexend có sẵn offline.
