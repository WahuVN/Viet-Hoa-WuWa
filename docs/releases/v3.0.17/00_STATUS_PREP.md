# VHWuWa 3.0.17 – tình trạng hiện tại

**Đã có bản RC1 để kiểm thử, chưa đăng GitHub hoặc Discord.**

## Đã xong
- App và updater đã lên phiên bản **3.0.17.0**.
- Giao diện cài Việt hóa gọn hơn trên cả trang chủ và trang cài. Nút chính là **Cài Việt hóa**, các chức năng gỡ/quét/xóa/cách ly đưa vào **Tùy chọn khác**.
- Font cài kèm mặc định đã đổi thật sang **Lexend 650**. PAK `WahuFont_100_P.pak` được đóng từ Lexend 650, không phải Beaufort đổi tên.
- Thư viện có 73 font. Lexend Variable chỉnh weight 100–900, mặc định 650.
- **130/130 test PASS**, Release build 0 lỗi/0 cảnh báo.
- Mở được app PC trên WIN3, phiên bản và trang Font đúng; thử mở/thu gọn các tùy chọn trên trang chủ thành công.
- Kiểm thử updater **3.0.16 → 3.0.17 PASS**: đã chạy cả bộ giả lập rollback/SHA và thử updater 3.0.16 cài nguyên ZIP RC1, mở app mới, nhận health-check, giữ nguyên cấu hình. Xem `UPDATER_316_317_QA.md`.
- Đã tạo **ZIP QA 3.0.17 RC1**, đủ 569 file, kiểm tra CRC toàn ZIP PASS và font PAK SHA khớp.

## Bộ cài QA nội bộ
`D:\WAHU_WORK\WUWA_MOD_CLEAN_SAFE_20261013\dist\VietHoa-WuWa-v3.0.17-RC1-QA.zip`

SHA-256: `75364afbeb21981914b50026fc3050c8122faa323d741917de16036a5a8bb241`

Font PAK SHA-256: `ffc75bd057e15e47f288f2c1861b1f63b6443916e0cf255a987a055bd7da3632`

## Còn chờ để phát hành chính thức
- Máy WIN3 chưa có thư mục game Wuthering Waves để test trực tiếp. **Chưa có kết quả chạy trong game**, DX11/DX12, màn hình thoại hoặc font in-game.
- PAK dịch Tên Anh và Hán Việt trong RC1 **giữ nguyên nội dung đã có từ bản QA 3.0.16**. Chưa có dữ liệu thoại mới được chốt cho 3.0.17.
- Updater đã nâng cấp gói ZIP RC1 thật trong thư mục thử nghiệm Windows và PASS; chưa thử tự tải từ GitHub vì 3.0.17 chưa public, cũng chưa nghiệm thu trong game thật.
- Chưa tag/push GitHub, chưa công bố Release, chưa chỉnh manifest BotDis và chưa gửi thông báo.

Tài liệu gửi người chơi: `RELEASE_NOTES.md`, `DISCORD_ANNOUNCEMENT.md`, `GITHUB_RELEASE_BODY_DRAFT.md`. Checklist kỹ thuật giữ riêng trong `RELEASE_QA_CHECKLIST.md`.
