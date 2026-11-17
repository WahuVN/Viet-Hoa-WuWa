# Xử lý mod cũ và xung đột

## Cài Việt hóa R29

Đóng game và launcher, chọn đúng thư mục chứa `Client`, rồi bấm **Cài Việt hóa**. R29 hiện chỉ có biến thể **tên Anh** và dùng **font gốc của game**.

Trước khi can thiệp, ứng dụng kiểm tra thư mục và SHA-256 gói R29. Chỉ file mod cũ/xung đột đã nhận diện rõ mới được chuyển vào vùng **Quarantine** có bản sao và manifest SHA-256 để khôi phục.

**Mount phụ không xác minh được phải bị chặn và giữ nguyên**, không tự xóa. Người dùng cần xác minh file thuộc mod nào trước khi cài tiếp. Không ghi vào PAK, SIG hoặc Mount chính thức.

## Quy tắc xử lý file mod

- Mod được xác minh sẽ được cách ly bên ngoài thư mục game, không bị xóa vĩnh viễn.
- Mod không rõ nguồn, file gốc và bản sao lưu người dùng được giữ nguyên.
- Nếu thiếu gói/sai SHA-256, đường dẫn không hợp lệ, game đang chạy hoặc thiếu quyền ghi, quá trình cài sẽ dừng an toàn.
- Nếu quá trình gặp lỗi, hãy đọc log và kiểm tra vùng cách ly trước khi thử lại.

## Cài lại và cập nhật

Chỉ bấm **Cài lại Việt hóa** sau khi ứng dụng xác minh R29 và thư mục game hợp lệ. Với cập nhật ứng dụng, chỉ dùng gói đúng phiên bản từ nguồn chính thức; updater xác minh checksum và có cơ chế khôi phục khi khởi động bản mới lỗi.

## Khi không thể cài

Kiểm tra thư mục chứa `Client`, đóng game, quyền truy cập và file Mount không rõ nguồn. R29 chỉ tạo các thư mục và tệp WAHU sau khi xác minh PAK và SIG nguồn hợp lệ. **Không tự tạo SIG rỗng, không quét đại SIG ngôn ngữ và không sửa file chính thức** để bỏ qua cảnh báo.

Vào **Trợ giúp → Hỗ trợ Discord** để gửi ảnh lỗi, phiên bản game, phiên bản ứng dụng và log đã ẩn thông tin cá nhân.
