Bản PC v3.0.10 cập nhật toàn bộ gói Việt hóa cho Wuthering Waves 3.7.0, đồng bộ cả hai biến thể Tên Anh và Hán Việt.

### 🌟 Điểm mới nổi bật
- **Hỗ trợ Wuthering Waves 3.7.0:** cập nhật dữ liệu Việt hóa lên bộ ConfigDB 3.7 hiện tại.
- **Tên Anh + Hán Việt đồng bộ:** hai PAK đều được dựng từ cùng nguồn 3.7; bản Hán Việt đã áp lại speaker, role và tên trong thoại.
- **Ứng dụng PC mới:** VHWuWa + updater cùng phiên bản 3.0.10.
- **PAK V12:** cả hai biến thể đều qua kiểm tra cấu trúc/index sau khi đóng gói.
- **Updater giữ luồng cũ:** người đang dùng bản trước có thể cập nhật tại chỗ khi release được phát hành.
- **Checksum release:** có file SHA256SUMS để đối chiếu toàn bộ asset.

### 📥 Cách cập nhật & Cài đặt
- **Đang dùng bản cũ:** mở app và dùng chức năng cập nhật khi v3.0.10 được phát hành.
- **Cài mới:** tải `VietHoa-WuWa-v3.0.10.zip`, giải nén và mở `VHWuWa.exe`.

### 📦 Bảng tải tệp phát hành

| Tệp đính kèm | Mục đích sử dụng |
| :--- | :--- |
| **`VietHoa-WuWa-v3.0.10.zip`** | Người chơi: cài đặt và quản lý Việt hóa PC |
| **`App-Dich-WuWa-v3.0.10.zip`** | Dịch giả / đóng góp |
| **`WuWaVH_EN_99_P.pak`** | PAK Tên Anh |
| **`WuWaVH_HanViet_99_P.pak`** | PAK Hán Việt |
| **`VHWuWa-v3.0.10-SHA256SUMS.txt`** | SHA256 của toàn bộ asset PC |

### ✅ QA
- dotnet test: **104/104 PASS**
- Smoke launch `VHWuWa.exe`: **PASS**
- App + updater: **3.0.10.0**
- Runtime Gacha delivery: **PASS**
- Embedded EN payload hash trong player ZIP khớp authority: **PASS**

---
💬 **Cộng đồng & Hỗ trợ:** [Discord WAHU](https://discord.gg/Gy5YQ84Yc2) · [Báo lỗi GitHub](https://github.com/WahuVN/Viet-Hoa-WuWa/issues)
