# WuWa Việt Hóa PC v3.0.20

**Bản vá cài đặt PC và tự cập nhật.** Giữ gói ZIP portable, dữ liệu dịch **R29**, không dùng Setup.exe.

- Sửa nhận diện SIG tiếng Anh trong `Lang_en/Base`, thư mục phiên bản và vị trí SIG trực tiếp. Thư mục `Mount` phụ được tạo khi cần, không sửa `MountLang_en.txt` chính thức.
- Nút **Cài Việt hóa** cho phép thao tác khi đã nhận đúng thư mục game. Nếu SIG hoặc dữ liệu English thật sự thiếu, ứng dụng nêu chính xác vấn đề thay vì báo chung “thiếu Mount/SIG”.
- Gộp sửa giao diện font, nút và lựa chọn **Tên Hán Việt / Cài font tiếng Việt**. Không tự thay lựa chọn đã tick thành bản tên Anh khi bấm Cài.
- Giữ cơ chế sao lưu mod xung đột, bảo vệ PAK và tệp gốc của game.
- Sửa cờ nhận diện bản R29 từ 3.0.19 trở đi để **3.0.20 không rơi ngược về loader 3.7** khi cập nhật tại chỗ.
- Tự cập nhật qua GitHub: xác minh SHA-256 của ZIP, xác nhận khởi động ứng dụng mới và khôi phục bản cũ nếu nâng cấp thất bại.

**Lưu ý:** R29 hiện hỗ trợ bản giữ tên quốc tế và font mặc định của game. Gói Hán Việt/font PAK riêng chưa được nghiệm thu, nên hai lựa chọn này chưa thể áp dụng vào game. Không yêu cầu tắt Windows Defender hoặc SmartScreen.
