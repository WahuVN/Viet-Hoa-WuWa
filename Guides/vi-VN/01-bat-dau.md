# Bắt đầu nhanh

## Chuẩn bị

1. Tải `VietHoa-WuWa-v3.0.4.zip` ở trang Releases.
2. Giải nén toàn bộ ZIP ra thư mục riêng; không chạy EXE trực tiếp bên trong ZIP.
3. Tắt Wuthering Waves và các launcher đang cập nhật game.
4. Chạy `Chay VHWuWa.bat`.

## Cài trong ba bước

1. Ở **Trang chủ**, bấm **Tự tìm game**. Không tìm thấy thì dùng **Chọn thư mục**.
2. Vào **Cài Việt hóa**, chọn **Tên Anh** hoặc **Hán Việt**, giữ font tiếng Việt
   rồi bấm **Cài Việt hóa**.
3. Chờ app báo đã sao chép và kiểm tra đủ file; sau đó mở game, đặt ngôn ngữ chữ
   là **English**. Có thể dùng DirectX 11 hoặc 12; chỉ chuyển sang DX11 nếu DX12 gặp lỗi.

## Mở game trực tiếp và môi trường C# thử nghiệm

Ở **Trang chủ**, mục **Khởi chạy game** có nút mở thẳng
`Client\Binaries\Win64\Client-Win64-Shipping.exe` thay vì launcher.

- Giữ công tắc C# tắt để mở game bình thường.
- Bật công tắc để truyền `-ForceEnableCSharpEnvironment`. Khi hoạt động, cuối phiên bản
  hiển thị trong game có dấu `*`.
- Lần đầu có thể tải thêm dữ liệu. Đây là chế độ thử nghiệm, không bảo đảm tăng FPS trên
  mọi máy; nếu giật, crash hoặc lỗi, hãy tắt công tắc rồi mở lại.
- Với Steam, dùng **Chép tham số cho Steam** rồi tự dán vào **Properties → Launch Options**.
  App không tự sửa cấu hình Steam.

Muốn gỡ: tắt game → **Cài Việt hóa** → **Gỡ Việt hóa**.

> ### ⚠️ Cảnh báo an toàn tài khoản & Anti-cheat
> Bản Việt hóa và công cụ tùy biến font/mod là nội dung không chính thức do cộng đồng phát triển, không thuộc Kuro Games. Việc can thiệp vào tệp game **vẫn có khả năng bị hệ thống chống gian lận (Anti-cheat) quét và khóa tài khoản**. Người dùng vui lòng tự cân nhắc và tự chịu trách nhiệm khi sử dụng, nên thử nghiệm trước trên **tài khoản phụ (clone)**.

