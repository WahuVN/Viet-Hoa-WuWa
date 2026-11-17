# ZIP portable và giao diện PC — QA 15/11/2026

## Mục tiêu
- Giữ nguyên phương thức phân phối **ZIP portable**: người dùng giải nén vào thư mục tự chọn, chạy `VHWuWa.exe`, app tạo shortcut Desktop `WuWa`, cập nhật **tại chỗ** khi đồng ý.
- Không tạo Setup.exe/MSI; không yêu cầu quyền Admin lúc khởi động và không di chuyển thư mục người dùng.
- Duy trì khóa cài đặt nếu client game hoặc PAK/loader không được xác minh.

## Giao diện
- Đặt style `WahuStandardButtonStyle` dùng chung có chủ đích cho **nút WPF thường** tại 10 màn hình, với vùng bấm tối thiểu 36px, màu theo theme, focus/hover/disabled rõ ràng. Không ghi đè theme của `ui:Button`.
- Nút thao tác chính ưu tiên `Appearance=Primary` ở Trang chủ (Cài Việt hóa), Font (áp dụng), Cài đặt (cập nhật), Đồ họa (áp dụng tùy chỉnh), nhưng command và điều kiện mở khóa được giữ nguyên.
- Thêm **Mở thư mục ứng dụng** trong Cài đặt; lệnh đọc `AppContext.BaseDirectory`, mở đúng thư mục chứa EXE đang chạy.
- Hướng dẫn tab Trợ giúp + tệp `Guides/vi-VN/01-bat-dau.md`, `02-...`, `03-...` đã chỉnh đúng cấu trúc ZIP, nhãn nút và trạng thái từ chối cài khi không tương thích.
- Đã phát hiện thử một kiểu implicit style toàn cục làm nút trắng; **kiểu này đã được loại bỏ**. Ảnh QA sau sửa xác nhận nút có chữ sáng và nền xám nhất quán.

## GitHub Actions ZIP
- Khắc phục lỗi tên `VHWuWa.Updater.next.exe` không được trình updater nhận: giữ nguyên `VHWuWa.Updater.exe`.
- Workflow xác minh `VHWuWa.exe`, `VHWuWa.Updater.exe` ở gốc ZIP và không có tên updater sai; in SHA-256 sau đóng gói.
- Gói QA cũ từ 14/11 chứa 82 entries, có đủ hai EXE, không có tên `.next.exe`; SHA-256 `27821E5BA14DC2C34EBF11753C6F5D1C1AE42CBD2FD460AE6FF8EF07359D9693`. **Đây không phải ZIP mới chứa UI của lượt này**.

## Nghiệm thu WIN3
- Build WPF Release: **0 warning, 0 error**.
- Test toàn solution ngày 15/11: **165/165 PASS** (50 Core + 115 Infrastructure; workspace có thay đổi song song từ công việc game).
- Trực tiếp mở WPF trên WIN3, chuyển được các tab: Trang chủ, Font game, Đồ họa, Trợ giúp, Tên nhân vật, Thuật ngữ, UID, Cài đặt. Trang Settings cuộn xuống cuối và khối ZIP portable không bị tràn.
- Self-update E2E dùng fixture độc lập: **PASS 3.0.18 → 3.0.19**, xác minh chuyển giao tiến trình, kiểm tra khởi động, rollback khi bản mới lỗi và từ chối SHA sai. Process exit code 0, không cập nhật app hoặc game đang dùng trên WIN3.

## Chưa nghiệm thu
- Chưa thử trên máy Windows sạch của người dùng mới; SmartScreen vẫn có thể cảnh báo EXE chưa ký số.
- Chưa chứng nhận loader/PAK PC trên client game 3.8.1, vì vậy không được mở khóa cài PAK hay phát hành ZIP chính thức.
- Chưa tạo Release v3.0.19 trên GitHub; mã nguồn UI/ZIP chỉ nằm ở nhánh chuẩn bị.
