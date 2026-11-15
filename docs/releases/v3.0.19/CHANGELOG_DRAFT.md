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
- Sửa đóng gói PAK PC R29 theo cấu trúc đã dùng trên Android/PC cũ, giữ nguyên 62/62 DB của R28. Gói R28 PC cũ gây lỗi `Corrupt pak index`; R29 đã vào game thật và hiển thị tiếng Việt ở cảnh báo/đăng nhập.
- Đưa cơ chế cài/gỡ R29 vào chính ứng dụng **v3.0.19**: dùng Mount phụ, không chép loader DLL 3.7, kiểm tra SHA-256, bảo vệ tệp gốc và xác minh gỡ/cài lại trên PC.
- R29 mới hỗ trợ biến thể tên quốc tế; Hán Việt, font và độ phủ toàn bộ gameplay còn cần QA trước khi phát hành.

## Kiểm thử
- Unit test Release trên WIN3: **172/172 PASS** (56 Core, 116 Infrastructure).
- ZIP ứng viên v3.0.19 + R29 kiểm tra đầy đủ file bắt buộc, đúng tên updater; updater công khai 3.0.18 đã nâng sandbox thành công, không ảnh hưởng máy thật.
- Thêm khối an toàn chống rơi ngược về PAK/loader 3.7 khi gói R29 thiếu hoặc hỏng; sao lưu 6 tệp cũ theo SHA sau nâng cấp.
- Cải tiến giữ `editor/project.db` của người dùng khi cập nhật; máy cài mới dùng `editor/project.default.db` để khởi tạo dữ liệu làm việc.
- Gói QA nội bộ đã thử nâng cấp 3.0.18 → 3.0.19 và giữ cấu hình thử nghiệm.
- **Chưa phát hành v3.0.19**; bản PAK R29 đã được kiểm tra cảnh báo/đăng nhập, nhưng cần QA Hán Việt, font và nội dung gameplay trước khi công bố.

*Nhật ký này được chuyển thành nội dung công khai chỉ sau khi các điều kiện phát hành đạt.*
