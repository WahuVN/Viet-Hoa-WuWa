# CHECKLIST phát hành GitHub v3.0.17 — QUAN TRỌNG

## Đã chuẩn bị và kiểm tra
- [x] Bản 3.0.16 đã phát hành có đường GitHub API `/releases/latest`, tự kiểm tra khi khởi động mặc định bật và hộp thoại Yes/No.
- [x] ZIP 3.0.17 đúng tên: `VietHoa-WuWa-v3.0.17.zip`.
- [x] Gồm `WuWaVH_EN_99_P.pak` và `WuWaVH_HanViet_99_P.pak`.
- [x] Gói ZIP RC3 đã qua CRC và chứa EXE version 3.0.17, PAK/font xác minh SHA.
- [x] Updater thật của 3.0.16 áp dụng bản RC3 thành công, giữ cấu hình, từ chối SHA sai.
- [x] 4 bài unit test xác nhận bản 16 phát hiện release JSON 17 và từ chối gói sai.
- [ ] Test font/thoại và xưng hô trực tiếp trong Wuthering Waves 3.7 trên máy thật.
- [ ] Kiểm tra hiển thị UI ở ít nhất hai cỡ DPI.
- [ ] Sau khi được chấp thuận, Publish GitHub Release `v3.0.17` không Draft, không Pre-release.
- [ ] Chạy script `Verify-GitHubLatest-v317.ps1` sau Publish, đảm bảo latest công khai báo `v3.0.17`, đủ ZIP/PAK và digest SHA trùng.
- [ ] Mở bản 3.0.16 trên máy khác, xác nhận hộp thoại có update, tải/cài và khởi chạy bản 17.

## Để bản 3.0.16 nhận cập nhật
Release phải có tag `v3.0.17` và đúng asset `VietHoa-WuWa-v3.0.17.zip` (không được đổi thành *-RC3.zip). GitHub API phải trả về `digest: sha256:<64hex>` của ZIP, và Release phải **Published**, **không Pre-release**. Dùng GitHub Releases “Publish release”; không chỉ push source/tag.

Khi Release còn Draft, bản 3.0.16 vẫn thấy 3.0.16 là latest và không báo sai.

## GitHub release
<https://github.com/WahuVN/Viet-Hoa-WuWa/releases>

Chỉ đăng bản public khi QA thực tế hoàn tất. Không upload source secrets, private keys hay key ký app lên GitHub.
