# Chuẩn bị thông báo ứng dụng / GitHub / Discord — PC v3.0.19 + R29

## Phạm vi phát hành
- **Ứng dụng:** WuWa Việt Hóa PC v3.0.19, hình thức ZIP portable, tự cập nhật khi người dùng đồng ý.
- **PAK:** R29, resource 3.8.11, SHA-256 `38527e34ffeb8ef7538206d3d4dbd5cbac54a1e6cd9d0957501035ff41a8e598`; chỉ cài khi fingerprint game tương thích.
- **Giới hạn:** R29 chỉ Tên Anh; không hỗ trợ Hán Việt/font PAK rời. Không có Android mới trong đợt PC này.
- **GitHub ZIP Draft:** 176.083.488 byte, SHA-256 `7e1b4a07399d548b3aea1e881442b1909e69d5872d637e873d17c21dfa27cabb`.
- **Public/latest:** còn là v3.0.18; vì vậy máy người dùng 3.0.18 chưa nhận thông báo v3.0.19.

## Ứng dụng
Mã hiện tại tự gọi GitHub `releases/latest` khi khởi động nếu tùy chọn AutoCheckUpdate bật. App dùng trực tiếp nội dung `body` của GitHub Release làm ghi chú trong hộp thoại hỏi Có/Không và trang Cài đặt. Nội dung GitHub Draft nằm ở `GITHUB_RELEASE_BODY_V319_R29.md` đã rút gọn để dễ đọc trên app. Không thay đổi logic updater đang qua QA.

## GitHub
GitHub Release v3.0.19 đang ở **Draft** với ZIP + SHA256SUMS.txt. Chỉ chỉnh nội dung body bản nháp; **không bấm Publish** khi chưa hoàn tất QA ứng viên trên máy game thật. Khi Publish, mới kiểm tra `releases/latest=v3.0.19`, tải ZIP và đối chiếu SHA lại từ ngoài.

## Discord (BotDis)
- Source bot đang chạy: `D:\TOOL\Bot Dis\WuWaVHBot`.
- Authority production đang dùng: PC 3.0.18, Android 0.2.0-alpha.18 / 3.7.0-R6.
- Bộ staging R29 ngày 15/11 nằm tại `release-prep\v3.0.19-r29-20261115` (manifest preview, 7 kênh, announcement, hỗ trợ, nhật ký và script QA).
- Đã chạy `npm run check`: PASS và `npm run release:dry-run`: PASS với **7/7** thẻ kênh tìm thấy, 1 kênh thông báo, không gửi/sửa.
- Preview R29: mã template `serverSetup.PREVIEW.js` và `post-announcement.PREVIEW.js` được tạo trong staging, kiểm tra cú pháp và render 7 embed **PASS**. Các thay đổi production được xuất ra hai tệp `*.R29_ON_GO.patch` để review sau khi public.
- Bot production hiện hardcode câu PC có Hán Việt và thông báo font; **không được áp dụng manifest 3.0.19 mà chưa áp dụng và QA template R29**.
- Không thay đổi `.env`, `serverSetup.js`, `post-announcement.js` hoặc `release-manifest.json` production khi còn Draft; không gửi thông báo hoặc ping.

## Kênh và ý nghĩa
- `📢・thông-báo`: một tin chính thức sau public, không mention role.
- `📥・tải-việt-hóa`: link ZIP thật đã public và Android vẫn là bản cũ.
- `🆕・cập-nhật-việt-hóa`: changelog version và link, tránh đăng trùng announcement.
- `❔・câu-hỏi-thường-gặp`: ZIP portable, tự update, giới hạn R29.
- `🆘・hỗ-trợ-cài-đặt`: lỗi nhận game, cập nhật, log, SmartScreen.
- `⚠️・báo-lỗi-bản-dịch`, `💡・góp-ý`, `✍️・tham-gia-dịch`: mẫu báo lỗi, góp ý và hướng dẫn.
- `📖・nhật-ký-bản-dịch`: chỉ đăng ví dụ Trước → Sau khi có key/QA thật; không bịa câu thay đổi.

## Các bước phải hoàn tất trước Publish
1. Test bộ ZIP v3.0.19 ứng viên trên máy game thực, cài/gỡ R29, giao diện/độ phủ tối thiểu; bảo đảm không phá bản 3.0.20 đang dùng trên MAY-007.
2. Xác minh PAK PC mới nhất và digest của Draft vẫn là R29; nếu PAK mới hơn xuất hiện, tạo ZIP, sha và thông báo mới, không tái sử dụng receipt cũ.
3. Chỉ khi release public: chạy thử prompt từ app 3.0.18 tới GitHub v3.0.19; kiểm SHA/rollback/dữ liệu.
4. Sau GitHub: sao lưu bot production, áp dụng preview manifest và template chính xác, `npm run check` + Discord dry-run; đồng bộ 7 kênh; gửi một announcement; kiểm slash commands.

**Tình trạng:** PREP/DRAFT; chưa phát hành, chưa gửi Discord, chưa sửa production bot.
