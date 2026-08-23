# Mod khác và xung đột

VHWuWa không tự ý quản lý hoặc xóa mod ngoài. Ứng dụng chỉ quản lý các file Việt hóa và font do chính VHWuWa cài.

## Quét trước khi cài

1. Mở tab **Cài Việt hóa**.
2. Xem bảng **Kiểm tra xung đột mod**.
3. Bấm **Quét lại mod** sau khi thêm, tắt hoặc gỡ một mod khác.

Ứng dụng kiểm tra PAK trong các thư mục mod, proxy loader như `version.dll`, `dxgi.dll`, `dinput8.dll` và các bộ nạp mod phổ biến. Khi có xung đột, nút cài Việt hóa sẽ bị khóa và đường dẫn cần xử lý được hiển thị rõ.

## Khi app báo xung đột

- Gỡ hoặc tắt mod bằng đúng công cụ đã dùng để cài mod đó.
- Không xóa bừa file trong thư mục game.
- Bấm **Quét lại mod**. Chỉ cài khi bảng báo không còn xung đột.

## Gỡ Việt hóa an toàn

Vào **Cài Việt hóa** → **Gỡ Việt hóa**. VHWuWa lưu tên và SHA-256 của từng file do app quản lý:

- File mod ngoài nằm cạnh Việt hóa vẫn được giữ nguyên.
- File có tên dạng `*_100_P.pak` không còn bị nhận nhầm là font của app.
- Nếu mod khác đã ghi đè PAK/font/loader của VHWuWa, app sẽ dừng gỡ và báo đúng file bị thay đổi thay vì xóa nhầm.
- Với gói `.vhwpack`, các mod ghi chung đường dẫn sẽ bị chặn; hãy gỡ mod cài sau trước.

Sau khi xử lý mod được báo, bấm **Quét lại mod** rồi thử gỡ lại.

