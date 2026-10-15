# CompatGuard DEV 3 – Nút Xóa xung đột cũ riêng

Ngày 16/10/2026 • chỉ dành cho bản thử nghiệm PC, chưa thay thế Stable 3.0.17.

## Tính năng

- Trang **Cài đặt Việt hóa** có mục riêng **3 · Xóa xung đột Việt hóa cũ**, nút Quét lại / Xóa xung đột cũ / Mở vùng cách ly.
- Liệt kê file dự kiến xử lý trước khi yêu cầu người dùng xác nhận **Có** (mặc định **Không**).
- Chỉ quét `Client/Binaries/Win64/wuwaVietHoa` (Đặng Dev 2.3.0), `wuwaIndonesia` (fork), `winhttp.dll` **nếu SHA-256 trùng DLL Đặng Dev đã xác minh**.
- Không chọn `version.dll`, `verorg.dll`, `WuWaVH.dll`, `~WuWaMods` của WAHU, không động vào PAK chính thức của game và thư mục mod không liên quan.
- Không xóa vĩnh viễn: chuyển vào `AppData/Quarantine` theo nguyên đường dẫn tương đối, xác nhận SHA mỗi file, lưu `quarantine-manifest.json`. Mở thư mục cách ly để khôi phục thủ công.
- Không xử lý symlink, file đang bị khóa, game/launcher đang chạy hoặc thư mục không phải game.
- Nếu DLL `winhttp.dll` chưa xác minh SHA sẽ **giữ nguyên** để người dùng kiểm tra thủ công. Thư mục còn backup cũng có thể cần xử lý thủ công.
- Lệnh **Xóa tất cả mod ngoài (vĩnh viễn)** cũ được phân biệt rõ trong Tùy chọn khác; không tự gọi khi dùng nút mới.
- Chức năng cài đặt/cập nhật tự động cũ chưa thay đổi; lớp CompatGuard trước đó vẫn chặn xung đột từ các folder WinHTTP.

## QA

- Infrastructure Installer **105/105 PASS** gồm 6 test mới cho tính năng này: không đụng WAHU/Mod khác, cách ly PAK + font, giữ DLL không đúng SHA, xử lý file khóa, đường dẫn sai, game đang chạy.
- Build UI/EXE và ZIP được kiểm tra riêng trong đợt này.
- Không chạy game thực tế, không đưa trực tiếp vào Stable và không đẩy thông báo Discord.

Mã nguồn tham khảo xác thực: `docs/releases/v3.0.17/COMPAT_WITH_OTHER_WUWA_TRANSLATORS_DEV_20261016.md`.
