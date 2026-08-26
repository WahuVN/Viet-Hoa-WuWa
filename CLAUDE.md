# 🚨 BỘ QUY TẮC PHÁT TRIỂN & PHÁT HÀNH BẮT BUỘC (MANDATORY AI & DEV RULES)

> **DÀNH CHO MỌI AI ASSISTANT (Antigravity, Claude, Cursor, Copilot, ChatGPT...) & LẬP TRÌNH VIÊN:**
> Bạn **BẮT BUỘC PHẢI ĐỌC VÀ TUÂN THỦ 100%** tất cả các quy tắc dưới đây trước khi thực hiện bất kỳ thay đổi mã nguồn, chạy lệnh build hoặc phát hành release!

---

## 🚫 1. NGUYÊN TẮC BẤT DI BẤT DỊCH (ZERO-TOLERANCE RULES)

1. **KHÔNG BAO GIỜ BUILD / RELEASE KHI CHƯA CHẠY TEST:**
   - Trước khi kết luận hoặc đóng gói release, bắt buộc phải chạy `dotnet test VHWuWa.sln`.
   - **Tất cả các bài test (Core, Infrastructure, XAML Validation) phải đạt 100% Passed**.

2. **CẤM TUYỆT ĐỐI DÙNG STATICRESOURCE / DYNAMICRESOURCE CHƯA KHAI BÁO:**
   - Trong XAML WPF, mọi `{StaticResource Tên_Key}` hoặc `{DynamicResource Tên_Key}` **BẮT BUỘC** phải có `x:Key="Tên_Key"` được khai báo trong `App.xaml` hoặc từ điển cục bộ.
   - Trình biên dịch C# không bắt lỗi này khi build, nhưng app sẽ **CRASH NGAY LẬP TỨC** khi người dùng mở ứng dụng (`XamlParseException`).
   - Luôn luôn để bài test `XamlResourceValidationTests` kiểm tra tự động.

3. **CẤM SỬA XAML MÀ KHÔNG CHẠY SMOKE TEST:**
   - Bất cứ khi nào chỉnh sửa tệp `.xaml`, phải chạy test kiểm tra định dạng và mở thử tệp `.exe` xem giao diện có tải đúng không.

4. **ĐỒNG BỘ 100% TÊN TỆP VÀ ĐỊNH DẠNG RELEASE:**
   - Tên 4 tệp release trên GitHub bắt buộc phải đúng chuẩn (xem mục 2).

---

## 📦 2. QUY ƯỚC TÊN TỆP GITHUB RELEASE (BẮT BUỘC ĐỒNG BỘ)

Mỗi bản phát hành `vX.X.X` trên GitHub (`WahuVN/Viet-Hoa-WuWa`) **BẮT BUỘC GỒM ĐÚNG 4 TỆP** sau:

| Tệp phát hành | Mô tả & Đối tượng |
| :--- | :--- |
| **`VietHoa-WuWa-vX.X.X.zip`** | Gói cài đặt & quản lý Việt hóa chính thức cho người chơi (Đầy đủ). |
| **`App-Dich-WuWa-vX.X.X.zip`** | Công cụ WAHU Community cho dịch giả và người đóng góp (kèm DB). |
| **`WuWaVH_EN_99_P.pak`** | Gói dữ liệu PAK Tên Anh độc lập. |
| **`WuWaVH_HanViet_99_P.pak`** | Gói dữ liệu PAK Hán Việt độc lập. |

*Tuyệt đối không upload các tệp thừa hoặc tên sai như `VHWuWa_BanCai.zip`, `update.json`, `dist.zip`.*

---

## 📝 3. CẤU TRÚC CHUẨN CỦA RELEASE NOTES

Nội dung mô tả (Markdown) của bản Release bắt buộc phải theo cấu trúc mẫu:

```markdown
# Việt Hóa Wuthering Waves vX.X.X

## Thay đổi chính

- **[Tính năng/Nội dung 1]:** Mô tả ngắn gọn, dễ hiểu, tránh thuật ngữ nội bộ.
- **[Tính năng/Nội dung 2]:** Mô tả cải tiến danh hiệu, hội thoại, giao diện...
- **[Tính năng/Nội dung 3]:** ...

## Cách cập nhật

- Đang dùng bản cũ: mở ứng dụng, kiểm tra cập nhật và cài bản mới.
- Cài mới: tải `VietHoa-WuWa-vX.X.X.zip`, giải nén đầy đủ rồi mở `VHWuWa.exe`.
- Nhớ tắt game hoàn toàn trước khi cài hoặc đổi bản Việt hóa.

## Tải gì?

| Tệp | Dành cho |
| --- | --- |
| **Bộ cài Việt hóa** | Người chơi: cài và quản lý Việt hóa (`VietHoa-WuWa-vX.X.X.zip`) |
| **WAHU Community** | Dịch giả/người đóng góp: chỉnh tên và thuật ngữ (`App-Dich-WuWa-vX.X.X.zip`) |
| **PAK Tên Anh** | Chỉ dùng khi cần bản Tên Anh riêng (`WuWaVH_EN_99_P.pak`) |
| **PAK Hán Việt** | Chỉ dùng khi cần bản Hán Việt riêng (`WuWaVH_HanViet_99_P.pak`) |

Hỗ trợ: [Discord WAHU](https://discord.gg/tuRCj47sy) · [GitHub Issues](https://github.com/WahuVN/Viet-Hoa-WuWa/issues)
```

---

## 🛠️ 4. QUY TRÌNH THỰC THI CHUẨN TRƯỚC KHI BÀN GIAO (5 BƯỚC)

1. **Bước 1 — Chạy Test:** `dotnet test VHWuWa\VHWuWa.sln` -> 100% Passed.
2. **Bước 2 — Build Installer:** `powershell -ExecutionPolicy Bypass -File VHWuWa\scriptsuild-dist.ps1 -Version X.X.X`
3. **Bước 3 — Build Community:** `powershell -ExecutionPolicy Bypass -File wuwavh_tool\Wahuuild-community.ps1 -Version X.X.X -IncludeDatabase`
4. **Bước 4 — Upload Release:** `gh release upload vX.X.X ... --clobber`
5. **Bước 5 — Kiểm tra Release:** `gh release view vX.X.X --repo WahuVN/Viet-Hoa-WuWa` để xác nhận 4 tệp và release body hiển thị hoàn hảo.
