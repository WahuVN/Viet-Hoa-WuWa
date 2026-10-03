# VHWuWa v3.0.12 — Bản vá thiếu text 3.7 và ghi đè khi cài

Bản vá dành cho người dùng PC đang dùng Wuthering Waves 3.7.0.

## Đã sửa

- Bổ sung **6.693 dòng dữ liệu 3.7** bị thiếu trong PAK trước đó.
- Sửa các trường hợp game hiện raw key như `Main_Mengzhou_3_7_29_2` thay vì câu Việt.
- Đồng bộ các text trùng giữa `lang_multi_text.db` và `lang_multi_text_1sthalf.db` để các màn dùng lookup khác nhau vẫn hiện đúng tiếng Việt.
- Bổ sung các row còn thiếu ở speaker, hot-patch và occupation DB.
- Giữ nguyên 13.681 dòng Việt hóa authority và kiểm readback sau đóng PAK.
- Cài lại VHWuWa giờ **luôn ghi đè** PAK/font/loader do VHWuWa quản lý.
- File cũ có thuộc tính ReadOnly sẽ được bỏ ReadOnly rồi ghi đè.
- Nếu Windows không cho xóa/ghi đè vì thiếu quyền hoặc file đang bị game/launcher khóa, ứng dụng sẽ dừng cài và báo rõ cần đóng game hoặc chạy VHWuWa bằng quyền Administrator.
- Giữ bản vá v3.0.11: không nhận nhầm file VHWuWa cũ là mod ngoài.

## Kiểm tra dữ liệu

- Official row 3.7 còn thiếu sau rebuild: **0**.
- PAK EN mới: `8114f9e79aba08ff3cc3ef1a6bdc1499782725b0547627c06856eb14e31f4725`.
- PAK Hán Việt mới: `ce8ec3b1cc7dbbb6ccc7a4fe330fd20e598f249729dc3d2e554ab8f9d49ef061`.
- Key `Main_Mengzhou_3_7_29_2` đọc ra cùng bản dịch tiếng Việt ở cả hai MultiText DB.

## Cập nhật

- Đang dùng **v3.0.9 / v3.0.10 / v3.0.11**: mở VHWuWa và kiểm tra cập nhật.
- Cài mới: tải `VietHoa-WuWa-v3.0.12.zip`, giải nén và mở `VHWuWa.exe`.

## Hỗ trợ

- Discord Wuthering Waves VN: https://discord.gg/Gy5YQ84Yc2
- Báo lỗi / góp ý: https://github.com/WahuVN/Viet-Hoa-WuWa/issues
