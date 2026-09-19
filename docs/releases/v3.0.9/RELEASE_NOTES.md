# VHWuWa v3.0.9

Xin lỗi mọi người vì **3.0.9 ra chậm hơn dự kiến**. Mình giữ bản này lại thêm một thời gian để sửa cho kỹ hơn, nên so với **3.0.8** thì cả phần Việt Hóa lẫn app đều thay đổi khá nhiều.

Bản này đã kiểm thử với **Wuthering Waves 3.6 trên PC/Windows**. Khi game lên bản mới vẫn cần kiểm lại trước khi coi là tương thích.

## Từ 3.0.8 lên 3.0.9 có gì đổi?

Nếu đang ở 3.0.8 thì đây không phải bản chỉ đổi app. Mình đã quay lại rà cả PAK dịch, nhất là các câu nghe cứng, xưng hô lệch, text rơi về English/test và những thuật ngữ chưa đồng bộ.

So với PAK EN đang public ở **v3.0.8**:

- có **112 text mới**;
- **39 text** được bỏ theo thay đổi của source;
- **17.445 text đang có** bị thay đổi; tính cả 112 text thêm và 39 text bỏ thì có **17.596 triple dữ liệu khác nhau** giữa hai bản.

Con số 17.445 là tổng chênh lệch dữ liệu/thuật ngữ/source giữa hai PAK, **không phải 17 nghìn câu ngồi dịch lại bằng tay**. Trong đó có các đợt đồng bộ source, thuật ngữ và cleanup hàng loạt.

Riêng phần mình rà lại từng câu theo ngữ cảnh, có **43 câu trước → sau đã xác minh nằm đúng trong PAK cuối**. Ngoài ra còn:

- phục hồi **270 text kỹ năng** từng bị source dev `test/test` ghi đè;
- khôi phục **35 text cốt truyện/hảo cảm** bị rơi về English;
- bỏ `test/` bị lộ trong hướng dẫn;
- sửa một lựa chọn Rogue để giữ đủ cả `{0}` và `{1}`;
- đồng bộ MASTER **V6.2.1**, gồm `Shell Credit`, `Quyền Giáp`, `Du Long Tích` và các alias source hiện hành;
- áp đúng **79 term correction** có guard theo source/topology;
- khôi phục **7 text Hổ Khẩu** bị override cũ làm co cả câu thành tên địa danh. Trong đó có 4 text nhiệm vụ từ 3.0.8 từng chỉ còn “Mỏ Hổ Khẩu”/“Mỏ Mỏ Hổ Khẩu”.

Danh sách chi tiết hơn nằm trong [`TRANSLATION_CHANGES.md`](TRANSLATION_CHANGES.md).

## Một vài câu đã sửa

| Trước | Sau |
| :--- | :--- |
| “Đi theo góc nhìn của nhân vật chính, mình như thể chính mình cũng đã trải qua hết cuộc phiêu lưu sử thi này đến cuộc phiêu lưu sử thi khác...” | “Càng đọc theo góc nhìn của nhân vật chính, tôi càng có cảm giác như chính mình cũng đang phiêu lưu...” |
| “Mình nhìn chằm chằm anh ta... Anh ta khựng lại, ánh mắt né tránh đầy vi diệu.” | “Bạn nhìn chằm chằm vào anh ta... anh ta khựng lại một nhịp rồi khẽ lảng ánh mắt đi.” |
| “Đều không phải.” | “Không. Hôm nay tôi chẳng hẹn gặp ai, cũng không chờ ai đến cả.” |
| “Tại sao nó lại khao khát trở thành Loài người đến vậy?” | “Tại sao nó lại muốn trở thành con người đến vậy?” |
| “Chú Lôi lại tự nói gì thế...” | “Chú Lôi đang nói gì vậy?” |
| “Ấy, cậu không phải là {PlayerName} đó chứ!” | “Ồ, cậu chẳng phải là {PlayerName} sao!” |
| “Ôm sao?” | “Ôm á?” |
| “Mỏ Hổ Khẩu” *(text nhiệm vụ bị co mất cả câu)* | “Giao "Bộ Dữ Liệu Cũ Của Hổ Khẩu" cho người kể chuyện” |
| “Mỏ Hổ Khẩu” *(tên nhiệm vụ bị co sai)* | “Bao Câu Chuyện Cổ Kim · Hổ Khẩu” |
| “Này, ngươi đã từng nghe nói về hiện tượng Vượt Tần...” | “Này, cậu đã từng nghe nói về hiện tượng Vượt Tần...” |

Ngoài mấy câu trên còn có các chỉnh sửa về `tôi/cậu`, `ta/cháu`, `bọn tôi`, cách gọi người nghe, câu narration và option. Khi sửa vẫn giữ nguyên placeholder, tag, nhánh `{Male=...;Female=...}` và cấu trúc runtime của game.

## Phần ứng dụng

3.0.9 cũng sửa khá nhiều ở bộ cài/updater:

- updater chỉ nhận đúng asset của version mới và bắt buộc kiểm **SHA-256** trước khi cài;
- cập nhật theo transaction, có health-check; bản mới mở lỗi thì rollback về bản cũ;
- khi cài/cập nhật, app nhận diện nhóm mod cũ/mod ngoài đã xác định là xung đột để xử lý, không đụng `pakchunk*` gốc của game;
- nếu PAK, `.sig`, font hoặc loader do VHWuWa quản lý bị thiếu/ghi đè, cập nhật nhanh có thể dựng lại và hậu kiểm hash;
- PAK Hán Việt được lấy theo **đúng tag phiên bản app**, không lấy nhầm asset của release khác;
- Exit Watchdog xử lý trường hợp cửa sổ game đã đóng nhưng process game do app mở vẫn treo nền;
- sửa lại nguồn tải font, bỏ fallback release cũ không còn tồn tại;
- Discord trong app và WAHU Community chuyển sang invite cố định mới.

## Cài / cập nhật

- **Đang dùng bản cũ:** mở `VHWuWa.exe`; nếu bật tự kiểm tra cập nhật, app sẽ báo khi GitHub có bản mới.
- **Cài mới:** tải `VietHoa-WuWa-v3.0.9.zip`, giải nén rồi mở `VHWuWa.exe`.
- Trong game đặt **Text Language = English**.

## File phát hành

| Tệp | Dùng cho |
| :--- | :--- |
| `VietHoa-WuWa-v3.0.9.zip` | Người chơi |
| `App-Dich-WuWa-v3.0.9.zip` | Dịch giả / người đóng góp |
| `WuWaVH_EN_99_P.pak` | PAK Tên Anh |
| `WuWaVH_HanViet_99_P.pak` | PAK Hán Việt |

## Báo lỗi / góp ý

Nếu gặp câu dịch sượng, xưng hô sai, English còn sót, text hiển thị lỗi hoặc lỗi cài/update, cứ gửi **ảnh + nhiệm vụ/màn hình + nhân vật/ngữ cảnh** nếu có.

- Discord: https://discord.gg/Gy5YQ84Yc2
- GitHub Issues: https://github.com/WahuVN/Viet-Hoa-WuWa/issues

Bot Discord cũng đã được chuẩn bị lệnh `/bao-loi` và `/gop-y` để các report gửi qua command được lưu lại, đỡ bị trôi. Bot không tự sửa theo report; mình vẫn kiểm source và ngữ cảnh trước khi áp dụng.
