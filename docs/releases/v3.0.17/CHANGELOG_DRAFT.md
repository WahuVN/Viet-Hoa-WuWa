# Changelog v3.0.17 (nháp)

## Cài Việt hóa
- Giao diện cài đặt chỉ hiện nút **Cài Việt hóa**; nút Gỡ, Quét, Xóa/Cách ly mod và Mở bản sao chuyển vào **Tùy chọn khác** (mặc định đóng).
- Hiển thị danh sách mod có thể xung đột và số file đã dọn.
- Kiểm tra file trước khi xóa/cài lại, tránh đụng nhầm file game và bản sao lưu.
- Giữ lại các file backup như `.bak`, `.backup`, `.orig`, `.old` và `version_goc.dll`.
- Dừng thao tác khi game đang chạy, file bị khóa hoặc dữ liệu quản lý bản cài không hợp lệ.
- Kiểm tra lại PAK, font và loader bằng SHA-256 sau khi cài.
- Hạn chế bấm nhiều thao tác cài, gỡ hoặc xóa cùng lúc.

## Font chữ
- Thêm Lexend Variable, mặc định weight 650.
- Có thanh chỉnh độ đậm 100–900 cho font hỗ trợ; font tĩnh sẽ khóa thanh kéo.
- Xem trước tiếng Việt, tạo font đúng weight trước khi cài.
- Giữ 72 font cũ, nâng danh mục lên 73 font.
- Đóng gói sẵn Lexend và công cụ tạo font, không cần người dùng cài Python.

## Kiểm tra
- Source hiện tại đã có kết quả **130/130 bài test PASS**.
- Bản 3.0.17 chưa build chính thức, chưa thử trực tiếp trên game và chưa chốt nội dung PAK mới.
- Chưa đẩy GitHub hay thông báo Discord.

*Đây là các thay đổi đang chuẩn bị so với bản 3.0.16. Không tính phần tự dọn mod cơ bản và sửa launcher của 3.0.16 là tính năng hoàn toàn mới.*
