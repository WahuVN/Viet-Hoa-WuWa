# 📥 Hướng Dẫn Tải & Cài Đặt VHWuWa

[![Discord Server](https://img.shields.io/badge/Discord-Tham_Gia_Server_VHWuWa-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/Gy5YQ84Yc2)

> 💬 **Discord VHWuWa (Windows):** https://discord.gg/Gy5YQ84Yc2
> 📱 **Discord Android (dự án cộng đồng độc lập):** https://discord.gg/3t5NSyJEz

Bản hiện tại: **VHWuWa v3.0.9**
Đã kiểm thử với: **Wuthering Waves 3.6 (PC/Windows)**

Truy cập [GitHub Releases](https://github.com/WahuVN/Viet-Hoa-WuWa/releases/latest) và chọn đúng gói theo nhu cầu.

---

## 📦 Chọn gói tải về

| Nhu cầu | Tệp cần tải | Sau khi giải nén |
| :--- | :--- | :--- |
| 🎮 **Cài và chơi Việt hóa** | `VietHoa-WuWa-v3.0.9.zip` | Mở `VHWuWa.exe` |
| 👥 **Dịch / duyệt / đóng góp** | `App-Dich-WuWa-v3.0.9.zip` | Mở `WAHU Community.exe` |

> [!IMPORTANT]
> Người chơi thông thường chỉ cần **`VietHoa-WuWa-v3.0.9.zip`**. Không cần tải Source code hay bộ WAHU Community.

---

## ⚡ Cài đặt cho người chơi

1. Tải **`VietHoa-WuWa-v3.0.9.zip`** và giải nén toàn bộ ra một thư mục riêng.
2. Mở **`VHWuWa.exe`**.
3. Bấm **Tự tìm game**; nếu không tìm thấy, chọn thủ công thư mục game có thư mục con `Client`.
4. Chọn kiểu tên **Tiếng Anh** hoặc **Hán Việt** và font mong muốn.
5. Bấm **Cài Việt hóa**.
6. Trong game đặt **Settings → Language → Text Language = English**.

Khi cài/cập nhật, v3.0.9 có thể tự dọn các mod đã được detector xác định là xung đột rồi phục hồi PAK/.sig/font/loader canonical. Các `pakchunk*` gốc của game không thuộc tập cleanup này.

### Ví dụ thư mục game hợp lệ

```text
D:\Game\Wuthering Waves Game
D:\Games\Wuthering Waves\Wuthering Waves Game
C:\Wuthering Waves\Wuthering Waves Game
D:\SteamLibrary\steamapps\common\Wuthering Waves
```

---

## 🔄 Cập nhật

- App tự kiểm tra GitHub Release khi mở nếu **Tự kiểm tra cập nhật** đang bật.
- Chỉ chấp nhận đúng asset `VietHoa-WuWa-vX.Y.Z.zip` và SHA-256 do GitHub cung cấp.
- Updater có health-check; nếu bản mới không khởi động đúng, transaction có thể rollback về bản cũ.
- PAK Hán Việt được lấy theo **đúng tag phiên bản app**, không lấy nhầm asset của release khác.

---

## 🐛 Báo lỗi

Nếu thấy câu dịch sượng, xưng hô sai, English còn sót, text lỗi hiển thị hoặc lỗi cài/update:

- Discord: https://discord.gg/Gy5YQ84Yc2
- GitHub Issues: https://github.com/WahuVN/Viet-Hoa-WuWa/issues

Nếu tiện, gửi **ảnh + nhiệm vụ/màn hình + nhân vật/ngữ cảnh**. Bot Discord v3.0.9 cũng được chuẩn bị với lệnh **`/bao-loi`** và **`/gop-y`** để đưa phản hồi vào hàng đợi kiểm tra.

---

## ⚠️ Tuyên bố rủi ro

VHWuWa là dự án cộng đồng, không trực thuộc hoặc được Kuro Games ủy quyền. Công cụ không inject/patch bộ nhớ game, nhưng vẫn là phần mềm/mod bên thứ ba tác động tới tệp phục vụ hiển thị; **không thể cam kết rủi ro tài khoản bằng 0**. Người dùng nên tự cân nhắc trước khi sử dụng.
