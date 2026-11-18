# Sửa lỗi không vào game sau khi cài Việt hóa

Nếu launcher mở được nhưng game không chạy, hoặc gỡ Việt hóa thì game chạy lại bình thường, hãy kiểm tra xung đột tệp Việt hóa và độ tương thích với phiên bản game.

## Cách xử lý

1. **Đóng game và launcher.** Tải mã nguồn tại [GitHub](https://github.com/WahuVN/Viet-Hoa-WuWa) (**Code → Download ZIP**), giải nén rồi mở PowerShell ở thư mục vừa giải nén.
2. **Kiểm tra**, không thay đổi tệp:

```powershell
.\scripts\FIX_WUWA_CONFLICT.ps1 -GameRoot "D:\Games\Wuthering Waves Game"
```

3. **Sửa các xung đột xác minh được**, có sao lưu và khả năng khôi phục:

```powershell
.\scripts\FIX_WUWA_CONFLICT.ps1 -GameRoot "D:\Games\Wuthering Waves Game" -Mode Repair -Apply
```

4. Mở game thử lại. Nếu chưa vào được, có thể **tắt riêng font Việt hóa** để khoanh vùng:

```powershell
.\scripts\FIX_WUWA_CONFLICT.ps1 -GameRoot "D:\Games\Wuthering Waves Game" -Mode DisableFont -Apply
```

5. Nếu muốn tạm tắt toàn bộ bản dịch R29 để xác định nguyên nhân, dùng `-Mode DisableR29 -Apply`. **Khôi phục** tệp đã cách ly bằng lệnh sau, thay phần đường dẫn sao lưu bằng giá trị `BACKUP_PATH` mà công cụ cung cấp:

```powershell
.\scripts\FIX_WUWA_CONFLICT.ps1 -GameRoot "D:\Games\Wuthering Waves Game" -Mode Restore -BackupPath "C:\Duong-dan-sao-luu" -Apply
```

**Lưu ý:** Thay đường dẫn ví dụ bằng đúng thư mục game có `Client`. Công cụ chỉ xử lý tệp Việt hóa nhận diện chắc chắn; tệp không xác định sẽ được giữ nguyên và báo cần kiểm tra. Nếu bản dịch chưa tương thích với tài nguyên game đang dùng, dọn xung đột **không thể tự làm cho bản dịch tương thích**.

Nếu vẫn gặp lỗi, hãy gửi phiên bản game, phiên bản Việt hóa và báo cáo kiểm tra tại [Issues](https://github.com/WahuVN/Viet-Hoa-WuWa/issues) hoặc [Discord](https://discord.gg/Gy5YQ84Yc2).
