# VHWuWa v3.0.9

Nếu đang dùng 3.0.8 thì 3.0.9 không phải bản chỉ sửa app. Mình đã quay lại rà cả PAK dịch, sửa những câu nghe cứng, xưng hô lệch, text bị rơi về English/test và một số thuật ngữ còn chưa đồng bộ.

## Từ 3.0.8 lên 3.0.9

So với PAK EN public ở 3.0.8:

- **17.445 text đang có bị thay đổi**;
- thêm **112 text mới**;
- bỏ **39 text** theo source mới;
- tổng cộng **17.596 triple dữ liệu khác nhau** giữa hai bản.

17.445 không có nghĩa là mình ngồi sửa tay 17 nghìn câu. Con số đó gồm đồng bộ source, thuật ngữ và các đợt cleanup hàng loạt. Riêng phần mình rà lại từng câu theo ngữ cảnh có **43 câu trước → sau đã kiểm tra và xác nhận nằm trong PAK cuối**.

Ngoài ra mình đã dọn:

- **270 text kỹ năng** từng bị source dev `test/test` ghi đè;
- **35 text cốt truyện/hảo cảm** bị rơi về English;
- **7 text Hổ Khẩu** bị exact override cũ co sai nội dung đã được khôi phục; trong đó có 4 text nhiệm vụ từng chỉ còn mỗi tên địa danh;
- 1 đoạn `test/` bị lọt vào hướng dẫn;
- 1 lựa chọn Rogue bị thiếu token, giờ giữ đủ `{0}` và `{1}`;
- MASTER **V6.2.1** và **79 term correction** có guard theo source/topology.

## Một vài câu mình đã sửa

- “Đi theo góc nhìn của nhân vật chính, mình như thể chính mình cũng đã trải qua hết cuộc phiêu lưu sử thi này đến cuộc phiêu lưu sử thi khác...” → **“Càng đọc theo góc nhìn của nhân vật chính, tôi càng có cảm giác như chính mình cũng đang phiêu lưu...”**
- “Mình nhìn chằm chằm anh ta... Anh ta khựng lại, ánh mắt né tránh đầy vi diệu.” → **“Bạn nhìn chằm chằm vào anh ta... anh ta khựng lại một nhịp rồi khẽ lảng ánh mắt đi.”**
- “Đều không phải.” → **“Không. Hôm nay tôi chẳng hẹn gặp ai, cũng không chờ ai đến cả.”**
- “Tại sao nó lại khao khát trở thành Loài người đến vậy?” → **“Tại sao nó lại muốn trở thành con người đến vậy?”**
- “Chú Lôi lại tự nói gì thế...” → **“Chú Lôi đang nói gì vậy?”**
- “Ấy, cậu không phải là {PlayerName} đó chứ!” → **“Ồ, cậu chẳng phải là {PlayerName} sao!”**
- “Ôm sao?” → **“Ôm á?”**
- Một số cảnh cũng sửa **ngươi → cậu**, cùng các chỗ `tôi/cậu`, `ta/cháu`, `bọn tôi` theo đúng người nói và ngữ cảnh.

Danh sách 43 câu có trước/sau và lý do sửa nằm trong `TRANSLATION_CHANGES_v3.0.9.md`.

## App / updater

- updater chỉ nhận đúng asset của version mới và kiểm **SHA-256** trước khi cài;
- update theo transaction, có health-check; bản mới mở lỗi thì rollback;
- xử lý các mod cũ/mod ngoài đã xác định là xung đột mà không đụng `pakchunk*` gốc của game;
- PAK, `.sig`, font hoặc loader do VHWuWa quản lý bị thiếu/ghi đè có thể được dựng lại và kiểm hash;
- PAK Hán Việt lấy đúng asset theo tag phiên bản app;
- Exit Watchdog xử lý process game do app mở còn treo sau khi cửa sổ game đã đóng;
- sửa nguồn tải font và thay link Discord cũ bằng invite cố định.

Người chơi cài mới chỉ cần `VietHoa-WuWa-v3.0.9.zip`. Nếu đang ở bản cũ, mở app để cập nhật lên 3.0.9. Release đã được phát hành tại https://github.com/WahuVN/Viet-Hoa-WuWa/releases/tag/v3.0.9.

Bản này đã kiểm thử với **Wuthering Waves 3.6 trên PC/Windows**. Khi game lên bản mới vẫn cần kiểm lại trước khi coi là tương thích.
