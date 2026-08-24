# 🎮 VHWuWa — Việt Hóa Wuthering Waves

Bộ cài đặt và công cụ quản lý bản dịch tiếng Việt cho **Wuthering Waves (PC - Windows)**.

---

## 📥 Tải xuống Bản Mới Nhất

| Tệp tải về | Mục đích |
| :--- | :--- |
| 🎮 [**VietHoa-WuWa-v3.0.5.zip**](https://github.com/WahuVN/Viet-Hoa-WuWa/releases/download/v3.0.5/VietHoa-WuWa-v3.0.5.zip) | **Dành cho người chơi** — Bộ cài Tool Việt hóa chính thức trên Windows (có sẵn bản Tiếng Anh + 72 Font) |
| 🛠️ [**App-Dich-WuWa-v3.0.4.zip**](https://github.com/WahuVN/Viet-Hoa-WuWa/releases/download/v3.0.4/App-Dich-WuWa-v3.0.4.zip) | **Dành cho dịch giả & modder** — Bộ công cụ Studio chỉnh sửa dữ liệu, QA và đóng gói |

> 📌 *Tải bản mới nhất tại: **[GitHub Releases](https://github.com/WahuVN/Viet-Hoa-WuWa/releases/latest)***

---

## ⚡ Hướng dẫn cài đặt nhanh

1. Tải file **`VietHoa-WuWa-vX.Y.Z.zip`** mới nhất và giải nén ra một thư mục riêng.
2. Mở **`VHWuWa.exe`**. Với bộ cài đầy đủ, có thể mở `Chay VHWuWa.bat` hoặc `app\VHWuWa.exe`.
3. Chọn **Tự tìm game** (hoặc chọn thư mục game có chứa thư mục con `Client`).
4. Chọn kiểu tên nhân vật (**Hán Việt** hoặc **Tên Tiếng Anh**).
5. Bấm **Cài Việt hóa**.
6. Vào game: đặt **Text Language = English**. Có thể dùng DirectX 11 hoặc 12; nếu DX12 gặp crash/lỗi đồ họa, hãy chuyển sang DX11.

Trong **Trang chủ → Khởi chạy game**, có thể mở trực tiếp file game thay vì launcher.
Tùy chọn môi trường C# thử nghiệm truyền `-ForceEnableCSharpEnvironment` và mặc định
tắt; nếu hoạt động, cuối phiên bản trong game có dấu `*`. Chế độ này không bảo đảm tăng
FPS trên mọi máy. Với Steam, app chỉ chép tham số để người dùng tự dán vào Launch Options.

---

## 📸 Hình ảnh

| 🎮 Việt hóa trong game (Wuthering Waves 3.6) | 👥 WAHU Community Tool |
| :---: | :---: |
| ![Việt hóa trong game](https://raw.githubusercontent.com/WahuVN/Viet-Hoa-WuWa/main/docs/images/vh_ingame.png) | ![WAHU Community Tool](https://raw.githubusercontent.com/WahuVN/Viet-Hoa-WuWa/main/docs/images/app_dich.png) |

---

## 💬 Hỗ trợ & Cộng đồng

* 🎮 **Discord :** [Tham gia Server VHWuWa](https://discord.gg/tuRCj47sy) — Nhận thông báo cập nhật, thảo luận.
* 📱 **Discord :** [Tham gia Server DangDev](https://discord.gg/3t5NSyJEz) — Nếu bạn muốn tìm bản Việt Hóa Android.
* ⚠️ **Báo lỗi :** [Gửi phản hồi tại GitHub Issues](https://github.com/WahuVN/Viet-Hoa-WuWa/issues)

---

## ⚠️ Lưu ý

> [!WARNING]
> * Hãy **tắt hoàn toàn game và launcher** trước khi cài hoặc gỡ Việt hóa.
> * **VHWuWa là dự án cộng đồng**, không phải sản phẩm chính thức của Kuro Games và không được Kuro Games bảo trợ hoặc ủy quyền.
> * Bản Việt hóa có thay đổi một số tệp của game nên không thể đảm bảo an toàn tuyệt đối đối với tài khoản hoặc hệ thống anti-cheat. Hãy tự cân nhắc trước khi sử dụng.

---

## ⚖️ Giấy phép

Mã nguồn được phân phối theo giấy phép [MIT License](LICENSE).

---

## 🚀 Phát hành bản mới

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-dist.ps1 -Version 2.1.0
```

Tạo release có tag đúng dạng **`v2.1.0`**, sau đó tải gói dành cho người chơi trong `dist` lên:

- `VietHoa-WuWa-v2.1.0.zip`

Khi phát hành kèm công cụ dịch hoặc PAK cài thủ công, tải thêm:

- `App-Dich-WuWa-v2.1.0.zip`
- `WuWaVH_HanViet_99_P.pak`

Không đổi tên tệp sau khi build. Ứng dụng tự đọc phiên bản, URL tải và SHA-256 của asset trực tiếp từ GitHub Release API để kiểm tra rồi cập nhật an toàn; không cần `update.json` hoặc `checksums.txt`.
