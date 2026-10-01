# VHWuWa v3.0.12 — Sửa ghi đè khi cài đặt

Bản vá cho luồng cài/cập nhật VHWuWa trên Windows. Nội dung Việt hóa Wuthering Waves 3.7.0 không thay đổi.

## Đã sửa

- Khi cài lại hoặc cập nhật, các file do VHWuWa quản lý được **ghi đè trực tiếp** bằng bản mới.
- Không còn xóa mù file cũ rồi bỏ qua lỗi.
- File VHWuWa bị đặt thuộc tính **Read-only** sẽ được xử lý để có thể ghi đè bình thường.
- Kiểm tra quyền ghi trước tại:
  - `Client\Content\Paks\~WuWaMods`
  - `Client\Binaries\Win64`
- Nếu Windows từ chối quyền ghi/xóa, ứng dụng báo rõ:
  - hãy đóng VHWuWa;
  - mở lại `VHWuWa.exe` bằng **Run as administrator**;
  - kiểm tra quyền ghi nếu game nằm trong `Program Files`.
- Nếu file đang bị game, launcher hoặc chương trình khác giữ, ứng dụng báo rõ cần đóng chương trình đang sử dụng file rồi thử lại.
- Trang Cài đặt hiện hộp thoại lỗi rõ ràng thay vì chỉ để lỗi ở dòng trạng thái.
- Tiếp tục bảo vệ file VHWuWa hợp lệ khỏi nút **Xóa mod ngoài**.

## Cập nhật

- Đang dùng **v3.0.9 / v3.0.10 / v3.0.11**: mở VHWuWa và kiểm tra cập nhật.
- Cài mới: tải `VietHoa-WuWa-v3.0.12.zip`, giải nén và mở `VHWuWa.exe`.

## Đã kiểm thử

- Test riêng ghi đè file Read-only: đạt.
- Test riêng file đang bị khóa: đạt và trả thông báo đúng.
- Toàn bộ bộ kiểm thử: **110/110 đạt**.

## Hỗ trợ

- Discord Wuthering Waves VN: https://discord.gg/Gy5YQ84Yc2
- Báo lỗi / góp ý: https://github.com/WahuVN/Viet-Hoa-WuWa/issues
