# VHWuWa v3.0.9

Bản cập nhật v3.0.9 tập trung vào độ ổn định của ứng dụng, quy trình cài/cập nhật Việt hóa và một đợt QA lớn cho dữ liệu dịch. Bản phát hành đã được kiểm lại luồng cài/gỡ mod, tự cập nhật, self-heal PAK/loader, Exit Watchdog và các thay đổi thuật ngữ/câu thoại trước khi đóng gói.

### 🌟 Điểm mới nổi bật

- **Logo mới:** Icon trong app và trên Desktop dùng logo nền trong suốt, hiển thị gọn và rõ hơn.
- **Shortcut WuWa:** Mở app sau khi cài để tự tạo shortcut Desktop tên **WuWa**; shortcut `VHWuWa` cũ sẽ được thay thế.
- **Giao diện gọn hơn:** Trang chủ và cài Việt hóa được rút gọn, ưu tiên mở game, chọn thư mục và cài/gỡ Việt hóa.
- **Tự cập nhật an toàn:** Kiểm tra đúng asset phát hành và SHA-256; updater self-contained cài theo transaction, health-check và rollback nếu bản mới không khởi động đúng.
- **Tự dọn mod xung đột:** Khi cài hoặc cập nhật nhanh, app tự xóa các mod cũ/mod ngoài đã được detector xác định là xung đột rồi cài đè PAK/loader của bản hiện tại; các `pakchunk*` gốc của game được bảo vệ.
- **Tự chữa bản cài:** Cập nhật nhanh phục hồi lại PAK, `.sig`, font và bộ loader nếu đã bị mod khác ghi đè/xóa, sau đó kiểm SHA-256 và trạng thái cài.
- **Tự dọn game chạy ẩn sau khi đóng:** Khi mở game từ app, Exit Watchdog chỉ kích hoạt sau khi cửa sổ `UnrealWindow` đã ổn định; nếu cửa sổ game biến mất liên tục 5 giây nhưng `Client-Win64-Shipping` vẫn còn chạy nền, app tự kết thúc đúng process tree đang theo dõi và dọn wrapper còn treo. Không inject/patch bộ nhớ game.
- **Dữ liệu Việt hóa mới:** So với v3.0.8 có 112 text mới và một đợt QA lớn cho cốt truyện, hảo cảm, kỹ năng, hướng dẫn, thuật ngữ và raw English; các regression `test/test`/English phát hiện trong candidate đã được sửa trước khi đóng gói.

### 📝 Một số thay đổi text đáng chú ý

- Bổ sung các mô tả nhạc nền/nhiệm vụ `ItemInfo_80110539..80110546` cho **Yên Vân U Viễn Tâm Kiếm Minh**, Ngự Kiếm Phi Hành, Vân Bạch Cơ Kỵ, Nhân Cảnh/Địa Cảnh và Thiên Khôi Kiếp Sát.
- Chuẩn hóa nhiều thuật ngữ/chuỗi hiển thị, ví dụ các nhóm **Đánh Thường**, **Tấn Công Trên Không**, **Quyền Giáp**, **Khí**, **giây**, và nhiều câu kỹ năng còn lẫn English.
- Làm sạch annotation nội bộ bị lộ như `(Tý)`, `(Nguyện)`, `(Academy)`, `(Long)`, `(Tình yêu)`; sửa nhiều câu hội thoại/hảo cảm bị rơi nguyên về English.
- Phục hồi 270 text kỹ năng bị source dev `test/test` ghi đè; bỏ `test/` lộ trong hướng dẫn; khôi phục 35 text cốt truyện/hảo cảm và sửa một lựa chọn Rogue giữ đúng cả `{0}` lẫn `{1}`.
- Có **43 câu P6 đã xác minh trước → sau thực sự nằm trong PAK R6**, tập trung vào câu Việt sượng/calque, xưng hô, register và ngữ cảnh. Danh sách đầy đủ được ghi trong `TRANSLATION_CHANGES_v3.0.9.md`.
- Đồng bộ MASTER **V6.2.1**: `Shell Credit`, `Quyền Giáp`, `Du Long Tích`, current-source alias cho Pioneer Association/Sword/Tactical Hologram/Phantom; R6 áp đúng **79** term override mới (3 `Du Long Tích` + 76 `Quyền Giáp`) với source fingerprint + topology guard.

### ✍️ Ví dụ câu dịch sượng/khô đã sửa

| Trước | Sau |
| :--- | :--- |
| “Đi theo góc nhìn của nhân vật chính, mình như thể chính mình cũng đã trải qua hết cuộc phiêu lưu sử thi này đến cuộc phiêu lưu sử thi khác...” | “Càng đọc theo góc nhìn của nhân vật chính, tôi càng có cảm giác như chính mình cũng đang phiêu lưu...” |
| “Trong cuộc họp hôm qua, có vài đồng nghiệp nhắc đến chuyện gần đây Kim Châu vừa đón một vị khách hiểu biết rộng rãi...” | “Trong cuộc họp hôm qua, mấy đồng nghiệp có nhắc đến một vị khách mới tới Kim Châu, nghe nói rất từng trải, hiểu biết rộng...” |
| “Cứu mạng với! Cứu mạng với...” | “Cứu với! Cứu tôi với—!” |
| “Tàn Tượng vẫn luôn dung hợp với tần số của họ, dần dần tự coi mình là Dạ Quy...” | “Những Tàn Tượng liên tục hòa vào tần số của họ, rồi dần tin rằng chính mình là Dạ Quy...” |
| “Chú Lôi lại tự nói gì thế...” | “Chú Lôi đang nói gì vậy?” |
| “Tại sao nó lại khao khát trở thành Loài người đến vậy?” | “Tại sao nó lại muốn trở thành con người đến vậy?” |
| “Đã có điều Bất thường, vậy thì đi xem thử đi.” | “Nếu đã thấy có gì bất thường thì cứ đi xem thử.” |
| “Quả thật rất mang phong cách của thi nhân.” | “Đúng là rất ra dáng một nhà thơ.” |
| “Đều không phải.” | “Không. Hôm nay tôi chẳng hẹn gặp ai, cũng không chờ ai đến cả.” |
| “Phương án vừa rồi... liệu có quá bảo thủ không?” | “Phương án vừa rồi... Có phải hơi dè dặt quá không?” |
| “Ấy, cậu không phải là {PlayerName} đó chứ!” | “Ồ, cậu chẳng phải là {PlayerName} sao!” |
| “Ôm sao?” | “Ôm á?” |

### 🗣️ Ví dụ xưng hô/ngữ cảnh đã chỉnh

- `Side_CXLR_7_18`: **“ngươi” → “cậu”** theo quan hệ/ngữ cảnh hiện tại.
- Rover: **“mình” → “tôi”** ở những cảnh đã xác nhận continuity ngôi thứ nhất.
- NPC nhút nhát: **“chúng tớ” → “bọn tôi”**, đồng thời thêm “hết” để câu khẩu ngữ tự nhiên hơn.
- Chú Đổng: chuyển sang **“ta/cháu”** để khớp giọng người lớn tuổi và cách gọi Rover trong các cảnh liên quan.
- Phoebe: chỉnh “Hử?” thành **“Ừm?”** và bổ sung cách gọi người nghe theo scene.
- Giữ nguyên các nhánh `{Male=...;Female=...}`, placeholder, tag và option/action line khi sửa câu.

### 📥 Cách cập nhật & cài đặt

- **Đang dùng bản cũ:** Mở app, nhận thông báo cập nhật tự động và bấm **Cập nhật**.
- **Cài mới:** Tải `VietHoa-WuWa-v3.0.9.zip` bên dưới, giải nén và mở `VHWuWa.exe`.
- **Shortcut:** Sau lần mở đầu tiên, ngoài Desktop sẽ có shortcut **WuWa**.

### 📦 Tệp phát hành

| Tệp đính kèm | Mục đích sử dụng |
| :--- | :--- |
| **`VietHoa-WuWa-v3.0.9.zip`** | **Người chơi:** Cài đặt, đổi font, ẩn UID và quản lý Việt hóa. |
| **`App-Dich-WuWa-v3.0.9.zip`** | **Dịch giả / Đóng góp:** Bộ công cụ WAHU Community kèm Database SQLite. |
| **`WuWaVH_EN_99_P.pak`** | File PAK bản Tên Tiếng Anh độc lập. |
| **`WuWaVH_HanViet_99_P.pak`** | File PAK bản Tên Hán Việt độc lập. |

---

### 💬 Phản hồi & hỗ trợ

Nếu gặp lỗi bản dịch hoặc lỗi ứng dụng, hãy gửi ảnh chụp màn hình kèm vị trí/ngữ cảnh để dễ đối chiếu. Có thể báo trực tiếp trên Discord hoặc GitHub Issues.

- Discord WAHU: https://discord.gg/tuRCj47sy
- GitHub Issues: https://github.com/WahuVN/Viet-Hoa-WuWa/issues
- Nếu thấy dự án hữu ích, có thể Star repository hoặc chia sẻ release cho người cần.
