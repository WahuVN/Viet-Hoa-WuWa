# Khắc phục lỗi

## Không tìm thấy game

Chọn thư mục **Wuthering Waves Game** có thư mục con `Client`, sau đó bấm **Kiểm tra file**.

## Cài xong vẫn còn tiếng Anh

- Đặt **Text Language = English**.
- Mở lại app và kiểm tra trạng thái Việt hóa.
- Cập nhật lên bản mới nếu chỉ một số nội dung mới của game còn tiếng Anh.

## Chữ lỗi dấu hoặc ô vuông

Gỡ font/mod chữ bên ngoài rồi cài lại font tiếng Việt trong tab **Font chữ**.

## Game văng hoặc báo Bad Image

1. Tắt môi trường C# thử nghiệm nếu đang bật.
2. Gỡ Việt hóa và thử game gốc.
3. Xử lý `version.dll` hoặc loader ngoài nếu app báo xung đột.
4. Nếu game gốc vẫn lỗi, dùng **Verify/Repair** và kiểm tra driver GPU.

> Lỗi `DXGI_ERROR_DEVICE_REMOVED` thường liên quan driver/GPU hoặc DirectX, không tự động chứng minh do Việt hóa.
