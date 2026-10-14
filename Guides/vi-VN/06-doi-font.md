# Font chữ

## Dùng font trong thư viện

1. Mở tab **Font chữ**.
2. Tìm và chọn font trong danh sách.
3. Kiểm tra ở khung **Xem trước**; dùng thanh cỡ chữ hoặc **Xem lớn**.
4. Bấm **Áp dụng**.

## Chỉnh độ đậm (Weight)

- **Lexend Variable** hỗ trợ chỉnh chính xác từ 100–900, mặc định đề xuất **650**.
- Các font **Variable** khác chỉ chỉnh trong khoảng mà chính font hỗ trợ.
- Font **tĩnh** sẽ khóa thanh Weight; chọn biến thể Regular / SemiBold / Bold sẵn có trong thư viện.
- Thanh Weight không chỉ đổi ảnh xem trước: khi bấm áp dụng, app xuất một bản font tĩnh đúng weight rồi đóng gói PAK.
- Bản phát hành có sẵn công cụ tạo font tĩnh độc lập, không cần người chơi cài Python. Nếu chạy từ source chưa build công cụ, có thể dùng Python 3 và `fontTools` (`py -m pip install fonttools`).
- Trước khi áp dụng phải tắt game; nếu lỗi, chọn **Gỡ font** để khôi phục.

## Dùng font ngoài

1. Bấm **Chọn font ngoài** và chọn `.ttf`, `.otf` hoặc `.ttc`.
2. Kiểm tra dấu tiếng Việt ở nhiều cỡ chữ.
3. Bấm **Cài font ngoài** nếu hiển thị đúng.

## Khôi phục

Dùng **Gỡ font** để bỏ font tùy chỉnh hoặc **Khôi phục mặc định** để trở về font chuẩn của bộ Việt hóa.

> Nếu chữ thành ô vuông hoặc mất dấu, font đó không hỗ trợ đầy đủ Unicode tiếng Việt. Hãy chọn font khác trong thư viện.
