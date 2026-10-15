# Kiểm tra thông báo cập nhật — WuWa Việt Hoá v3.0.17 (15/10/2026)

## Kết quả

Trạng thái: **Thông báo GitHub Draft đã chỉnh; tự cập nhật QA PASS; chưa Publish.** Không đồng nghĩa bản dịch/font trong game thật đã PASS.

- GitHub Draft `v3.0.17` vẫn `isDraft=true`, `isPrerelease=false`, đủ 3 asset đúng SHA-256.
- GitHub public `/releases/latest` vẫn là `v3.0.16`. Người dùng bản 16 chưa bị nhắc cập nhật sớm.
- `V316ToV317GitHubUpdateTests`: 4/4 PASS chạy lại trên WIN3 ngày 15/10.
- `scripts/verify-v316-v317-sync-20261015.ps1`: `STAGED_DRAFT_SYNCED_WAITING_FOR_PUBLISH`.
- `scripts/verify-github-v317-published.ps1 -BeforePublish`: PASS.
- Nội dung `body` GitHub Draft đã chuyển sang văn bản tự nhiên 768 ký tự, không có nhãn nội bộ RC3/SHA hay checklist kỹ thuật dài. Bản v3.0.16 lấy nguyên `body` GitHub làm ghi chú hộp thoại Windows, nên rút ngắn trực tiếp ở nguồn là cần thiết.
- Đã nhắc rõ “Nhanh” chỉ cập nhật bản dịch, “Đầy đủ” cập nhật cả ứng dụng; cách cập nhật sau, bảo vệ mod cũ và giới hạn nghiệm thu trong game.
- **Không sửa/đăng lại** ZIP, PAK; **không Publish**, không gửi Discord/BotDis.

Tài liệu nội dung đang dùng: `NOTIFICATION_BODY_v317_USER_FRIENDLY.md`.
Bản sao ghi chú cũ để truy vết: `GITHUB_DRAFT_BODY_BEFORE_UX_20261015.md`.

## Các chỗ trong app cần sửa trước khi coi UX hoàn thiện 100%

1. `MainWindow.xaml` sidebar ghi `Cập nhật nhanh tệp Việt hóa (~60MB)` và `Cập nhật toàn bộ ứng dụng (~190MB)`. Thực tế gói ZIP v3.0.17 là **294,586,523 byte (~281 MiB)**; hai PAK tải riêng có tổng kích thước **135,754,979 byte (~129 MiB)**. Không được tuyên bố tooltip hiện tại chính xác. Phải thay bằng mô tả công dụng không phụ thuộc kích thước hoặc build/publish gói có kích thước tương ứng.
2. `MainViewModel.ApplyDeltaPakUpdateAsync` hiện thông báo đã nạp bản dịch “vào ... thư mục game của bạn” kể cả trường hợp app không có thư mục game hoặc game chưa cài Việt hóa. Cần phân biệt: chỉ lưu PAK trong app, hay đã áp dụng vào game, và nhắc “cập nhật đầy đủ” khi còn EXE mới.
3. Popup phiên bản 3.0.16 vẫn hiển thị `Ghi chú:\n` + nguyên văn GitHub release `body` (không render Markdown). Ghi chú mới đã dùng câu ngắn và danh sách bằng dấu gạch đầu dòng; **chưa thử trực tiếp popup hiện lên từ GitHub stable v3.0.17** vì vẫn Draft.
4. Font/thoại in-game PC DX11/DX12 chưa nghiệm thu. Đây là chặn phát hành theo checklist QA, không phải lỗi hệ thống thông báo.

Các điểm 1–2 nằm trong binary hiện đã đóng ZIP RC3. Muốn sửa phải **build ra gói ứng viên mới, chạy lại QA/updater và đồng bộ SHA của asset GitHub Draft**. Không sửa bằng cách thay tài liệu thôi và không tự nhận đã có trong bản RC3.

## Đánh giá

- Tính logic nhận bản update từ v3.0.16: **Đạt QA offline / Mock; chưa có phép thử public post-publish**.
- Lời giới thiệu trong thông báo GitHub Draft: **Đã chỉnh, ngắn gọn, đúng tình trạng thực**.
- Thông báo/tip **bên trong binary RC3**: **Chưa đạt 100%** do các chỗ nêu trên.
- Phát hành GitHub Stable/Latest và Discord: **HOLD**.
