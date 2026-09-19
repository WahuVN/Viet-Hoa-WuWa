# Việt Hóa Wuthering Waves v2.0.2

Bản 2.0.2 cải thiện khả năng tự tìm thư mục cài đặt Wuthering Waves và tiếp tục hỗ trợ cập nhật trực tiếp trong ứng dụng.

## Thay đổi chính

- Tự tìm `Wuthering Waves Game` trên các ổ đĩa, kể cả đường dẫn dạng `D:\Game\Wuthering Waves Game`.
- Chỉ chấp nhận thư mục có Client thật: `Client\Binaries\Win64\Client-Win64-Shipping.exe`.
- Tự sửa khi người dùng chọn nhầm thư mục cha, `Client`, `Content\Paks` hoặc file EXE.
- Nhận diện đường dẫn từ game đang chạy, Registry, Steam và các thư mục cài phổ biến.
- Không nhận nhầm thư mục trùng tên nhưng thiếu Client hợp lệ.
- Sửa tạo font riêng: không còn lỗi `unexpected argument '-s'`; app tạo PAK V11, chuyển sang V12 và xác minh gói trước khi cài.
- Làm rõ: DirectX 12 dùng bình thường; chỉ chuyển DirectX 11 nếu gặp crash hoặc lỗi đồ họa.
- Tiếp tục tự kiểm tra, tải và xác minh cập nhật bằng SHA-256 trực tiếp từ GitHub.

## Tệp tải về

| Tệp | Mục đích |
| --- | --- |
| `VietHoa-WuWa-v2.0.2.zip` | Ứng dụng cài Việt hóa dành cho người chơi |
| `App-Dich-WuWa-v2.0.2.zip` | WAHU Community dành cho dịch giả và người đóng góp |
| `WuWaVH_HanViet_99_P.pak` | PAK Hán Việt để cài thủ công khi thật sự cần |

Ứng dụng lấy thông tin cập nhật và SHA-256 trực tiếp từ GitHub; không cần `update.json` hoặc `checksums.txt`.

Hỗ trợ: [Discord WAHU](https://discord.gg/tuRCj47sy) · [GitHub Issues](https://github.com/WahuVN/Viet-Hoa-WuWa/issues)
