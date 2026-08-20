# 🎮 Wuthering Waves Vietnamese Launcher & Mod Manager

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8" />
  <img src="https://img.shields.io/badge/WPF-Windows_App-0078D6?style=for-the-badge&logo=windows&logoColor=white" alt="WPF" />
  <img src="https://img.shields.io/badge/Architecture-MVVM-FF6F00?style=for-the-badge" alt="MVVM" />
  <img src="https://img.shields.io/badge/Status-Maintained-success?style=for-the-badge" alt="Status" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="License" />
</p>

Ứng dụng Windows hiện đại (WPF / .NET 8) hỗ trợ người chơi cài đặt và quản lý **Bản dịch Việt hóa**, Mod trang phục/hiệu ứng, tùy biến Font chữ và cấu hình đồ họa cho tựa game **Wuthering Waves**.

---

## ✨ Tính năng nổi bật

- 🇻🇳 **Cài đặt & Gỡ bỏ Việt hóa an toàn**:
  - Hỗ trợ đóng gói và kiểm tra gói dịch thuật `.vhwpack` bằng chữ ký điện tử và kiểm tra mã băm SHA-256.
  - Tự động sao lưu (Backup) file gốc và hỗ trợ khôi phục (Rollback) an toàn khi gặp sự cố.
- 🧩 **Quản lý Mod & Tùy biến**:
  - Bật/Tắt các mod game linh hoạt, phát hiện xung đột tệp tin.
  - Thay đổi Font chữ và áp dụng các cấu hình Preset đồ họa (Graphics Presets) tối ưu FPS.
- 🎨 **Giao diện Fluent chuẩn Windows 11**:
  - Thiết kế hiện đại với hỗ trợ chế độ Sáng / Tối (Light / Dark mode).
  - Tự động dò tìm thư mục cài đặt game từ Registry / Steam / Epic Games.
- 🔄 **Hệ thống Auto-Updater độc lập**:
  - Tự động kiểm tra bản cập nhật mới từ GitHub Releases và cập nhật tự động.

---

## 📦 Tải về & Cài đặt

1. Truy cập mục **[Releases](https://github.com/WahuVN/wuwa-vietnamese-launcher/releases)** của repository.
2. Tải về bản phát hành mới nhất `VHWuWa-x.y.z-win-x64.zip`.
3. Giải nén và chạy file `VHWuWa.exe` *(Bản phát hành độc lập Self-contained, không yêu cầu cài thêm .NET Runtime)*.

---

## 🛠️ Hướng dẫn phát triển & Build từ mã nguồn

### Yêu cầu môi trường
- Windows 10 / 11 (x64)
- .NET 8.0 SDK
- Visual Studio 2022 hoặc VS Code với C# Dev Kit

### Lệnh build
```bash
# Khôi phục dependencies và build
dotnet restore VHWuWa.sln
dotnet build VHWuWa.sln -c Release

# Chạy unit tests
dotnet test VHWuWa.sln -c Release

# Tạo bản release đóng gói
powershell -ExecutionPolicy Bypass -File scripts/build-release.ps1 -Version 1.0.0
```

---

## 🏗️ Kiến trúc dự án (Architecture)

```text
src/
├── VHWuWa.App            # Giao diện WPF (MVVM, Wpf.Ui, Dependency Injection)
├── VHWuWa.Core           # Mô hình dữ liệu & logic nghiệp vụ thuần .NET (Hash, Signature, .vhwpack)
├── VHWuWa.Infrastructure # Cài đặt/gỡ bỏ, sao lưu, mod, font, đồ họa, log (Serilog)
├── VHWuWa.Updater        # Trình cập nhật độc lập an toàn
└── VHWuWa.PackageTool    # Công cụ CLI đóng gói và ký số .vhwpack
```

---

## 📜 Giấy phép & Tuyên bố miễn trừ trách nhiệm
- Mã nguồn của launcher được phân phối dưới giấy phép [MIT License](LICENSE).
- **Lưu ý:** Repository này *chỉ chứa mã nguồn công cụ và dữ liệu mẫu*. Không chứa tài sản game có bản quyền hoặc các tệp PAK gốc của nhà phát triển.