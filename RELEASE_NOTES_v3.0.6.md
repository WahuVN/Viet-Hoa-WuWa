# Việt Hóa Wuthering Waves v3.0.6

Bản v3.0.6 tập trung hoàn thiện dữ liệu Việt hóa, bổ sung phụ đề nhạc còn thiếu và nâng cấp công cụ chỉnh tên/thuật ngữ trong WAHU Community.

## Thay đổi chính

- Rà soát lại 12.935 record thuộc 10 part trên các mốc game 3.4, 3.5 và 3.6 bằng đúng DB, bảng và key.
- Đồng bộ quy chuẩn tên nhân vật, địa danh, tổ chức, boss và thuật ngữ cho hai bản Tên Anh/Hán Việt.
- Hoàn thiện **186 phụ đề Vocal OST** ở cả hai biến thể; giữ đúng lời gốc, `{KeepOrigin}` và thẻ định dạng của game.
- Tăng kiểm tra placeholder, tag, biến giới tính và thuật ngữ trước khi đưa câu dịch vào PAK, hạn chế lỗi hiển thị hoặc câu tự động sai ngữ cảnh.
- Hoàn thiện bảng **Đặt tên nhân vật** với tên mặc định, tên Tiếng Việt và tên Hán Việt; có thể sửa trực tiếp rồi tạo lại PAK.
- Thêm tab **Thuật ngữ** theo nhóm, hiển thị CN/EN/VI/HV, hỗ trợ sao chép tiếng Anh và lưu theo đúng key.
- Bổ sung kiểm tra đồng bộ thuật ngữ và tên boss theo bộ quy tắc hiện hành.
- Sửa bước đóng PAK để luôn ẩn nhãn và giá trị UID ở cả bản Tên Anh/Hán Việt, kể cả sau khi đổi font hoặc tạo lại PAK.
- Tinh gọn giao diện, thông báo, phần xem trước font và hướng dẫn sử dụng.
- Hai PAK mới đã được đóng gói V12 và hậu kiểm thành công trên 97 cơ sở dữ liệu với 351.866 ô nội dung.

## Cập nhật

- Người đang dùng bản cũ có thể mở ứng dụng để kiểm tra và cài bản mới khi GitHub Release v3.0.6 được phát hành.
- Người cài mới chỉ cần tải gói dành cho người chơi, giải nén đầy đủ rồi mở `VHWuWa.exe`.
- Tắt hoàn toàn game trước khi cài, gỡ hoặc chuyển đổi bản Việt hóa.

## Tệp phát hành

| Tệp | Mục đích |
| --- | --- |
| `VietHoa-WuWa-v3.0.6.zip` | Ứng dụng cài Việt hóa dành cho người chơi |
| `App-Dich-WuWa-v3.0.6.zip` | WAHU Community dành cho dịch giả và người đóng góp |
| `WuWaVH_EN_99_P.pak` | PAK Tên Anh để tải hoặc khôi phục riêng khi cần |
| `WuWaVH_HanViet_99_P.pak` | PAK Hán Việt để tải/cài riêng khi cần |

Không cần `update.json` hoặc `checksums.txt`; ứng dụng đọc phiên bản và thông tin gói cập nhật trực tiếp từ GitHub Release.

Hỗ trợ: [Discord WAHU](https://discord.gg/tuRCj47sy) · [GitHub Issues](https://github.com/WahuVN/Viet-Hoa-WuWa/issues)
