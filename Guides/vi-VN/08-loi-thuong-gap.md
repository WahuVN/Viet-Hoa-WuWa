# Lỗi thường gặp

## App báo không thấy game

Chọn thư mục game (thư mục chứa `Client`), ví dụ:

```text
D:\Game\Wuthering Waves Game
D:\Games\Wuthering Waves\Wuthering Waves Game
C:\Wuthering Waves\Wuthering Waves Game
```

Sau khi chọn xong, bấm **Kiểm tra file** để xác nhận.

## Cài xong vẫn còn tiếng Anh

- Đặt ngôn ngữ chữ trong game là **English**.
- Có thể dùng DirectX 11 hoặc 12; chỉ chuyển sang DX11 nếu DX12 bị crash/lỗi đồ họa.
- Mở lại app và xem trạng thái hậu kiểm có báo đủ PAK/SIG/font/loader không.
- Nếu chỉ riêng cốt truyện/kỹ năng còn EN, cập nhật VHWuWa lên bản mới nhất.

## Chữ có dấu thành ô vuông

Cài lại với tùy chọn **Cài kèm font tiếng Việt**. Nếu đang dùng font/mod ngoài,
gỡ nó để tránh ghi đè font của bản Việt hóa.

## App báo có mod hoặc bản Việt hóa khác

Không cài chồng. Gỡ bản cũ bằng công cụ đã dùng để cài nó, rồi bấm **Quét lại mod**
trong tab **Cài Việt hóa**. VHWuWa sẽ hiển thị đường dẫn PAK/loader còn xung đột.

## Game văng

1. Gỡ Việt hóa và thử game gốc.
2. Nếu đang bật **Môi trường C# thử nghiệm**, hãy tắt công tắc và mở lại game ở chế độ thường.
3. Nếu game gốc vẫn văng, Verify/Repair và kiểm tra driver/cấu hình máy.
4. Nếu chỉ văng khi cài, gửi ảnh lỗi và danh sách file app báo đã cài.

## Bật C# nhưng không thấy dấu `*`

- Đóng hẳn tiến trình game rồi mở lại từ nút trên Trang chủ.
- Kiểm tra app đang trỏ đúng thư mục chứa `Client\Binaries\Win64\Client-Win64-Shipping.exe`.
- Với Steam, dán đúng `-ForceEnableCSharpEnvironment` vào Launch Options nếu mở game từ Steam.
- Tính năng này thuộc giai đoạn thử nghiệm và có thể thay đổi hoặc bị game ngừng hỗ trợ ở bản sau.

## Muốn trả game về nguyên bản

Tắt game → **Cài Việt hóa** → **Gỡ Việt hóa**. Sau đó dùng Verify/Repair nếu
trước đây từng cài thêm mod bằng công cụ khác.
