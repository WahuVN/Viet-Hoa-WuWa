# VHWuWa v3.0.13 — Bản vá thiếu text 3.7

Bản vá dành cho người dùng Wuthering Waves 3.7.0 trên PC.

## Đã sửa

- Bổ sung **6.693 dòng dữ liệu 3.7** bị thiếu trong PAK trước đó.
- Sửa các trường hợp game hiện raw key như `Main_Mengzhou_3_7_29_2` thay vì câu tiếng Việt.
- Đồng bộ text trùng giữa `lang_multi_text.db` và `lang_multi_text_1sthalf.db` để các màn dùng lookup khác nhau vẫn hiện đúng.
- Bổ sung các row còn thiếu ở speaker, hot-patch và occupation DB.
- Giữ nguyên toàn bộ 13.681 dòng Việt hóa authority hiện có.
- Kiểm fresh-unpack sau đóng PAK: official row thiếu còn lại = **0**.
- Giữ toàn bộ sửa lỗi của v3.0.12: cài lại luôn ghi đè PAK/font/loader do VHWuWa quản lý; nếu Windows chặn ghi/xóa hoặc file đang bị khóa thì ứng dụng báo rõ cần đóng game hoặc chạy bằng quyền Administrator.
- Giữ sửa lỗi của v3.0.11: không nhận nhầm file VHWuWa cũ là mod ngoài.

## Kiểm tra dữ liệu

- PAK Tên Anh: `8114f9e79aba08ff3cc3ef1a6bdc1499782725b0547627c06856eb14e31f4725`
- PAK Hán Việt: `ce8ec3b1cc7dbbb6ccc7a4fe330fd20e598f249729dc3d2e554ab8f9d49ef061`
- Key `Main_Mengzhou_3_7_29_2` đọc ra cùng bản dịch tiếng Việt ở cả hai MultiText DB.

## Cập nhật

- Đang dùng **v3.0.9 / v3.0.10 / v3.0.11 / v3.0.12**: mở VHWuWa và kiểm tra cập nhật.
- Cài mới: tải `VietHoa-WuWa-v3.0.13.zip`, giải nén và mở `VHWuWa.exe`.

## Hỗ trợ

- Discord Wuthering Waves VN: https://discord.gg/Gy5YQ84Yc2
- Báo lỗi / góp ý: https://github.com/WahuVN/Viet-Hoa-WuWa/issues
