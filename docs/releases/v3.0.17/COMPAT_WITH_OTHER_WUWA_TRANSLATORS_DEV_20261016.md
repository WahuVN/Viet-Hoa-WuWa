# WuWa 3.0.17 – CompatGuard DEV, evidence from official DangDevVH

Phân tích tĩnh ngày 16/10/2026, không chạy file lạ và không thay đổi game thực tế.

## Nguồn trực tiếp, không phải fork

Hugging Face: https://huggingface.co/datasets/BachMacThanh/DangDevVH

- Windows `Launcher/DangDevVH.exe`: v2.3.0, 20,485,052 bytes, SHA-256 `D6E882FC0F5129A275CE5270FBE4D0105012AA3C3817645E20C1FE2E147388F2`.
- Android `Launcher/DangDevVH.apk`: v1.3.11, 3,565,303 bytes, SHA-256 `8C39C8D792FEBAA4B7E3C5D17F545AA3B75A72E748FAF8A7F9E897EE25FBF10F`.
- WuWa `Wuwa/version.json`: nội dung 3.7.2 (hotfix lời bài hát), khác số update 1.2.0 trong thông báo về game khác.
- WuWa `Wuwa/dlls/winhttp.dll`: 81,408 bytes, SHA-256 `E757622BB86041860CEA04EB164D298C21B5AC3565EF7B44C0D6126939F4E083`.
- Manifest trên Hugging Face đã xác nhận SHA launcher Windows và Android.
- Cả launcher Windows và DLL `winhttp.dll` không có chữ ký Authenticode; việc này không tự nó chứng minh file nguy hiểm.
- Android APK có chứng chỉ chữ ký tự ký `CN=Dang Nguyen` và mã gói `com.wuwavh.launcher`.

## Mã thực tế của launcher Windows

Đã tách .NET single-file, dịch ngược các lớp `DangDevVH.MainWindow` và `DangDevVH.LauncherBridge` chỉ để nghiên cứu; không thực thi launcher.

- `RunInstallation`: ghi PAK `WuWaVH_99_P.pak` vào `Client/Binaries/Win64/wuwaVietHoa` và tải `winhttp.dll` vào `Client/Binaries/Win64`. Thêm `Default_font_99_P.pak` nếu chưa có.
- `RemoveWuwaInstallation`: xoá cả `winhttp.dll` **và `version.dll` trong Win64 nếu chúng tồn tại, không kiểm tra chủ sở hữu/hàm băm**, đồng thời xoá toàn bộ `Win64/wuwaVietHoa`.
- WAHU 3.0.17 dùng `version.dll`/`verorg.dll`/`WuWaVH.dll` và `Paks/~WuWaMods/WuWaVH_99_P.pak`.
- Vì vậy gỡ Đặng Dev có thể xoá loader WAHU; dán đè Client WAHU không tự loại bộ WinHTTP/PAK cũ. Khả năng nạp hai PAK tuỳ cách game load; **chưa tái hiện lỗi game thực tế**.
- Bản fork `TitoTFP/WuwaID` từng dùng `Win64/wuwaIndonesia`, khác tên thư mục chính thức hiện tại.

## CompatGuard DEV được cập nhật

- Kiểm tra `Win64/wuwaVietHoa` (chính thức), `Win64/wuwaIndonesia` (fork), hoặc `winhttp.dll` có SHA đúng với DLL WuWa đã xác minh.
- Chặn `InstallAsync` và `UpdateTranslationPakAsync` **trước khi tự dọn mod**; không xóa hoặc ghi đè file khi bị chặn.
- Có công cụ quét chỉ đọc `scripts/CHECK_WUWA_MOD_CONFLICT.ps1` (Win64, Paks, marker, hash loader). Có thể xuất JSON ra ngoài thư mục game.
- `dotnet test` toàn module cài đặt: **99/99 PASS**, build Windows x64 DEV đã có; chưa test chạy game thật.
- Bản 3.0.17 Stable và gói Client EN dán tay trên GitHub **không có preflight DEV mới**. Gói dán tay còn có khả năng để lại file bên thứ ba.
- Không sử dụng DLL Đặng Dev trong WAHU, không sao chép bộ cài bên ngoài, không publish Stable/chạy trên game khách.

## Khuyến nghị vận hành

Dùng **một** bộ Việt hoá trong một game tại một thời điểm. Trước khi gỡ Đặng Dev bằng launcher, lưu ý chức năng uninstall của launcher có thể xóa `version.dll` WAHU. Sau khi dọn có kiểm soát, có thể cần dùng app WAHU để cài lại loader/PAK WAHU.

Phần còn lại để nghiệm thu: log từ máy người chơi, trạng thái DLL và PAK thực tế, runtime game 3.7; không tuyên bố crash đã sửa xong khi chưa có dữ liệu đó.
