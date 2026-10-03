# VHWuWa v3.0.16

Bản kế tiếp sau GitHub public v3.0.15, gộp hotfix launcher Wuthering Waves 3.7 và đợt hardening clean-install/mod conflict vào một patch release duy nhất.

## Sửa khởi chạy Wuthering Waves 3.7

- Nút mở game dùng `launcher.exe` Kuro chính thức ở thư mục cha của `Wuthering Waves Game`.
- Không còn mở trực tiếp wrapper/client gây lỗi `kuro: Use launcher to start game!`.
- Bỏ **Chế độ C# thử nghiệm** khỏi UI vì launcher chuẩn 3.7 không sử dụng cờ đó.
- Chống mở nhiều VHWuWa cùng lúc để tránh state cài/gỡ lệch.

## Clean install / mod conflict

- Kiểm tra payload cần cài trước khi dọn bất kỳ file nào.
- Mod/VH ngoài đã được detector xác định là xung đột được dọn trực tiếp trước khi cài/cập nhật; auto-flow không tạo Quarantine.
- Không còn khóa nút Cài chỉ vì phát hiện mod ngoài; clean-install tự xử lý trong operation lock.
- Nhận diện thêm mod/VH ngoài để dọn xung đột: WWMI/3DMigoto, UE4SS, proxy DLL phổ biến, `ShaderFixes`, `ShaderCache`, `wuwaVietHoa.dll`, `wuwaVietHoa_Wahu_SDK.dll`, `wuwaVietHoa_SDK.dll`, `wuwaVietHoa_VH.dll`, `wuwaVietHoa.off`, `SigPakV2.dll`, cùng thư mục `Win64\wuwaVietHoa\`. Các tên `wuwaVietHoa*` này **không phải lineage managed của VHWuWa**.
- File game official được bảo vệ bằng `OriginResource.json`; nếu có nhiều manifest, ưu tiên version game cao nhất.
- Với WuWa 3.7, không phục hồi `version_goc.dll` proxy cũ nếu manifest không xác nhận `version.dll` là file official.
- Gỡ Việt hóa dọn file/log do VHWuWa quản lý nhưng không xóa file lạ ngoài marker.

## Version artifact

- **Player / VHWuWa:** 3.0.16.
- **App-Dịch / WAHU Community:** 3.0.16, rebuild từ source hiện tại; portable dùng ConfigDB authority `_dbcfg` 3.7 và không còn đóng gói snapshot `analysis/extracted_*` cũ.
- **PAK EN/Hán Việt:** giữ payload QA của 3.0.15; không đổi nội dung chỉ vì installer tăng patch.

## Kiểm tra đóng gói / cleanup

- Theo yêu cầu đợt cleanup cuối này **không chạy lại unit/automated test**.
- `dotnet build VHWuWa.sln -c Release`: PASS, 0 warning / 0 error.
- Python AST + PowerShell parser cho pipeline WAHU/Player vừa sửa: PASS.
- Player và App-Dịch ZIP: foreign-artifact scan = 0 file.
- Player chứa đủ EN + Hán Việt + 3 loader canonical; build script bắt buộc đủ cả hai PAK.
- App-Dịch chứa đủ `project.db`, `_dbcfg` 3.7, font canonical và 3 loader canonical.
- Player ZIP SHA-256: `03c6bfa9f2653832a6b967e4c8d29a49e39b255147b18adc335f10f02dc3df70`.
- App-Dịch ZIP SHA-256: `064ac8c3dbd00a92d8e7c07c6e3f3f6ac69971fbf18040e3b73bba1030680da4`.
- EN PAK SHA-256: `1e647329564fd159c8f94849b84cfe2b2550703e4354093022416c4be951fa28`.
- Hán Việt PAK SHA-256: `2fb7e68a11c0dd8190887751a93f56aef12fde5316929a786585ea46290820b3`.
