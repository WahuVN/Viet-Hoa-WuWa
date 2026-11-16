# WuWa Việt Hóa PC v3.0.21 — Hotfix OneClick R29

Đây là bản hotfix của **v3.0.20**. Giữ nguyên ZIP portable và cập nhật trong ứng dụng, không chuyển sang Setup.exe.

**Sửa lỗi**
- Đồng bộ màn hình **Cài Việt hóa** với cơ chế OneClick R29 đã kiểm thử ở commit `a750dfa`. Nút Cài không còn bị khóa chỉ vì người dùng đã tick **Tên Hán Việt** hoặc **Cài font tiếng Việt**.
- Khi R29 chưa có gói Hán Việt/font riêng tương thích, ứng dụng **thông báo rõ** và sử dụng **bản tên Anh + font game hiện có**, đồng thời cập nhật lại trạng thái lựa chọn; không tuyên bố đã cài biến thể chưa tồn tại.
- Giữ cách tự tạo Mount phụ, nhận SIG từ `Lang_en/Base` hoặc thư mục phiên bản, sao lưu mod cũ/xung đột và bảo vệ file gốc như v3.0.20.
- Giữ bộ updater ZIP tại chỗ từ GitHub, kiểm tra SHA-256, tín hiệu khởi động và cơ chế hoàn tác khi cập nhật thất bại.

**Phạm vi:** Chỉ đồng bộ logic OneClick bị sửa ngược. PAK Việt hóa **R29** và dữ liệu game **3.8.11** không thay đổi.

**Lưu ý:** Nút cài hoạt động ngay khi đường dẫn game hợp lệ; việc tick Hán Việt/font không tạo ra PAK Hán Việt/font mới. Bản cài thực tế vẫn dùng tên quốc tế và font mặc định cho đến khi gói tương thích được nghiệm thu.
