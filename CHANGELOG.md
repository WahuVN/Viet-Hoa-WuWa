# Nhật Ký Thay Đổi (Changelog)

Tất cả các thay đổi đáng chú ý của dự án VHWuWa được ghi lại tại đây theo chuẩn [SemVer](https://semver.org/lang/vi/).

## [2.0.3] - 2026-08-23
### Cài đặt và quản lý xung đột
- Cài, gỡ và chuyển đổi Việt hóa/font an toàn hơn, theo dõi đúng các file do ứng dụng quản lý.
- Báo rõ mod có thể xung đột và cho phép người dùng chọn giữ bản sao hoặc xóa sau khi xác nhận.
- Cải thiện thao tác gỡ Việt hóa khi file đã bị thay đổi, tránh báo lỗi khó hiểu và không tự ý xóa file lạ.
- Cập nhật đúng liên kết tải gói Hán Việt của bản 2.0.3.

### Khởi chạy game
- Thêm nút mở trực tiếp `Client-Win64-Shipping.exe` và nút tắt game đặt cạnh nhau, không bắt buộc đi qua launcher.
- Có công tắc tùy chọn `-ForceEnableCSharpEnvironment`, mặc định tắt và cảnh báo rõ đây là môi trường C# thử nghiệm.
- Thêm nút sao chép tham số cho Steam Launch Options; app không tự sửa cấu hình Steam.
- Hiển thị hướng dẫn kiểm tra dấu `*` ở cuối phiên bản và cách quay về chế độ thường.

### WAHU Community
- Làm mới màn hình khởi động, logo, bố cục và các liên kết Discord/GitHub.
- Tinh gọn mục nhân vật, giữ riêng phần đặt tên và mặc định tải đầy đủ dữ liệu khi chọn `Tất cả`.
- Tách ba bộ tên độc lập: Hán Việt, tên Anh và tên tự đặt; có thể tạo PAK riêng cho từng bộ.
- Bản tên tự đặt tự dùng tên Hán Việt cho những nhân vật chưa được nhập tên riêng.
- Giảm cảnh báo QA sai đối với placeholder, thẻ định dạng và câu dài; sửa cách nhận diện dữ liệu khi nhiều bảng dùng cùng mã key.
- Bổ sung dữ liệu giao diện hệ thống vào PAK và Việt hóa màn cảnh báo nhạy cảm ánh sáng khi mở game.

## [2.0.2] - 2026-08-23
### Cải thiện
- Tự tìm đúng thư mục `Wuthering Waves Game` có `Client\Binaries\Win64\Client-Win64-Shipping.exe`.
- Nhận diện được đường dẫn ở thư mục cha, thư mục `Client`, `Content\Paks` hoặc file EXE của game.
- Bổ sung vị trí cài dạng `D:\Game\...`, thư mục Kuro Games tùy chọn và game đang chạy.
- Không nhận nhầm thư mục chỉ có tên giống game nhưng thiếu Client thực tế.
- Sửa tạo font tùy chọn: bỏ tham số `repak -s` không tồn tại, đóng gói V11 rồi chuyển và xác minh đúng định dạng PAK V12 của game.
- Làm rõ DirectX 11 chỉ là phương án thay thế khi DirectX 12 gặp lỗi.

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

