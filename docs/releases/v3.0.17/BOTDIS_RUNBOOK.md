# Runbook BotDis — chuẩn bị PC v3.0.17 (KHÔNG GỬI)

**Bot production:** `D:\TOOL\Bot Dis\WuWaVHBot`.
**Khu staging:** `D:\TOOL\Bot Dis\WuWaVHBot\release-prep\v3.0.17`.
**Kênh về sau:** `📢・thông-báo`.
**Trạng thái:** chỉ lưu tài liệu, không cập nhật production manifest, không đồng bộ 7 kênh, không gửi tin.

## Authority hiện hành
- Bot lấy version, link từ `release-manifest.json` thông qua `serverSetup.js`.
- Tại lần đọc: PC `3.0.15`, Mobile `0.2.0-alpha.13`, content mobile `3.7.0-R4`.
- `npm run check` PASS theo authority đang hoạt động, **không phải** QA cho v3.0.17.
- `post-announcement.js` hiện hardcode nhiều mô tả về text 3.7 và Android; khi public PC mới cần sửa **có review** để không quảng bá nhầm nội dung hoặc tự nhận Mobile cùng update.
- Bot có chế độ dry-run, release:sync và release:announce bị khóa nếu chưa có cờ confirm; đừng bật những cờ đó ở giai đoạn này.

## Nội dung PC v3.0.17 cần xuất hiện trong embed về sau
- Các nút Xóa / Cách ly / Mở bản sao / Quét lại, warning UI và số file tự xóa.
- Backup bảo vệ (`.bak`/`.backup`/`.orig`/`version_goc.dll`), kiểm file khóa/marker SHA trước xóa.
- Lexend Variable mặc định 650, slider 100–900, 73 font giữ 72 font cũ, font tĩnh khóa.
- **Cảnh báo quan trọng:** AUTO CLEAN là **xóa vĩnh viễn** mod đã nhận diện; phải chọn Cách ly riêng nếu cần backup.
- Chỉ công bố sửa thoại/text khi có bản EN/Hán Việt thực sự khác v3.0.16, đã QA.
- Nếu Mobile không phát hành mới, tách thành dòng “Android sử dụng bản Mobile hiện có”, không nói có cập nhật Mobile.

## Manifest khi có lệnh GO (không làm bây giờ)
1. Build chính thức `v3.0.17`; lấy tên asset đúng và SHA-256 từ file thực tế.
2. Tạo release GitHub public và đảm bảo `gh release view` cho đúng asset + digest. Không tạo SHA giả.
3. Sao lưu manifest production rồi thay đúng **block PC**; giữ block Mobile nếu không cập nhật Mobile.
4. Thay nội dung embed sau review; cắt ngắn nếu vượt giới hạn Discord: embed title <=256, description <=4096 và tổng fields <=6000, không vượt 10 embed.
5. Chạy `npm run check` và `CHECK_RELEASE.bat` (không `-Apply`) để xác minh GitHub digest và Discord dry-run.
6. Soát 7 kênh: tải-việt-hóa, FAQ, cập-nhật-việt-hóa, hỗ-trợ-cài-đặt, báo-lỗi-bản-dịch, góp-ý, tham-gia-dịch; tránh edit message không đúng version.
7. **Chỉ khi có GO gửi Discord riêng:** đồng bộ rồi gửi duy nhất một announcement, chống duplicate. Không mention: `allowedMentions: { parse: [], users: [], roles: [] }`.
8. Hậu kiểm slash commands `/download`, `/guide`, `/faq`, `/bao-loi`, `/gop-y`, bot health và 7 card.

## TUYỆT ĐỐI KHÔNG Ở GIAI ĐOẠN PREP
- Không chạy `GO_LIVE_RELEASE.bat` hoặc `DEPLOY_RELEASE.ps1 -Apply`.
- Không dùng `npm run setup` (thay đổi roles/channels), không set `CONFIRM_SEND_RELEASE=YES`, `CONFIRM_CHANNEL_UPDATE_RELEASE=YES`.
- Không chỉnh `release-manifest.json`, `post-announcement.js`, `serverSetup.js` hoặc `.env` production; không gửi ping/tag.
- Không đưa token/webhook hay ID bí mật vào tài liệu. Chỉ có template và checklist.

**Tham chiếu:** `SOURCE_CHANGE_MATRIX.md`, `RELEASE_NOTES.md`, `DISCORD_ANNOUNCEMENT.md` cùng thư mục source.
