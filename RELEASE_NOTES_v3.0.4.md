# Việt Hóa Wuthering Waves v3.0.4

Bản 3.0.4 tập trung rà soát dữ liệu, đồng bộ quy chuẩn tên và sửa các lỗi có thể ảnh hưởng trực tiếp đến nội dung hiển thị trong game.

## Thay đổi chính

- Kiểm tra 11.788 key thuộc các mốc game 3.4, 3.5 và 3.6.
- Xác nhận 4.845 bản dịch trùng hoặc tương đương với dữ liệu hiện tại.
- Bổ sung 5 mô tả thuộc tính đơn giản đã được đối chiếu nội dung.
- Không nhập 328 key còn trống, 107 key còn chữ Trung, 18 key còn nguyên tiếng Anh, 66 key sai placeholder và 834 key sai thẻ định dạng.
- Giữ lại 5.585 key khác biệt để kiểm tra ngữ cảnh thủ công, tránh làm hỏng thuật ngữ, tên riêng hoặc ý nghĩa kỹ năng.
- PAK mới đã được build và kiểm tra; số dòng thiếu chắc chắn giảm từ 12.935 xuống 8.203.
- Áp dụng MASTER V5.1 thống nhất cho bản Tên Anh và Hán Việt.
- Sửa 603 vị trí đã kiểm tra trong 342 key, gồm tên cũ, lỗi viết hoa, câu trống, câu fallback sai nội dung, thẻ định dạng, placeholder và số phần trăm.
- Bản Tên Anh dùng `Chixia`, `Fuling`, `Black Shores` và `Vùng Huyền Phương`.
- Bản Hán Việt dùng `Sí Hà`, `Phục Linh`, `Hắc Hải Ngạn` và `Vùng Huyền Phương`.
- Chuẩn hóa thêm tên địa danh, tổ chức và cách viết như `Thành Huyền Phương`, `Huyền Phương Thành`, `Viện Nghiên cứu Hoa Tư`, `Học viện Startorch`.
- Tăng kiểm tra sau khi gộp để dữ liệu cũ không tự ghi đè quy chuẩn mới.
- Ứng dụng tải gói Hán Việt trực tiếp từ release mới nhất.
- Kiểm tra 1.147 key của Part 10, nhập 915 bản dịch mới/đã chỉnh và xác nhận lại 226 bản dịch đã đúng.
- Chuẩn hóa 35 block bị lặp trường VI/HV; chỉ giữ ngoài PAK 6 nhãn nội bộ `dnt/` không có nguồn EN.
- Chỉnh lại 8 câu thoại của Rebecca trong nội dung 3.4, thống nhất giọng nhân vật và cách xưng hô `tao/mày`.

## Tệp phát hành

| Tệp | Mục đích |
| --- | --- |
| `VietHoa-WuWa-v3.0.4.zip` | Ứng dụng cài Việt hóa dành cho người chơi |
| `App-Dich-WuWa-v3.0.4.zip` | WAHU Community dành cho dịch giả và người đóng góp |
| `WuWaVH_HanViet_99_P.pak` | PAK Hán Việt để cài thủ công khi cần |

Hỗ trợ: [Discord WAHU](https://discord.gg/tuRCj47sy) · [GitHub Issues](https://github.com/WahuVN/Viet-Hoa-WuWa/issues)
