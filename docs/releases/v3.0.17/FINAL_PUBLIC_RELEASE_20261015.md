# Trạng thái phát hành WuWa Việt Hoá — 15/10/2026

## PC v3.0.17 (Stable / Public)

- GitHub: https://github.com/WahuVN/Viet-Hoa-WuWa/releases/tag/v3.0.17
- ZIP: `VietHoa-WuWa-v3.0.17.zip`
- ZIP SHA-256: `4b14a12ba7ed3d16e56912555a88c5a54f9f66c477a35e91af9598af186bf055`
- Giao diện gọn; Lexend 650; 73 font; hai PAK EN và Hán Việt (12 key Qingxiao – Rover được cập nhật).
- Sửa tooltip dung lượng cũ và thông báo cập nhật nhanh: chỉ báo 'đã áp dụng vào game' sau khi thực hiện cài thành công.
- 570 mục ZIP không trùng tên; kiểm tra toàn vẹn bằng 7-Zip PASS.
- PC 3.0.16 có luồng tự cập nhật GitHub đã kiểm thử ở vòng RC3; RC4 sửa nội dung thông báo và đã đóng lại.
- Người dùng yêu cầu không chạy nghiệm thu trong game thật: **không khẳng định gameplay/font/runtime PASS**.

## Android v0.2.0-alpha.18 (Pre-release / Public)

- GitHub: https://github.com/WahuVN/VHWuWa-Mobile/releases/tag/v0.2.0-alpha.18
- APK: `VHWuWa-Android-0.2.0-alpha.18-3.7.0-R6.apk`
- APK SHA-256: `c5115cd5edc029fa15fb171a0a08ebb85baab494412ac7247427c7f9f445ea12`
- Android 10+ và Shizuku; versionCode 18, versionName 0.2.0-alpha.18; source content 3.7.0-R6.
- Release build SUCCESS khi loại riêng các tác vụ Android Lint vì Gradle offline thiếu `material-icons-extended-desktop:1.7.8`. **Lint đầy đủ chưa PASS.**
- APK đã zipalign, ký theo certificate SHA-256 `1ae6ddca530ffe6755f5a37aa2e9cc74480c6b071686b6b852a586ad797e445b` giống bản alpha.14 công khai; apksigner verify PASS.
- Hai PAK EN và Hán Việt trong APK khớp chính xác SHA với source; 562 mục không trùng.
- **Chưa kiểm tra cài đặt và hiển thị trong game Android thực tế** theo yêu cầu bỏ qua runtime test.

## Discord

- 7 kênh ghim đã cập nhật link phiên bản mới; FAQ dùng đường dẫn `~WuWaMods`, không hướng dẫn xoá cả thư mục.
- Thông báo phát hành tại `https://discord.com/channels/1539838668154470400/1540320036290428969/1560214390886633576`; không `@everyone`.
- `#nhật-ký-bản-dịch` chỉ đọc, đăng 12 mục QA không ping; lưu ý không nhầm bản QA và bản public trước đó.
- BotDis GitHub main commit `c886595`, đã đồng bộ cấu hình PC 3.0.17 / Android alpha.18.
- Link App-Dich-WuWa được ghim về bản 3.0.16 đang có thật, không tạo URL phiên bản 3.0.17 không tồn tại.

Không thay đổi/cài đặt trực tiếp game của người dùng. Các kiểm tra runtime bỏ qua theo chỉ thị, không được coi là PASS.
