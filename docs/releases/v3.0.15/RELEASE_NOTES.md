Bản cập nhật **VHWuWa v3.0.15** dành cho **Wuthering Waves 3.7.0**, sửa triệt để nhóm lỗi game hiện raw localization key dù bản dịch đã tồn tại.

### 🌟 Nội dung cập nhật
- Sửa lỗi identity nằm sai cơ sở dữ liệu runtime: **2.417 key** đã có bản dịch nhưng chỉ nằm ở `lang_multi_text_1sthalf.db`, nay được materialize đúng tại `lang_multi_text.db`.
- Materialize **1.535 identity source rỗng** để runtime trả nội dung rỗng thay vì fallback ra mã nội bộ.
- Ẩn **6 identity debug/test** còn lại theo cùng contract.
- Regression `Main_Mengzhou_3_7_3_1` đã đọc đúng câu Việt ở main MultiText DB.
- Gate mới bắt buộc **25.278/25.278 identity 3.7** tồn tại đúng `dbfile/table/key`; không còn chấp nhận key nằm nhầm DB.
- Quét final PAK: **raw key 3.7 = 0**, **token topology lỗi = 0**, **P0 = 0**, **P1 3.7 = 0**.
- Đồng bộ cả hai biến thể **Tên Anh** và **Hán Việt**.

### ✅ Kiểm tra
- Full strict checker EN: **PASS**, 0 failure, 0 warning.
- Hán Việt fresh-unpack: **25.278/25.278 exact identity**, raw key = 0, toàn bộ SQLite quick-check sạch.
- PAK Tên Anh SHA256: `1e647329564fd159c8f94849b84cfe2b2550703e4354093022416c4be951fa28`
- PAK Hán Việt SHA256: `2fb7e68a11c0dd8190887751a93f56aef12fde5316929a786585ea46290820b3`

### 📥 Cập nhật
- **Đang dùng bản cũ:** mở VHWuWa và kiểm tra cập nhật để lên **v3.0.15**.
- **Cài mới:** tải `VietHoa-WuWa-v3.0.15.zip`, giải nén và mở `VHWuWa.exe`.
- Trong game tiếp tục để **Text Language = English**.

### 📦 Tệp phát hành
| Tệp | Mục đích |
| :--- | :--- |
| `VietHoa-WuWa-v3.0.15.zip` | Bộ cài / cập nhật dành cho người chơi PC |
| `App-Dich-WuWa-v3.0.15.zip` | WAHU Community dành cho dịch giả / người đóng góp |
| `WuWaVH_EN_99_P.pak` | PAK Việt hóa dùng Tên Anh |
| `WuWaVH_HanViet_99_P.pak` | PAK Việt hóa dùng Hán Việt |
| `VHWuWa-v3.0.15-SHA256SUMS.txt` | SHA-256 kiểm tra toàn vẹn file |

---
Discord cộng đồng: https://discord.gg/Gy5YQ84Yc2
Báo lỗi / góp ý: https://github.com/WahuVN/Viet-Hoa-WuWa/issues
