# QA giao diện PC: Cài / Cài lại / Gỡ / Khôi phục mod R29 — 19/11/2026

**Phạm vi:** thao tác nút thực tế qua UI Automation trên ứng dụng WPF build từ nguồn commit `1b1be6e`, phiên bản EXE `3.0.24`, trong **thư mục game QA giả**, dùng PAK R29 thật có SHA256 `38527E34FFEB8EF7538206D3D4DBD5CBAC54A1E6CD9D0957501035FF41A8E598`. Game giả chỉ có executable dummy, stock SIG và Mount mô phỏng. **KHÔNG phải kết quả game khởi động/thế giới thật.**

## Thao tác qua UI thực tế và bằng chứng filesystem

| Thao tác trên giao diện | Quan sát sau thao tác | Kết quả |
| --- | --- | --- |
| Mở app từ build + chọn GameRoot QA giả | UI hiện v3.0.24, GameRoot hợp lệ | PASS |
| Bấm **Cài Việt hóa** | Sinh R29 text pak/sig/sidecar và font Lexend pak/sig/receipt/sidecar; pak đúng SHA | PASS |
| Tự dọn xung đột trước cài | 9 file loader, PAK/font cũ, mod ngoài được cách ly; backup 9/9 SHA hợp lệ; 3 Mount stock nguyên | PASS |
| Bấm **Cài Việt hóa** lần thứ hai | PAK giữ SHA đúng, không tạo thêm backup xung đột | PASS |
| Bấm **Gỡ Việt hóa** | File R29 và font/sidecar được gỡ; stock Mount giữ nguyên | PASS |
| Bấm **Khôi phục mod** sau gỡ và xác nhận Yes | 9/9 file phục hồi đúng SHA; không mất backup | PASS |
| Bỏ chọn **Kèm font tiếng Việt**, bấm Cài | Lexend và `Mount/wahufont.txt` vẫn được cài | **BUG tồn tại** (R29 bỏ qua tham số font) |
| Bấm **Khôi phục mod** trong lúc R29 đang hoạt động | Loader/PAK cũ được phục hồi trong khi R29 vẫn Mount | **BUG P0 tái hiện ở build trước vá** |

Mọi thay đổi game xảy ra **trong fixture QA**, không thao tác game người dùng hoặc sản phẩm đang chạy. Cấu hình GamePath người dùng được hoàn trả đúng giá trị trước test; thông số LastUpdateCheck biến đổi trong phiên được giữ lại thay vì ghi đè bằng backup cũ.

## Vá P0: không phục hồi mod xung đột khi R29 còn bật

- `src/VHWuWa.Infrastructure/ViethoaInstaller.cs`: `RestoreQuarantine` từ chối phục hồi nếu file kích hoạt `Mount/wahuviethoa.txt` hoặc `Mount/wahufont.txt` còn tồn tại, kể cả trường hợp R29 không đầy đủ. Cũng từ chối nếu một bản Việt hóa còn được xác nhận đã cài.
- `src/VHWuWa.App/ViewModels/InstallViewModel.cs`: lệnh **Khôi phục mod** bị vô hiệu hóa khi R29 đang bật; sau Refresh cập nhật trạng thái nút. Back-end vẫn là chốt chặn chính.
- Regression `RestoreQuarantine_BlocksWhileR29MountActive_ThenRestoresAfterDeactivation`: **FAIL trước vá / PASS sau vá**.
- Smoke `VHWuWa.R29SmokeQa` dùng PAK thật: **PASS** chặn restore khi R29 còn bật, gỡ R29 xong mới phục hồi 9/9 file với SHA256 chính xác.

### Kiểm thử nhánh hotfix độc lập

Worktree QA tách riêng tránh ảnh hưởng các file UI đang được một phiên khác sửa. Lệnh:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\QA_TRUOC_PHAT_HANH_PC.ps1 -Mode SourceOnly -R29Pak "<path to verified R29 PAK>"
```

Kết quả sau vá, 19/11/2026:
- **156/156** hạ tầng và **69/69** Core: PASS.
- Build WPF: **0 lỗi, 0 cảnh báo**.
- PowerShell scanner bảo vệ `MountLauncher.txt`, `MountOption_Pack_*` và `MountLang_*`: PASS.
- Smoke PAK thật trên game QA giả: PASS, tự cách ly và khôi phục 9 file, giữ nguyên file stock, chặn cài/gỡ khi game đang chạy.
- Cổng `readyToRelease=false`: game runtime thật **HOLD**, không tự phát hành.

## Chưa xử lý / release HOLD

1. Bỏ chọn font mà R29 vẫn cài Lexend 660; cần làm rõ thiết kế sản phẩm rồi sửa nhất quán UI và installer nếu cho phép người dùng tắt font.
2. Chưa xác nhận tương thích game sử dụng resource khác với PAK R29 gốc `3.8.11`.
3. Cài R29/font thất bại sau khi cách ly mod **chưa tự rollback tất cả mod**.
4. Chưa có QA mở game thật, vào thế giới và test Steam/Kuro từ người báo lỗi.
5. Vá P0 trong nhánh riêng **không đồng nghĩa ZIP công khai v3.0.24 đã được sửa**; không thay release, không tự tăng version.

**Kết luận:** đã tái hiện nguyên nhân xung đột từ chính thao tác UI **Khôi phục mod**, vá fail-closed trong nguồn, test qua unit và PAK thật trên fixture. Trạng thái runtime game thật vẫn **HOLD**.
