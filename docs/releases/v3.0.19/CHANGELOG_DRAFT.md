# Nhật ký thay đổi — VHWuWa v3.0.19 (bản nháp)

Bản cập nhật tập trung vào giao diện, quản lý font và độ an toàn của quy trình cài đặt/cập nhật trên PC.

## Ứng dụng PC
- Tinh gọn Trang chủ, thanh điều hướng và các nhãn trạng thái để dễ đọc hơn.
- Đồng bộ sidebar sang màu xám trung tính, sử dụng bảng màu theo vai trò và logo PNG nguồn lớn; bỏ khung xanh thừa, giữ trạng thái chọn dễ nhận biết.
- Đồng bộ accent mặc định của slider, radio và các điều khiển WPF UI sang xám/trắng, tránh màu xanh hệ thống lẫn trong tab Font; đổi theme cũng giữ cùng bảng màu.
- Đồng bộ vùng bấm, kích thước, trạng thái hover/focus/disabled của nút thường ở 10 trang; nút cài Việt hóa và thao tác áp dụng quan trọng có cấp độ ưu tiên rõ ràng.
- Thêm hướng dẫn sử dụng ZIP portable, xem thư mục ứng dụng đang chạy trong Cài đặt; cập nhật hướng dẫn cài lần đầu cho đúng cấu trúc ZIP hiện tại.
- Sửa workflow tạo ZIP trên GitHub: giữ đúng `VHWuWa.Updater.exe` và kiểm tra tên tệp bắt buộc trước khi đóng artifact.
- Phân biệt launcher với client game thực; không báo đã cài Việt hóa dựa trên đường dẫn game cũ không còn tồn tại.
- Cải thiện xem trước font và điều chỉnh độ đậm; ngăn áp dụng/gỡ font khi chưa nhận diện được client hợp lệ.
- Bổ sung xử lý trạng thái PAK và kiểm tra bản cập nhật, giữ cơ chế xác minh SHA-256, sao lưu và khôi phục khi có lỗi.

## Dữ liệu Việt hóa
- Đã chuẩn bị riêng gói PAK PC R28 cho tài nguyên 3.8.10. Kiểm tra đóng/mở 62/62 mục đạt, nhưng **chưa nghiệm thu loader và hiển thị trong game Wuthering Waves 3.8.1**.
- Chưa mở cài tự động PAK R28. Bản PC và bản Android sử dụng PAK riêng, không hoán đổi lẫn nhau.

## Kiểm thử
- Unit test Release trên WIN3: 162/162 PASS (50 Core, 112 Infrastructure).
- Gói QA nội bộ đã thử nâng cấp 3.0.18 → 3.0.19 và giữ cấu hình thử nghiệm.
- **Chưa phát hành v3.0.19**; cần nghiệm thu game PC thực tế, loader, font và PAK trước khi công bố.

*Nhật ký này được chuyển thành nội dung công khai chỉ sau khi các điều kiện phát hành đạt.*
