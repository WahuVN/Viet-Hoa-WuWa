# WuWa 3.0.17 – Compatibility DEV (16/10/2026)

Mục tiêu: ngăn lỗi khi người chơi đã từng dùng bộ mod khác rồi cài WAHU lên cùng game.

## Căn cứ từ GitHub

- Fork công khai **TitoTFP/WuwaID** (parent `CallMeDangDev/WuwaVH`) sử dụng proxy `winhttp.dll` và thư mục `Client/Binaries/Win64/wuwaIndonesia` để mount PAK.
- Fork launcher **TitoTFP/WuwaIDLauncher** có lô-gic cập nhật giao dịch và rollback. Đây là các bản fork, **không xác nhận là mã launcher Đặng Dev đang phân phối**.
- Bản khác **Lai-Hoang/wuwa-viet-hoa** dùng `winmm.dll` và `~mods`.
- WAHU PC 3.0.17 dùng `version.dll`, `verorg.dll`, `WuWaVH.dll`, `~WuWaMods`.

## Cải tiến đã triển khai (nhánh DEV)

- Quét nhận diện `Win64/wuwaIndonesia`, kể cả khi `winhttp.dll` đã bị xóa mà thư mục còn sót.
- Chặn **InstallAsync** và **UpdateTranslationPakAsync** trước giai đoạn tự dọn/xóa nếu phát hiện thư mục này; thông báo bằng tiếng Việt kèm gợi ý Quét mod / Cách ly mod.
- Đưa `wuwaIndonesia` vào danh mục mod ngoài được báo cáo để người dùng xem/quyết định.
- Giữ nguyên mọi file game và mod khác khi chặn, **không tự xóa hoặc hợp nhất loader**.
- Công cụ chỉ-đọc `scripts/CHECK_WUWA_MOD_CONFLICT.ps1` kiểm tra DLL, thư mục PAK và SHA marker; chưa chạy trên máy khách.

## Kiểm tra

- `dotnet test tests/VHWuWa.Infrastructure.Tests`: **96/96 PASS**, bao gồm **3 test** chặn auto-install/quick-update và giữ nguyên PAK mod ngoài.
- `dotnet publish` ứng dụng Windows x64 Release **PASS**.
- **Không có nghiệm thu runtime game thật**; không nên thông báo đã sửa xong crash cho mọi máy.
- Đây là **gói DEV**, không thay thế Stable v3.0.17 công khai.
- **ZIP cài tay cũ vẫn không chạy preflight**: chỉ đơn thuần copy `Client`, nên người dùng cần chạy kiểm tra/xác định mod còn sót trước khi dán.
- Không phát hành tự động lên kênh thông báo hay Discord.

Nguồn tham khảo:
- https://github.com/TitoTFP/WuwaID
- https://github.com/TitoTFP/WuwaIDLauncher
- https://github.com/Lai-Hoang/wuwa-viet-hoa
