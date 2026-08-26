# 🛡️ WuWaVH Master Development, Build & Release Guardrails

Dự án **WuWaVH** (Việt Hóa Wuthering Waves) là một hệ sinh thái gồm:
1. **`VHWuWa` (.NET 8 WPF, C#):** Trình cài đặt, quản lý mod, đổi font, ẩn/đổi UID, tối ưu đồ họa và tự động cập nhật dành cho người chơi.
2. **`wuwavh_tool/Wahu` (Python 3.14, SQLite, PyInstaller, repak):** Bộ công cụ dịch thuật chuyên sâu (WAHU Community), trích xuất dữ liệu Unreal Engine, biên dịch PAK (`repak.exe`), xử lý danh hiệu, thuật ngữ và loader DLLs (`version.dll`, `verorg.dll`, `WuWaVH.dll`).

---

## 🚫 1. NGUYÊN TẮC BẤT DI BẤT DỊCH (ZERO-TOLERANCE RULES)

1. **CẤM RELEASE KHI CHƯA PASS 100% UNIT TESTS:**
   - Trước khi đóng gói hoặc kết luận bất kỳ task nào, **BẮT BUỘC** phải chạy `dotnet test VHWuWa.sln`.
   - Tất cả các bài test (Core Tests, Infrastructure Tests, XAML Validation Tests) phải đạt **100% Passed**.

2. **CẤM TUYỆT ĐỐI DÙNG STATICRESOURCE / DYNAMICRESOURCE CHƯA KHAI BÁO TRONG XAML:**
   - Trong WPF, mọi `{StaticResource KeyName}` hoặc `{DynamicResource KeyName}` **BẮT BUỘC** phải được định nghĩa bằng `x:Key="KeyName"` trong `App.xaml` hoặc trong `<UserControl.Resources>` / `<Window.Resources>` cục bộ.
   - Trình biên dịch `dotnet build` **KHÔNG** bắt lỗi thiếu StaticResource lúc build, nhưng ứng dụng sẽ **CRASH NGAY LẬP TỨC** khi khởi động (`XamlParseException`).
   - Mọi chỉnh sửa XAML phải vượt qua bài test tự động `XamlResourceValidationTests`.

3. **CẤM SỬA LOGIC DỮ LIỆU GAME MÀ KHÔNG ĐỒNG BỘ CẢ 2 BIẾN THỂ:**
   - Dự án luôn duy trì song song 2 biến thể tên:
     * **`English` (Tên Tiếng Anh):** Tên nhân vật/boss/vũ khí giữ nguyên bản tiếng Anh chuẩn quốc tế.
     * **`HanViet` (Tên Hán Việt):** Tên nhân vật/boss/vũ khí chuyển dịch theo chuẩn âm Hán Việt chuẩn xác.
   - Mọi cập nhật Database (`project.db`) phải có đầy đủ dữ liệu cho cả hai cột `vi` và `hv`.

4. **CẤM THAY ĐỔI CẤU TRÚC ĐƯỜNG DẪN GAME HOẶC MOD LOADER SAI QUY CHUẨN:**
   - Thư mục game hợp lệ **BẮT BUỘC** phải chứa `Client/` và `Client/Binaries/Win64/Client-Win64-Shipping.exe`.
   - File PAK Việt hóa bắt buộc nằm tại: `<GamePath>/Client/Content/Paks/~mods/` (hoặc `Client/Saved/Paks/` / `Client/Content/Paks/`).
   - Bộ 3 Loader DLLs (`version.dll`, `verorg.dll`, `WuWaVH.dll`) bắt buộc nằm tại: `<GamePath>/Client/Binaries/Win64/`.

---

## 🏗️ 2. KIẾN TRÚC HỆ THỐNG & LOGIC CỐT LÕI

### 2.1. Cấu trúc Solution VHWuWa (.NET 8 WPF)
* **`VHWuWa.Core`**:
  - `Models/`: `NameVariant`, `PackageType`, `GameLaunchOptions`, `Result<T>`.
  - `Abstractions/`: `ISettingsService`, `IGameDetectionService`, `IGameLaunchService`, `IPackageInstallerService`, `IViethoaInstaller`, `IFontService`, `ILogService`, `ISelfUpdateService`.
  - `Services/`: `ErrorFormatter` (Việt hóa 100% mã lỗi hệ thống và ngoại lệ), logic nghiệp vụ độc lập UI.
* **`VHWuWa.Infrastructure`**:
  - Triển khai Windows API, Registry Detection (Steam, Epic, Official Launcher), Win32 Process Management, File IO & Backup, ZIP extraction.
* **`VHWuWa.App`**:
  - MVVM pattern (`CommunityToolkit.Mvvm`).
  - Giao diện Wpf.Ui: `HomePage`, `InstallPage`, `UidEditorPage`, `CharacterNamesPage`, `TermsEditorPage`, `FontPage`, `GraphicsPage`, `GuidePage`, `SettingsPage`.
* **`VHWuWa.Core.Tests` & `VHWuWa.Infrastructure.Tests`**:
  - Bộ kiểm thử tự động xUnit, bao gồm `XamlResourceValidationTests` tự động quét toàn bộ cây XAML.

### 2.2. Logic Ẩn / Đổi Biệt Hiệu UID
Tính năng UID quản lý 4 vị trí khóa trong game:
1. **HUD Watermark:** Góc dưới cùng bên phải màn hình khi chơi.
2. **Player Profile Card:** Menu chính / Thẻ căn cước Rover.
3. **Chat & Co-op Lobby:** Khung chat và sảnh chờ nhiều người chơi.
4. **Photo Mode:** Watermark khi dùng tính năng chụp ảnh trong game.
*Logic lưu:* Khi người dùng đổi UID hoặc chọn Ẩn hoàn toàn, app gọi `WahuPakEditor.exe` (hoặc cập nhật trực tiếp DB/PAK) để tạo lại file PAK ghi đè tương ứng.

### 2.3. Logic Font & Repak On-Demand
- Danh mục 72 font hỗ trợ được định nghĩa trong `Fonts/fonts.json`.
- Chế độ on-demand: App tải font từ CDN khi người dùng chọn.
- Hỗ trợ nạp font ngoài (`.ttf`/`.otf`): Tự động gọi `tools/repak.exe` để đóng gói thành file PAK chuẩn UE tương thích ngay trong tích tắc.

---

## 📦 3. QUY ƯỚC ĐỒNG BỘ 100% GITHUB RELEASE

Mỗi Release trên repo `WahuVN/Viet-Hoa-WuWa` **BẮT BUỘC GỒM ĐÚNG 4 TỆP** sau:

| Tên tệp đính kèm | Dung lượng ước tính | Mục đích & Đối tượng |
| :--- | :--- | :--- |
| **`VietHoa-WuWa-vX.X.X.zip`** | ~190 MB | **Bộ cài chính thức:** Dành cho người chơi cài đặt, quản lý mod, đổi font, ẩn UID. |
| **`App-Dich-WuWa-vX.X.X.zip`** | ~246 MB | **WAHU Community:** Dành cho dịch giả / người đóng góp thuật ngữ (kèm DB SQLite đầy đủ). |
| **`WuWaVH_EN_99_P.pak`** | ~14 MB | **PAK Tên Anh:** File PAK ngôn ngữ độc lập. |
| **`WuWaVH_HanViet_99_P.pak`** | ~14 MB | **PAK Hán Việt:** File PAK ngôn ngữ độc lập. |

*Không được upload các tệp tạm như `VHWuWa_BanCai.zip`, `update.json`, `dist.zip`.*

---

## 📝 4. CẤU TRÚC CHUẨN CỦA RELEASE NOTES

Mô tả bài đăng Release bắt buộc tuân theo định dạng chuẩn đã được thống nhất:

```markdown
# Việt Hóa Wuthering Waves vX.X.X

## Thay đổi chính

- **[Tên tính năng/Nội dung 1]:** Mô tả ngắn gọn, súc tích những gì người dùng nhận được.
- **[Tên tính năng/Nội dung 2]:** Tránh dùng từ ngữ nội bộ kỹ thuật sâu, tập trung vào trải nghiệm.
- **[Tên tính năng/Nội dung 3]:** ...

## Cách cập nhật

- Đang dùng bản cũ: mở ứng dụng, nhận thông báo cập nhật tự động và bấm Cập nhật.
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

## 🛠️ 5. QUY TRÌNH BUILD & PHÁT HÀNH CHUẨN (CHECKLIST 5 BƯỚC)

Mọi quy trình phát hành phiên bản mới **BẮT BUỘC THỰC HIỆN ĐỦ 5 BƯỚC TUẦN TỰ**:

### Bước 1: Kiểm thử tự động (Automated Verification)
```powershell
dotnet test VHWuWa\VHWuWa.sln -c Release
```
*Yêu cầu:* Toàn bộ Unit Tests và `XamlResourceValidationTests` phải **PASS 100%**.

### Bước 2: Build Bộ Cài Người Chơi (Player Installer)
```powershell
powershell -ExecutionPolicy Bypass -File VHWuWa\scriptsuild-dist.ps1 -Version X.X.X
```
*Kết quả:* Tạo ra `VHWuWa\dist\VietHoa-WuWa-vX.X.X.zip` (đã tích hợp kiểm thử tự động tại Bước 0).

### Bước 3: Build Bộ Công Cụ Dịch Thuật (WAHU Community)
```powershell
powershell -ExecutionPolicy Bypass -File wuwavh_tool\Wahuuild-community.ps1 -Version X.X.X -IncludeDatabase
```
*Kết quả:* Tạo ra `VHWuWa\dist\App-Dich-WuWa-vX.X.X.zip`.

### Bước 4: Kiểm tra tính toàn vẹn (Smoke Test)
- Kiểm tra dung lượng 4 tệp trong `VHWuWa\dist\`.
- Đảm bảo có đủ 2 tệp zip và 2 tệp `.pak`.

### Bước 5: Upload & Cập nhật GitHub Release
```powershell
# Upload 4 tệp chuẩn
gh release upload vX.X.X "VHWuWa\dist\VietHoa-WuWa-vX.X.X.zip" "VHWuWa\dist\App-Dich-WuWa-vX.X.X.zip" "VHWuWa\dist\WuWaVH_EN_99_P.pak" "VHWuWa\dist\WuWaVH_HanViet_99_P.pak" --repo WahuVN/Viet-Hoa-WuWa --clobber

# Cập nhật tiêu đề và nội dung Release Notes
gh release edit vX.X.X --title "Việt Hóa Wuthering Waves vX.X.X" --notes-file "VHWuWa\dist\RELEASE_BODY.md" --repo WahuVN/Viet-Hoa-WuWa

# Xác minh lại lần cuối
gh release view vX.X.X --repo WahuVN/Viet-Hoa-WuWa
```
