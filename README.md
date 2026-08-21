# 🎮 VHWuWa — Launcher & Trình Quản Lý Việt Hóa Wuthering Waves

[![.NET 8](https://img.shields.io/badge/.NET-8.0_WPF-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows_10%2F11_x64-0078D6?logo=windows&logoColor=white)](https://github.com/WahuVN/wuwa-vietnamese-launcher)
[![Game Version](https://img.shields.io/badge/Wuthering_Waves-v3.6_Ready-FFB800)](https://wutheringwaves.kurogames.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![GitHub Release](https://img.shields.io/github/v/release/WahuVN/wuwa-vietnamese-launcher?color=blue&label=Latest%20Release)](https://github.com/WahuVN/wuwa-vietnamese-launcher/releases/latest)

**VHWuWa** là ứng dụng Windows hiện đại (WPF / .NET 8) hỗ trợ người chơi tự động cài đặt, gỡ bỏ và quản lý **Bản dịch Việt hóa Wuthering Waves**, tùy biến kiểu **Tên Nhân Vật (Tên Anh / Hán Việt)**, chuyển đổi hơn **70+ Font chữ tiếng Việt** sắc nét, quản lý Mod an toàn và áp dụng các cấu hình tối ưu đồ họa (Graphics Presets).

> ### ⚠️ CẢNH BÁO QUAN TRỌNG VỀ TÀI KHOẢN & RỦI RO ANTI-CHEAT (VUI LÒNG ĐỌC KỸ)
> * **Bản Việt hóa và công cụ mod là dự án phi lợi nhuận do cộng đồng tự phát triển**, hoàn toàn **KHÔNG** thuộc bản quyền hay được bảo đảm/ủy quyền bởi **Kuro Games**.
> * Việc can thiệp vào tệp trò chơi hoặc chèn tệp ngôn ngữ/font ngoài **vẫn luôn có khả năng bị hệ thống Anti-cheat của nhà phát hành quét trúng và dẫn đến việc KHÓA TÀI KHOẢN (BAN NICK)**.
> * Vui lòng **tự cân nhắc kỹ lưỡng và tự chịu trách nhiệm** về mọi rủi ro tài khoản khi sử dụng. Nếu bạn lo ngại, khuyến khích nên trải nghiệm thử trên **tài khoản phụ (clone)** trước.

---

## 📥 Tải Về Bản Mới Nhất

👉 Truy cập trang **[GitHub Releases](https://github.com/WahuVN/wuwa-vietnamese-launcher/releases/latest)** và chọn gói phù hợp:

| Tệp tải về | Đối tượng & Mục đích | Nội dung bao gồm |
| :--- | :--- | :--- |
| 🎮 **`01_CAI_VIET_HOA_WUWA.zip`** | **Dành cho Người chơi game** (~94.7 MB) | Ứng dụng Launcher + Dữ liệu Việt hóa 3.6 + Bộ 70+ Font tiếng Việt + File khởi chạy nhanh `00_BAT_DAU_CAI.bat`. |
| 👥 **`02_TOOL_DICH_WAHU_COMMUNITY.zip`** | **Dành cho Cộng tác viên dịch** (~490 MB) | Toàn bộ công cụ dịch thuật WAHU Community 3.6: duyệt cốt truyện theo tuyến, dịch tay song ngữ, QA và tạo PAK đóng góp. |
| 🛡️ **`03_KIEM_TRA_SHA256.txt`** | **Xác thực toàn vẹn** | Bảng mã băm SHA-256 đối chiếu tính nguyên bản của các gói tải về. |

> 📌 **Lưu ý:** Người chơi thông thường **không tải** mục *Source code (zip/tar.gz)*.

---

## ⚡ Hướng Dẫn Cài Đặt Nhanh (Dành cho Người chơi)

1. **Tải về:** Tải gói **`01_CAI_VIET_HOA_WUWA.zip`** từ mục Releases.
2. **Giải nén:** Giải nén toàn bộ tệp zip ra một thư mục riêng (ví dụ: `D:\VHWuWa\`).
3. **Mở App:** Chạy file **`00_BAT_DAU_CAI.bat`** (hoặc `app\VHWuWa.exe`).
4. **Chọn thư mục game:**
   - Bấm **🔍 Tự tìm game** để app tự quét registry / Steam / Epic Games.
   - Nếu app chưa nhận, bấm **📂 Chọn thư mục** và trỏ đến thư mục chứa game (phải thấy thư mục con `Client`).
5. **Chọn kiểu tên nhân vật:**
   - 🇬🇧 **Tên Anh:** `Jinhsi`, `Changli`, `Yangyang`, `Shorekeeper`, `Suisui`...
   - 🇻🇳 **Hán Việt:** `Kim Tịch`, `Trường Ly`, `Ương Ương`, `Thủ Ngạn Nhân`, `Tuệ Tuệ`...
6. **Cài đặt:** Giữ tùy chọn **Cài kèm font tiếng Việt** và bấm **✅ Cài Việt hóa**.
7. **Thiết lập trong game:**
   - Đặt ngôn ngữ hiển thị trong game là **English**.
   - Khởi chạy game ở chế độ **DirectX 11**.

---

### 📂 Mẫu Đường Dẫn Thư Mục Game Hợp Lệ

* **Bản Launcher chính thức (Kuro Games):**
  ```text
  C:\Wuthering Waves\Wuthering Waves Game
  D:\Games\Wuthering Waves\Wuthering Waves Game
  E:\WutheringWaves\Wuthering Waves Game
  ```
* **Bản Steam:**
  ```text
  C:\Program Files (x86)\Steam\steamapps\common\Wuthering Waves
  D:\SteamLibrary\steamapps\common\Wuthering Waves
  ```

> 📌 **Quy tắc vàng:** Thư mục bạn chọn phải mở ra và thấy ngay thư mục con `Client`.  
> ❌ **Không chọn:** `...\Client`, `...\Client\Content\Paks` hoặc thư mục chỉ chứa launcher ngoài.

---

## 🔤 Thư Viện 70+ Font Tiếng Việt & Quản Lý Font

Tab **Font chữ** trong VHWuWa cung cấp công cụ chuyển đổi font chữ toàn diện:

### 🌟 Tính năng nổi bật của hệ thống Font:
- **70+ Font tiếng Việt chuẩn Unicode:** Tối ưu hóa hiển thị, không bị lỗi dấu, không bị ô vuông.
- **Tìm kiếm thông minh:** Gõ tên font (Arial, Calibri, Segoe...) để lọc nhanh trong danh sách.
- **Xem trước thời gian thực (Live Preview):** Nhập chữ mẫu tùy ý để kiểm tra độ sắc nét và nét dấu trước khi cài.
- **Hỗ trợ Font ngoài:** Nạp font `.ttf`, `.otf`, `.ttc` từ máy tính hoặc gói `.vhwpack`.
- **Khôi phục 1-Click:** Dễ dàng trở về font mặc định của bản Việt hóa bất cứ lúc nào.

### 🎨 Bảng Gợi Ý Phong Cách Font

| Nhóm Font | Đại diện tiêu biểu | Trải nghiệm mang lại |
| :--- | :--- | :--- |
| **🔹 Không chân (Sans-Serif)** | `Arial`, `Calibri`, `Segoe UI`, `Candara`, `Corbel` | Hiện đại, sắc nét, cực kỳ dễ đọc trên màn hình 1080p / 2K / 4K. |
| **🔸 Có chân (Serif/Cổ phong)** | `Cambria`, `Constantia`, `Palatino Linotype` | Cổ điển, trang nhã, rất hợp không khí kiếm hiệp Hoàng Long / Kim Châu. |
| **▪️ Đơn cách (Monospace)** | `Cascadia Code`, `Consolas`, `Courier New` | Đều đặn, phong cách công nghệ / Sci-Fi của Bờ Biển Đen (Black Shores). |

---

## 🔄 Đổi Biến Thể Tên & Gỡ Bỏ Sạch Sẽ

- **Đổi Tên Anh ↔ Hán Việt:** Chọn lại kiểu tên mong muốn trong tab **Cài Việt hóa** và bấm **Cài Việt hóa** lại.
- **Gỡ bỏ hoàn toàn:** Tắt game $\rightarrow$ Mở tab **Cài Việt hóa** $\rightarrow$ Bấm **🗑 Gỡ Việt hóa**. Toàn bộ file mod/PAK/font sẽ được xóa sạch 100%, trả game về nguyên bản.

---

## 📜 Nội Dung Dữ Liệu Bản 2.0.0 (Community 3.6)

* ✅ **Cốt truyện chính 3.6:** Đạt **1.827 / 1.827** câu thoại tiếng Việt (VI & HV).
* ✅ **Mô tả Kỹ năng:** Hoàn thiện 100% kỹ năng các mốc **3.4 (1.766 mục)**, **3.5 (93 mục)** và **3.6 (97 mục)**.
* ✅ **Hai bản PAK độc lập:** Xuất riêng biến thể Tên Anh canonical và Hán Việt.
* ✅ **Ẩn UID:** Tinh tế bằng ký tự trắng, tránh lộ thông tin khi quay chụp màn hình.
* ✅ **Hậu kiểm SHA-256:** Kiểm tra tính toàn vẹn của 85 tệp cơ sở dữ liệu sau khi cài đặt.

---

## 🛠️ WAHU Community (Dành cho Người Dịch & Đóng Góp)

Gói `WAHU_Community_BanCai.zip` cung cấp giao diện dịch thuật chuyên sâu:
- Duyệt cốt truyện theo khu vực, tuyến nhiệm vụ và từng Part.
- Đối chiếu song song 4 ngôn ngữ: **Trung (CN) - Anh (EN) - Việt (VI) - Hán Việt (HV)**.
- Tra cứu danh tính người nói, người nghe và quy chuẩn xưng hô ngữ cảnh.
- Sửa giao diện UI, tên nhân vật, vật phẩm, chỉ số thuộc tính (HP/ATK/DEF).
- Tích hợp bộ kiểm tra chất lượng (QA), tự động đóng gói PAK thử nghiệm và xuất gói đóng góp.

👉 Xem chi tiết tại: [Hướng dẫn đóng góp bản dịch](Guides/vi-VN/09-dong-gop-ban-dich.md).

---

## 🔧 Hướng Dẫn Build Từ Mã Nguồn

Yêu cầu môi trường: **Windows 10 / 11 (x64)**, **.NET 8.0 SDK**.

```powershell
# 1. Khôi phục dependencies
dotnet restore VHWuWa.sln

# 2. Chạy kiểm thử tự động
dotnet test VHWuWa.sln -c Release

# 3. Build toàn bộ bản phát hành
dotnet build VHWuWa.sln -c Release
```

---

## ⚖️ Giấy Phép & Bản Quyền

* Mã nguồn công cụ được phát hành theo giấy phép **[MIT License](LICENSE)**.
* **Tuyên bố miễn trừ:** Dự án này là công cụ mã nguồn mở phi thương mại do cộng đồng phát triển. Repository chỉ chứa mã nguồn công cụ và dữ liệu mẫu, không chứa tài sản game có bản quyền hoặc các tệp PAK gốc của nhà phát triển (Kuro Games).


