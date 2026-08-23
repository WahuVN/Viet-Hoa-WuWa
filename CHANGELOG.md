# Nhật Ký Thay Đổi (Changelog)

Tất cả các thay đổi đáng chú ý của dự án VHWuWa được ghi lại tại đây theo chuẩn [SemVer](https://semver.org/lang/vi/).

## [2.0.0] - 2026-08-21 (Cộng đồng 3.6)
### Thêm mới & Nâng cấp
- **Dữ liệu Việt hóa Wuthering Waves 3.6:** 100% Cốt truyện chính 3.6 (1.827 câu) + Kỹ năng các mốc 3.4, 3.5, 3.6.
- **Hệ thống Font chữ 2.0:** Tích hợp hơn 70+ bộ font tiếng Việt chuẩn Unicode, hỗ trợ tìm kiếm/lọc thời gian thực, xem trước chữ mẫu có dấu tiếng Việt trực quan, chuyển đổi và khôi phục font 1-click.
- **Hai biến thể Tên Nhân Vật độc lập:** Tùy chọn chuyển đổi linh hoạt giữa Tên Tiếng Anh canonical (*Jinhsi, Changli, Yangyang...*) và Tên Hán Việt (*Kim Tịch, Trường Ly, Ương Ương...*).
- **Bộ công cụ dịch thuật WAHU Community 3.6:** Hỗ trợ duyệt cốt truyện theo tuyến, dịch tay nhiều dòng, nhận diện ngữ cảnh và xưng hô nhân vật.
- **Bảo mật & Ẩn UID:** Tự động ẩn UID bằng ký tự trắng khi quay chụp màn hình, tích hợp kiểm tra mã băm SHA-256 hậu kiểm sau khi cài đặt.

## [1.0.0] - 2026-07-14
### Thêm mới
- Ứng dụng WPF (.NET 8, MVVM, DI, Wpf.Ui) với các trang chức năng: Trang chủ, Cài Việt hóa, Quản lý mod, Font chữ, Đồ họa, Hướng dẫn, Cài đặt.
- Hệ thống gói `.vhwpack` (manifest + SHA-256 + chữ ký RSA), bảo vệ chống path traversal / zip-slip.
- Tự động sao lưu và khôi phục khi cài/gỡ; quản lý mod (bật/tắt, phát hiện xung đột); chỉnh đồ họa theo cấu hình mẫu.
- Tự động kiểm tra bản cập nhật mới qua GitHub Releases kèm trình cập nhật độc lập `VHWuWa.Updater`.
- Công cụ CLI đóng gói và ký số `VHWuWa.PackageTool`.

