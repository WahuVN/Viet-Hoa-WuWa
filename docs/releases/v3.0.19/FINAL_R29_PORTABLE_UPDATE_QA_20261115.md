# VHWuWa PC v3.0.19 + R29 — kết quả QA chuẩn bị phát hành (15/11/2026)

**Trạng thái:** Ứng viên release ZIP portable đã build và kiểm thử nâng cấp sandbox; **CHƯA** công bố GitHub Release v3.0.19.

## Phiên bản và dữ liệu
- Ứng dụng/Updater FileVersion: **3.0.19.0**. Không dùng 3.0.20 cho đợt phát hành này.
- Dữ liệu: **R29**, `content/3.8.11/WuWaVH_PC_R29_99_P.pak`, SHA-256 `38527e34ffeb8ef7538206d3d4dbd5cbac54a1e6cd9d0957501035ff41a8e598`.
- Phạm vi game đã xác minh MAY-007: PC game 3.8.0 / resource 3.8.11; UI đăng nhập/cảnh báo hiển thị tiếng Việt. Không khẳng định toàn bộ gameplay, Hán Việt hay font rời đã QA.
- Quy trình R29 chỉ ghi PAK + SIG + Mount phụ, không ghi file chính thức/loader DLL vào Win64.

## ZIP portable đã tạo
- Đường dẫn: `dist/V319_R29_RELEASE_CANDIDATE_20261115/VietHoa-WuWa-v3.0.19.zip`
- Dung lượng: **176.081.320 bytes**; **83 file**.
- SHA-256: `e4023c657bc60894c1965846dbb53394d0f3477062d6b38c3316ddc8060c94bc`.
- File app, updater, gói R29, marker R29-only, editor binary, `editor/project.default.db`, 58 avatar, Fonts/Guides/tools đều đủ. Không có `editor/project.db` để tránh ghi đè dữ liệu user.
- Không đóng kèm 6 PAK/font/loader 3.7 cũ. Không đóng 482 file `editor/build_support` của 3.4–3.7 vì không dùng cho nhánh R29; chúng **không phải avatar**.
- Receipt và SHA256SUMS nằm cùng thư mục ứng viên. Gói này được dựng từ ZIP public v3.0.18 + ZIP R29 QA đã đối chiếu digest, không phụ thuộc file name suy đoán.

## Kiểm thử thực tế
- Build Release app/updater: **3.0.19.0**, không lỗi.
- `dotnet test VHWuWa.sln -c Release`: **172/172 PASS** (Core 56, Infrastructure 116).
- ZIP QA source SHA và PAK R29 SHA kiểm tra đầy đủ, 83 file round-trip ZIP.
- Nâng cấp sandbox bằng đúng `VHWuWa.Updater.exe` public v3.0.18:
  SHA updater `a1903ea4eec2baa94387a22d527b5e758fc5bcb2a7fff5b6a6015c4b386c58a4`.
  Updater trả exit 0 qua `--no-relaunch` (QA), hai EXE v3.0.19.0, PAK R29 đúng SHA.
- Kiểm chứng `editor/project.db` do user chỉnh sửa và file riêng không bị overwrite/delete khi nâng cấp.
- Gọi trực tiếp migration C#: **6/6** PAK/font/loader 3.7 có hash v3.0.18 chính thức được backup ra ngoài thư mục app rồi loại khỏi thư mục đang hoạt động; file lạ giữ nguyên. Sandbox đã dọn sau khi PASS.
- Trên cửa sổ R29 thực: `v3.0.19`; Hán Việt radio **disabled**; cài font rời **disabled**; nút Cài **disabled** khi không tìm thấy game hợp lệ. Menu đồng bộ màu xám/Logo nguyên bản.

## Các điểm còn chưa nghiệm thu để công bố public
- Vòng UI đầy đủ 3.0.18 public → chấp nhận cập nhật GitHub → 3.0.19 public chỉ có thể test sau khi GitHub Release 3.0.19 tồn tại. Bài offline đã dùng updater thật nhưng chế độ `--no-relaunch` để tránh tác động Desktop/LocalAppData.
- Chưa thử gói ZIP ứng viên này trực tiếp trên MAY-007 (máy hiện dùng QA EXE 3.0.20, không hạ phiên bản cưỡng bức).
- QA R29 thực tế mới xác nhận đăng nhập/cảnh báo và một số câu dịch; chưa đầy đủ toàn bộ font, Hán Việt, gameplay hay mọi phiên bản resource.
- EXE/updater chưa có Authenticode; có thể gặp SmartScreen trên máy Windows mới.
- Mất điện đúng thời điểm Windows thay thư mục app vẫn là trường hợp phục hồi thủ công khó xử lý trong updater v3.0.18 đã phát hành. Không tuyên bố rollback 100% sau mất điện.

## Lệnh xác nhận nguồn và xây dựng lại ứng viên
- `dotnet test VHWuWa.sln -c Release`
- Publish app/updater win-x64 self-contained single-file với `-p:Version=3.0.19`.
- `python tools/build_v319_r29_release.py --help`
- `python tools/test_v319_real_v318_updater.py --help`
- `python tools/verify_v319_upgraded_sandbox.py --help`

Không push tag stable, tạo GitHub Release public, thay file game trên MAY-007 hay đăng thông báo cộng đồng trong lượt QA này.
