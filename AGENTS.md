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
+
---

## 🔄 5. QUY TẮC BẮT BUỘC CHO TỰ CẬP NHẬT CLIENT

Các quy tắc này áp dụng cho mọi thay đổi updater từ bản `N` lên `N+1`; không được kết luận “sạch” chỉ bằng tìm chuỗi hoặc static check.

1. **Một nguồn phát hành duy nhất:** Client chỉ được tự cài asset có tên chính xác `VietHoa-WuWa-vX.X.X.zip` của tag `vX.X.X`. Không chọn ZIP “gần giống”, app-only ZIP hoặc asset của version khác.
2. **SHA-256 là bắt buộc:** Chỉ cho phép tải/cài khi GitHub Release API trả về digest `sha256:<64 hex>`. Không được fallback sang URL redirect có SHA rỗng và không được coi “ZIP mở được” là xác minh tính toàn vẹn.
3. **Payload bắt buộc:** ZIP cập nhật phải có `VHWuWa.exe` và `VHWuWa.Updater.exe` ở gốc payload (hoặc cùng thư mục `app/` chuẩn). Hai EXE phải cùng đúng version của release và version mới phải lớn hơn version đang chạy.
4. **Không dùng updater PowerShell inline:** Không dùng `Expand-Archive`, `Copy-Item`, `robocopy` hoặc `VHWuWa.Updater.next.exe` để tự sửa trực tiếp thư mục app. Luồng chuẩn là chạy `VHWuWa.Updater.exe` self-contained từ thư mục tạm.
5. **Không đóng app quá sớm:** Client chỉ shutdown sau khi updater đã chạy, xác minh SHA/layout và ghi ready-handshake. Nếu updater không ready hoặc thoát sớm, giữ nguyên client đang chạy.
6. **Cài theo transaction:** Tạo candidate ở cùng ổ đĩa bằng bản hiện tại + overlay payload mới; sau khi validate mới đổi tên nguyên thư mục `target -> backup`, `candidate -> target`. Không merge trực tiếp vào target.
7. **Chỉ commit sau health-check:** Bản mới phải mở thành công, hiển thị cửa sổ chính và ghi health file chứa đúng version. Updater phải giữ backup cho đến lúc đó. App mới crash, báo sai version hoặc timeout thì đóng app mới, rollback nguyên thư mục và mở lại bản cũ.
8. **Rollback phải chính xác:** Sau rollback không được còn file chỉ có ở bản mới; cấu hình/file cục bộ của người dùng phải còn nguyên. Không xóa backup nếu khôi phục chưa thành công.
9. **Dọn file tạm:** ZIP, updater chạy tạm, candidate và backup phải được dọn sau khi updater kết thúc; dọn lỗi không được làm mất bản đang hoạt động.
10. **Test bắt buộc trước khi báo hoàn tất:**
    - `dotnet test VHWuWa\VHWuWa.sln -c Release` phải 100% pass.
    - Chạy `powershell -ExecutionPolicy Bypass -File VHWuWa\scripts\test-client-self-update.ps1 -CurrentVersion N -NextVersion N+1`.
    - E2E phải xác nhận đủ: ready trước shutdown, cập nhật thành công, đúng version cả app/updater, giữ config người dùng, app mới lỗi thì rollback sạch, SHA sai thì không chạm bản cũ.
    - Timeout do stdout/WPF không được gọi là PASS; phải lấy exit code thật và kiểm tra cây file sau chạy.
11. **Build phải tự kiểm payload:** `build-dist.ps1` phải publish kèm updater self-contained, kiểm version của hai EXE, kiểm đúng hai EXE trong ZIP và in SHA-256 cuối cùng.
12. **Không tự phát hành:** Build/test xong chỉ báo đường dẫn, size, version và SHA-256. Chỉ commit, tag, push hoặc tạo GitHub Release khi người dùng yêu cầu rõ ràng.
