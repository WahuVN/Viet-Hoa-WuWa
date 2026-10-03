# VHWuWa v3.0.11 — Bản vá xung đột mod

Bản vá dành cho người dùng PC, không thay đổi nội dung Việt hóa Wuthering Waves 3.7.0.

## Đã sửa

- Sửa lỗi bản VHWuWa cũ bị nhận nhầm là mod xung đột.
- Các file của VHWuWa cũ như `WuWaVH_99_P.pak`, `WahuFont_100_P.pak` và loader `version.dll` không còn bị đưa vào danh sách **Xóa mod ngoài** khi nhận diện đúng bộ cài VHWuWa cũ.
- Nút **Xóa xung đột** đổi thành **Xóa mod ngoài** để tránh hiểu nhầm.
- Khi có cả VHWuWa cũ và mod ngoài, ứng dụng chỉ xóa/cách ly mod ngoài.
- Có thể nâng cấp trực tiếp bản cài VHWuWa cũ chưa có thông tin quản lý mới; sau khi cập nhật, ứng dụng sẽ tạo lại thông tin quản lý chuẩn.
- Cập nhật nhanh cũng hỗ trợ bản cài VHWuWa cũ thay vì báo thiếu thông tin quản lý.
- Tránh tạo bản sao `version_goc.dll` sai từ chính loader VHWuWa cũ trong quá trình nâng cấp.

## Cập nhật

- Đang dùng **v3.0.9** hoặc **v3.0.10**: mở VHWuWa và kiểm tra cập nhật.
- Cài mới: tải `VietHoa-WuWa-v3.0.11.zip`, giải nén và mở `VHWuWa.exe`.

## Đã kiểm thử

- Toàn bộ kiểm thử: **108/108 đạt**.
- Test riêng luồng cài cũ/xóa mod: **24/24 đạt**.
- Khởi động `VHWuWa.exe`: đạt.
- Ứng dụng và trình cập nhật: `3.0.11.0`.

## Tải xuống

| Tệp | Dành cho |
| :--- | :--- |
| `VietHoa-WuWa-v3.0.11.zip` | Người chơi PC |
| `App-Dich-WuWa-v3.0.11.zip` | Dịch giả / người đóng góp |
| `WuWaVH_EN_99_P.pak` | Gói Việt hóa Tên Anh |
| `WuWaVH_HanViet_99_P.pak` | Gói Việt hóa Hán Việt |
| `VHWuWa-v3.0.11-SHA256SUMS.txt` | Mã kiểm tra toàn vẹn tệp |

## Hỗ trợ

- Discord Wuthering Waves VN: https://discord.gg/Gy5YQ84Yc2
- Báo lỗi / góp ý: https://github.com/WahuVN/Viet-Hoa-WuWa/issues
