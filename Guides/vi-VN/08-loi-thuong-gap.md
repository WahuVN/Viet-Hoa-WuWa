# Khắc phục lỗi

## Không tìm thấy game

Trong **Trang chủ**, bấm **Tự tìm game** hoặc **Chọn thư mục**. Chọn thư mục game có `Client` rồi bấm **Kiểm tra**. Không chọn nhầm ứng dụng launcher.

## Không cài được Việt hóa

Đóng game và kiểm tra đường dẫn trong ứng dụng. R29 dùng SIG có sẵn của game: trước tiên kiểm tra nguồn `Lang_en` như các bản cũ; khi không có nguồn này, chỉ chấp nhận SIG ngôn ngữ khác nếu `MountLang_*` chính thức tham chiếu đúng tệp và SHA-1. **Không cần chuyển game sang tiếng Nhật, không phải tự tạo SIG.** Nếu không xác minh được, ứng dụng dừng trước khi ghi; dùng **Verify/Repair** trong launcher và kiểm tra lại thư mục game. Không tự chép hoặc xóa tệp game gốc.

## Vẫn còn tiếng Anh sau khi cài

- Trong game đặt **Text Language = English**.
- Thoát game và vào app kiểm tra trạng thái Việt hóa.
- Một số nội dung game mới có thể chưa được dịch; gửi ảnh câu còn thiếu ở kênh báo lỗi.

## Không thấy thông báo cập nhật

- Kiểm tra mạng và bật **Cài đặt → Tự kiểm tra cập nhật**.
- Bấm **Kiểm tra cập nhật** để thử thủ công.
- Bản mới phải được đăng **công khai trên GitHub Releases**; bản Draft chưa được app phát hiện.
- Không cần Setup.exe. Người đang dùng ZIP portable cập nhật trong thư mục đang chạy.

## Chữ thiếu dấu hoặc ô vuông

Mặc định bản Việt hóa cài **Lexend 660**. Nếu chữ thiếu dấu hoặc ô vuông, mở tab **Font game** để kiểm tra Lexend 660 hoặc thử font khác; phần xem trước không tự đổi font trong game cho đến khi bấm **Áp dụng**.

## Game lỗi hoặc thoát bất thường

1. Đóng game, gỡ Việt hóa bằng ứng dụng và thử mở game gốc.
2. Nếu vừa cài mod khác, kiểm tra và gỡ mod bằng đúng công cụ của mod đó.
3. Nếu game gốc vẫn lỗi, dùng **Verify/Repair** của launcher; kiểm tra driver khi có lỗi GPU.

## Liên hệ hỗ trợ

Vào **Trợ giúp → Hỗ trợ Discord**, chọn kênh **hỗ-trợ-cài-đặt** hoặc **báo-lỗi-bản-dịch**. Khi báo lỗi, ghi phiên bản app/game, gửi ảnh và nhật ký tại **Cài đặt → Mở thư mục nhật ký**.

> Che UID, email, tên đăng nhập và thông tin riêng trước khi gửi.
