# 🛡️ QUY TẮC PHÁT TRIỂN & PHÁT HÀNH WUWAVH

Tài liệu này quy định các kiểm tra logic bắt buộc khi chỉnh sửa code, giao diện XAML, đóng gói và phát hành bản cập nhật cho dự án Việt Hóa Wuthering Waves.

---

## 🚫 1. KIỂM TRA BẮT BUỘC TRƯỚC KHI BUILD / RELEASE
1. **Kiểm tra XAML:** Mọi `{StaticResource Key}` phải có `x:Key` tương ứng trong `App.xaml` hoặc ResourceDictionary cục bộ (tránh lỗi `XamlParseException` làm văng app khi mở).
2. **Kiểm tra Unit Test:** Bắt buộc chạy `dotnet test VHWuWa\VHWuWa.sln` đạt 100% Passed (bao gồm bài test tự động `XamlResourceValidationTests`).
3. **Cập nhật README.md chính:** Khi tăng version (ví dụ `v3.0.7`), **BẮT BUỘC** phải cập nhật lại badge phiên bản, tên tệp tải về trong bảng download và các tính năng mới trong `README.md` (cả ở thư mục gốc và thư mục `VHWuWa`), sau đó commit & push lên GitHub.
4. **Kiểm tra Logic Game & 2 Biến Thể:**
   - Dữ liệu `project.db` phải đồng bộ cả 2 biến thể: `English` (Tên Anh chuẩn quốc tế) và `HanViet` (Tên Hán Việt).
   - Thư mục game hợp lệ phải có `Client\Binaries\Win64\Client-Win64-Shipping.exe`.
   - File PAK đặt tại `Client\Content\Paks\~mods\`.
   - Loader 3 DLLs (`version.dll`, `verorg.dll`, `WuWaVH.dll`) đặt tại `Client\Binaries\Win64\`.
   - Tính năng Ẩn/Đổi UID phải đồng bộ đủ 4 vị trí: HUD góc dưới phải, Thẻ Profile Esc, Khung Chat/Co-op và Chế độ Chụp ảnh.

---

## 📦 2. CHUẨN 4 TỆP PHÁT HÀNH GITHUB (vX.X.X)
Mọi bản release trên `WahuVN/Viet-Hoa-WuWa` bắt buộc phải có đúng 4 tệp đính kèm:
1. `VietHoa-WuWa-vX.X.X.zip` — Bộ cài & quản lý Việt Hóa cho người chơi.
2. `App-Dich-WuWa-vX.X.X.zip` — WAHU Community cho dịch giả (kèm DB SQLite).
3. `WuWaVH_EN_99_P.pak` — File PAK Tiếng Anh độc lập.
4. `WuWaVH_HanViet_99_P.pak` — File PAK Hán Việt độc lập.

---

## 📝 3. NỘI DUNG RELEASE NOTES CHUẨN (TỐI ƯU EMBED DISCORD/FACEBOOK)
*Không để tiêu đề `# Tên Release` ở dòng 1 (tránh bị lặp tiêu đề 2 lần trên card ảnh OpenGraph và link preview).*
*Dòng 1 luôn là một câu tóm tắt nổi bật, ngắn gọn về bản cập nhật:*

```markdown
Bản cập nhật vX.X.X ... [tóm tắt điểm nổi bật nhất trong 1-2 câu] ...

### 🌟 Điểm mới nổi bật
- **[Tính năng 1]:** Mô tả ngắn gọn, dễ hiểu.
- **[Tính năng 2]:** ...

### 📥 Cách cập nhật & Cài đặt
- **Đang dùng bản cũ:** Mở app, nhận thông báo cập nhật tự động và bấm Cập nhật.
- **Cài mới:** Tải `VietHoa-WuWa-vX.X.X.zip` bên dưới, giải nén và mở `VHWuWa.exe`.

### 📦 Bảng tải tệp phát hành

| Tệp đính kèm | Mục đích sử dụng |
| :--- | :--- |
| **`VietHoa-WuWa-vX.X.X.zip`** | **Người chơi:** Cài đặt, đổi font, ẩn UID và quản lý Việt hóa trọn gói |
| **`App-Dich-WuWa-vX.X.X.zip`** | **Dịch giả / Đóng góp:** Bộ công cụ WAHU Community kèm Database SQLite |
| **`WuWaVH_EN_99_P.pak`** | File PAK bản Tên Tiếng Anh độc lập |
| **`WuWaVH_HanViet_99_P.pak`** | File PAK bản Tên Hán Việt độc lập |

---
💬 **Cộng đồng & Hỗ trợ:** [Discord WAHU](https://discord.gg/tuRCj47sy) · [Báo lỗi GitHub](https://github.com/WahuVN/Viet-Hoa-WuWa/issues)
```

---

## 🛠️ 4. CÁC LỆNH BUILD & RELEASE CHUẨN
```powershell
# 1. Chạy toàn bộ test
dotnet test VHWuWa\VHWuWa.sln -c Release

# 2. Build bộ cài người chơi (đã tích hợp tự chạy test ở Bước 0)
powershell -ExecutionPolicy Bypass -File VHWuWa\scripts\build-dist.ps1 -Version X.X.X

# 3. Build bộ công cụ dịch thuật
powershell -ExecutionPolicy Bypass -File wuwavh_tool\Wahu\build-community.ps1 -Version X.X.X -IncludeDatabase

# 4. Upload GitHub Release
gh release upload vX.X.X "VHWuWa\dist\VietHoa-WuWa-vX.X.X.zip" "VHWuWa\dist\App-Dich-WuWa-vX.X.X.zip" "VHWuWa\dist\WuWaVH_EN_99_P.pak" "VHWuWa\dist\WuWaVH_HanViet_99_P.pak" --repo WahuVN/Viet-Hoa-WuWa --clobber

# 5. Cập nhật README.md & commit push
git add README.md VHWuWa\README.md
git commit -m "docs: Update README for vX.X.X"
git push origin main
```
