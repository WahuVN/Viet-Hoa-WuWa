# Việt Hóa Wuthering Waves v2.0.3

Bản 2.0.3 tập trung vào cài/gỡ an toàn hơn, xử lý xung đột rõ ràng, mở game trực tiếp và hoàn thiện bộ công cụ đặt tên nhân vật.

## Thay đổi chính

- Cài, gỡ và chuyển đổi Việt hóa/font an toàn hơn; không tự ý xóa file lạ.
- Báo rõ mod có thể xung đột, cho phép giữ bản sao hoặc xóa sau khi người dùng xác nhận.
- Cải thiện gỡ Việt hóa khi file đã bị thay đổi và cập nhật đúng gói Hán Việt 2.0.3.
- Hiển thị đúng phiên bản Việt hóa đã cài và sửa lỗi chữ tiếng Việt trong cửa sổ cập nhật.
- Mở game trực tiếp từ Trang chủ bằng `Client-Win64-Shipping.exe`; nút **Tắt game** được đặt ngay cạnh để dễ sử dụng.
- Tùy chọn bật môi trường C# thử nghiệm bằng `-ForceEnableCSharpEnvironment`; mặc định tắt, có cảnh báo lần đầu và có thể tắt ngay khi gặp lỗi.
- Có nút chép tham số cho Steam Launch Options. App không tự ý chỉnh sửa cấu hình Steam.

## WAHU Community và dữ liệu dịch

- Giao diện mở ứng dụng gọn đẹp hơn, dùng logo mới và có liên kết Discord/GitHub.
- Mục đặt tên nhân vật được tinh gọn và mặc định hiển thị đầy đủ dữ liệu khi chọn `Tất cả`.
- Hỗ trợ ba PAK tên độc lập: **Hán Việt**, **Tên Anh** và **Tên tự đặt**.
- Nhân vật chưa nhập tên riêng trong bản tự đặt sẽ tự dùng tên Hán Việt.
- Giảm các cảnh báo QA sai và sửa lỗi dữ liệu có thể bị ghép nhầm khi nhiều bảng dùng cùng mã key.
- Bổ sung dữ liệu giao diện hệ thống và Việt hóa màn cảnh báo nhạy cảm ánh sáng khi mở game.
- Sửa quy trình tạo font tùy chọn để đóng gói đúng định dạng PAK của game.

## Cập nhật ứng dụng

- Ứng dụng tự kiểm tra bản mới khi mở và đề xuất cập nhật nếu GitHub có phiên bản mới hơn.
- Phiên bản, liên kết tải và SHA-256 được đọc trực tiếp từ GitHub Release.
- Không cần phát hành thêm `update.json` hoặc `checksums.txt`.

## Tệp tải về

| Tệp | Mục đích |
| --- | --- |
| `VietHoa-WuWa-v2.0.3.zip` | Ứng dụng cài Việt hóa dành cho người chơi |
| `App-Dich-WuWa-v2.0.3.zip` | WAHU Community dành cho dịch giả và người đóng góp |
| `WuWaVH_HanViet_99_P.pak` | PAK Hán Việt để cài thủ công khi cần |

Hỗ trợ: [Discord WAHU](https://discord.gg/tuRCj47sy) · [GitHub Issues](https://github.com/WahuVN/Viet-Hoa-WuWa/issues)
