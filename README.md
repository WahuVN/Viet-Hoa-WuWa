# 🎮 VHWuWa — Việt Hóa Wuthering Waves

VHWuWa là bộ cài và công cụ quản lý bản Việt hóa Wuthering Waves trên Windows.

Hiện hỗ trợ **Wuthering Waves 3.6**, hai lựa chọn tên nhân vật **Tên Anh / Hán Việt**, thay đổi font tiếng Việt và một số công cụ hỗ trợ cài đặt, gỡ bỏ và chỉnh sửa bản dịch.

[📥 Tải bản mới nhất](https://github.com/WahuVN/wuwa-vietnamese-launcher/releases/latest) · [💬 Discord PC](https://discord.gg/c9ws4q9U7) · [📱 Discord Android (Dangdev)](https://discord.gg/3t5NSyJEz) · [🐛 Báo lỗi](https://github.com/WahuVN/wuwa-vietnamese-launcher/issues)

---

## 📥 Tải xuống

### `01_CAI_VIET_HOA_WUWA.zip`
> Dành cho người chơi.

Giải nén và chạy:
- `00_BAT_DAU_CAI.bat`
- hoặc: `app\VHWuWa.exe`

### `02_TOOL_DICH_WAHU_COMMUNITY.zip`
> Dành cho người muốn sửa hoặc đóng góp bản dịch.

Tool hỗ trợ xem và chỉnh sửa nội dung CN / EN / VI / HV, tìm câu thoại, NPC, kỹ năng, UI và đóng gói dữ liệu để thử trong game.

> 📌 *Nếu chỉ muốn chơi bản Việt hóa, không cần tải `Source code` do GitHub tự tạo.*

---

## 🎮 Cài đặt

1. Tải **`01_CAI_VIET_HOA_WUWA.zip`**.
2. Giải nén ra thư mục riêng.
3. Chạy **`00_BAT_DAU_CAI.bat`**.
4. Bấm **Tự tìm game**.
5. Chọn **Tên Anh** hoặc **Hán Việt**.
6. Bấm **Cài Việt hóa**.
7. Trong game, đặt ngôn ngữ hiển thị là **English** (và chạy bằng DirectX 11).

*Nếu tự tìm game không hoạt động, hãy chọn thư mục game có chứa thư mục con `Client` (ví dụ: `...\Wuthering Waves Game`).*

---

## 📖 Bản Việt hóa

Bản hiện tại hỗ trợ nội dung Wuthering Waves 3.6, bao gồm:

- Cốt truyện chính 3.6 (1.827/1.827 câu).
- Thoại NPC ngoài thế giới.
- Kỹ năng, UI, vật phẩm và thuật ngữ.
- Hai biến thể Tên Anh (*Jinhsi, Changli, Yangyang...*) / Hán Việt (*Kim Tịch, Trường Ly, Ương Ương...*).
- Hơn 70 font hỗ trợ tiếng Việt.

Chi tiết thay đổi của từng phiên bản xem tại [Releases](https://github.com/WahuVN/wuwa-vietnamese-launcher/releases) hoặc [CHANGELOG.md](CHANGELOG.md).

---

## 🔤 Font

Có thể đổi font trực tiếp trong VHWuWa, xem trước trước khi cài hoặc sử dụng font ngoài `.ttf`, `.otf`, `.ttc`.

---

## 👥 WAHU Community

Tool dành cho người muốn tham gia chỉnh sửa bản dịch:

- Đối chiếu song song CN / EN / VI / HV.
- Xem người nói và ngữ cảnh hội thoại.
- Sửa cốt truyện, NPC, UI, kỹ năng và thuật ngữ.
- Kiểm tra dữ liệu và tạo bản thử nghiệm.

Hướng dẫn chi tiết: [HUONG_DAN_TAI_DUNG_GOI.md](HUONG_DAN_TAI_DUNG_GOI.md).

---

## ⚠️ Lưu ý

- **VHWuWa là dự án cộng đồng**, không phải sản phẩm chính thức của Kuro Games và không được Kuro Games bảo trợ hoặc ủy quyền.
- Bản Việt hóa có thay đổi một số tệp của game. Dự án **không thể bảo đảm hoàn toàn về rủi ro tài khoản hoặc anti-cheat** khi sử dụng.
- Hãy tự cân nhắc trước khi cài.

---

## 💬 Hỗ trợ & đóng góp

Nếu gặp lỗi dịch, lỗi font hoặc sự cố cài đặt:

- 🎮 **Discord VHWuWa (Bản PC):** [https://discord.gg/c9ws4q9U7](https://discord.gg/c9ws4q9U7)
- 🐛 **Báo lỗi:** [GitHub Issues](https://github.com/WahuVN/wuwa-vietnamese-launcher/issues)

> 📱 **Bản Việt hóa cho Android:** Nếu bạn muốn tìm và cài đặt bản Việt hóa Wuthering Waves dành riêng cho thiết bị **Android**, vui lòng tham gia server Discord của **Dangdev**:  
> 👉 [https://discord.gg/3t5NSyJEz](https://discord.gg/3t5NSyJEz)

Đóng góp mã nguồn và dữ liệu bản dịch đều luôn được chào đón.

---

## 🔧 Build từ mã nguồn

Yêu cầu: Windows 10/11 x64 và .NET 8 SDK.

```bash
dotnet restore VHWuWa.sln
dotnet test VHWuWa.sln -c Release
dotnet build VHWuWa.sln -c Release
```

---

## ⚖️ Giấy phép

Mã nguồn được phân phối theo giấy phép [MIT License](LICENSE).


