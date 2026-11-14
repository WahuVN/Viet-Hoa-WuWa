# Kiểm tra trước khi công bố v3.0.19

**Trạng thái: CHƯA ĐỦ ĐIỀU KIỆN PHÁT HÀNH.** Chỉ dùng bản QA nội bộ; không đổi trạng thái `latest` hoặc đăng thông báo Discord.

- [x] Mã nguồn PC khai báo 3.0.19, app/updater QA có FileVersion 3.0.19.0.
- [x] Test Release chạy lại ngày 15/11/2026: 162/162 PASS.
- [x] Gói QA nội bộ có app, updater, font, editor và PAK PC R28; ZIP 82 mục; SHA-256 đã đối chiếu.
- [x] Đã có biên bản thử updater 3.0.18 → 3.0.19 trên môi trường mô phỏng.
- [x] Đã chuẩn bị nhật ký và nội dung thông báo bản nháp.
- [ ] Tìm được client game PC thật `Client/Binaries/Win64/Client-Win64-Shipping.exe` của bản mục tiêu.
- [ ] Xác minh loader 3.8.1 trên client thật, bảo đảm vào game và khôi phục được.
- [ ] Thử PAK và font 3.8.10 trong game: tiếng Việt, glyph, màn hình, nhiệm vụ, các lựa chọn tên.
- [ ] Chuẩn bị cặp PAK EN/Hán Việt có nghiệm thu và manifest phù hợp (không tái dùng PAK 3.7).
- [ ] Dựng ZIP **production** bằng pipeline chính thức, kiểm tra tải, giải nén, bản cài mới và nâng cấp 3.0.18 → 3.0.19.
- [ ] Xác minh SHA-256 của các asset trên GitHub và cập nhật README/changelog theo release đã công bố.
- [ ] Sau khi hoàn tất: tạo/cập nhật draft GitHub, duyệt lại notes, mới publish và đăng thông báo.

Gói `VietHoa-WuWa-v3.0.19-LOCAL-QA.zip` không phải asset chính thức và không được đổi tên để phát hành.
