# Game không mở sau khi cài Việt hóa PC

**Triệu chứng:** launcher mở được, game không vào; gỡ Việt hóa lại vào bình thường. Đây là lỗi cần kiểm tra tương thích bản dịch/mod, **không tự kết luận hỏng game**.

## Các phiên bản cần chú ý

| Ứng dụng PC | Cơ chế cài | Rủi ro khi chuyển từ bản cũ |
| --- | --- | --- |
| v3.0.10–v3.0.18 | PAK/font tại `Client/Content/Paks/~WuWaMods`, DLL `version.dll`, `verorg.dll`, `WuWaVH.dll` ở `Client/Binaries/Win64` | Loader cho game 3.7, xung đột proxy DLL/mod khác hoặc lỗi sau khi game đổi phiên bản |
| v3.0.19–v3.0.21 | PAK R29 trong `Client/Saved/Resources/3.8.0/wahuviethoa`, kích hoạt bằng `Mount/wahuviethoa.txt` | Có cơ chế xử lý mod cũ nhưng không chứng minh sạch 100% mọi bản cài |
| v3.0.22–v3.0.24 | R29 riêng và Lexend 660 qua `wahufont`; **không tự dọn loader/PAK cũ** | Có thể tồn tại đồng thời hai bộ dịch hoặc hai bộ font; nhánh R29 luôn cài Lexend 660 kể cả khi tham số cài font là false |

**PAK R29 công khai:** 125.532.999 byte, SHA-256 `38527E34FFEB8EF7538206D3D4DBD5CBAC54A1E6CD9D0957501035FF41A8E598`, được đóng cho tài nguyên **3.8.11**. Tên thư mục `3.8.0` là đường dẫn tài nguyên của game, không chứng minh độ tương thích. Có tài nguyên **3.8.12** không có nghĩa đã nghiệm thu khả năng khởi động của R29 trên bản đó.

**Lưu ý:** Kiểm tra PAK/SIG/sidecar khớp hash chỉ xác nhận **cài file đúng**, không kiểm tra game khởi động thành công. Máy bị lỗi có thể còn xung đột DLL, hai PAK dịch, hai PAK font, PAK/SIG không tương thích hoặc mod bên thứ ba.

## Tự kiểm tra mà không sửa game

1. **Đóng game và launcher**, ghi lại phiên bản ứng dụng PC đã sử dụng khi gặp lỗi (v3.0.16, .18, .19, .22...) và tài nguyên game hiện tại.
2. Không cài chồng hoặc gỡ/xóa thử `version.dll`, `winhttp.dll`, PAK hay SIG. Sao lưu các mod trước khi xử lý.
3. Tải mã nguồn từ repository này, mở PowerShell trong thư mục dự án rồi chạy:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\CHECK_WUWA_MOD_CONFLICT.ps1 -GameRoot "D:\Games\Wuthering Waves\Wuthering Waves Game" -OutputJson "$env:USERPROFILE\Desktop\WuWa_ConflictReport.json"
```

Thay `-GameRoot` bằng **đúng thư mục gốc có `Client`**. Script **chỉ đọc**, không sửa game; tệp JSON được tạo **ngoài** thư mục game.

4. Xem mục `warnings` và `r29` trong JSON. Kiểm tra `r29.filesComplete`, `legacyPakPresent`, `legacyLoaderPresent`, `fontFilesComplete`, `legacyFontPresent`. Chúng cho biết **file hiện diện**, không chứng minh game đã nạp file đó.
5. Nếu cần xác định đúng điểm sập, gửi báo cáo cùng thông tin từ `Client/Saved/Logs` (nếu có) và **Windows Event Viewer → Windows Logs → Application** (tên module gây lỗi). Xóa hoặc che thông tin cá nhân/đường dẫn tài khoản trước khi đăng công khai.

## Khoanh vùng theo triệu chứng

- **Nháy cửa sổ rồi thoát / sập ngay:** kiểm tra DLL loader cũ, `version.dll`, `winhttp.dll` và mod inject trước.
- **Vào launcher nhưng game dừng khi tải:** đối chiếu PAK, SIG, file kích hoạt Mount, hai bộ font và phiên bản tài nguyên.
- **Gỡ Việt hóa thì vào bình thường:** tập trung vào những file do bộ Việt hóa thêm/sửa, **chưa đủ cơ sở khẳng định là PAK hỏng**.

Nếu đã phát hiện nhiều bộ cài, chỉ gỡ qua **đúng công cụ đang quản lý tệp** và dùng bản sao/manifest để đối chiếu. Nếu phần gỡ báo file bị thay đổi hoặc không xác nhận chủ sở hữu, dừng và gửi báo cáo; **không xóa file lạ bằng tay**.

Báo cáo này đối chiếu mã nguồn/tag lịch sử và thử bộ kiểm tra trên **3 thư mục game mô phỏng**. Không phải kết quả kiểm thử khởi động trên máy của người báo lỗi. Không kèm thay đổi ZIP hay phiên bản phát hành.
