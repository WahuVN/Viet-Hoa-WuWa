# WuWa Việt Hóa PC 3.0.19 — cài lại / dọn mod tự động (15/11/2026)

## Cơ chế mới
- Bấm **Cài Việt hóa**: ứng dụng dùng cùng operation lock, kiểm gói R29 và thư mục game, tự dọn mod xung đột đang hoạt động rồi cài R29 (không hỏi xác nhận từng mod).
- Dọn các file bên ngoài thuộc thư mục mod Paks, Win64 loader, mod WAHU 3.7 cũ (kể cả marker cũ bị hỏng), sidecar Mount ngoài và cặp PAK/SIG được tham chiếu, Mount R29 hỏng/còn sót.
- **Không xóa game gốc**: pakchunk, SIG stock, MountLang_en và các bản sao lưu *.bak/version_goc.dll không thuộc phạm vi dọn.
- Trước khi xóa khỏi game, công cụ tạo bản sao tại `%LOCALAPPDATA%\VHWuWa\Quarantine` cùng manifest SHA-256 để khôi phục nếu cần. Nếu dọn lỗi, rollback các file đã dời; nếu cài lỗi sau khi dọn, cố gắng tự khôi phục.
- **Cài lại Việt hóa** vẫn bấm được khi R29 đã cài. **Cập nhật nhanh** cũng dùng cùng luồng tự dọn mod vừa chèn. Không khóa theo SHA EXE hoặc bản resource game.
- Khi thiếu PAK R29 hoặc thiếu thư mục Client/Mount/SIG để thực hiện an toàn, báo đúng lý do **trước khi đụng file cũ**.

## QA trên WIN3
- Mã nguồn sau sửa: 174/174 unit tests PASS (56 Core, 118 Infrastructure); app tự đóng gói `3.0.19.0`.
- QA tích hợp dùng **PAK R29 thật**, thư mục game giả lập có EXE game khác MAY-007, các tệp và mod giả lập:
  - Bản WAHU 3.7 không marker hợp lệ; PAK/SIG/font, ba loader Win64.
  - WinHTTP/DangDev, mod Paks gốc và thư mục ~OtherMods.
  - Mount sidecar bên ngoài kèm cặp PAK/SIG trong Resources.
  - R29 chưa hoàn chỉnh (PAK/SIG/sidecar stale) đang chiếm đích.
- Kết quả **18/18 tệp mod cũ được dọn khỏi trạng thái hoạt động**, có bản sao trong Quarantine; PAK R29 mới được cài/nhận diện; các stock pakchunk/Mount/SIG và `version_goc.dll` nguyên vẹn.
- Cài lại / UpdateTranslationPakAsync khi xuất hiện mod mới: PASS, chỉ dọn thêm đúng một mod; gỡ R29: PASS, không xóa các file khác.
- Gói R29 bị thiếu: PASS, không xóa mod đang có.
- Không khởi động/ghi vào game thật trên WIN3/MAY-007 trong lượt QA này.

## Gói ứng viên
- `dist\V319_R29_RELEASE_CANDIDATE_20261115\VietHoa-WuWa-v3.0.19.zip`
- 84 tệp, 176.084.745 byte; SHA256 **f38648f4d836413d2560b5bcc2ce36d2cd0ecfb8cfd393f4c947bbc116a5fe9e**.
- PAK PC R29 SHA256 **38527e34ffeb8ef7538206d3d4dbd5cbac54a1e6cd9d0957501035ff41a8e598** (không đổi).
- ZIP nội dung CRC/EXE + Updater/guides: PASS.
- Test dùng **updater công khai 3.0.18 thật** nâng sandbox lên ZIP mới 3.0.19: PASS. User editor DB và file riêng không đổi; backup 6 asset legacy thành công.
- Lượt test updater không gọi mở GUI cuối để tránh ghi setting/ngắt app người dùng; chưa coi là nghiệm thu cập nhật công khai khi GitHub Release còn Draft.

## Ranh giới
- Tự dọn các *mod có thể nhận diện trong đường dẫn mod chuẩn*. Không theo symlink ra ngoài game, không xóa các file chính thức, và không xóa bất kỳ file không xác định ngoài vùng mod.
- Không thể cam kết mọi game 3.5/3.8/3.9 đều mount thành công nếu game đổi cấu trúc runtime, dù không còn khóa theo số phiên bản.
- Không sửa bot Discord production, không Publish v3.0.19 trước khi QA phát hành hoàn tất.
