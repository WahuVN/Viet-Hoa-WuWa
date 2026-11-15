# PC v3.0.19 R29 — bỏ khóa sai phiên bản EXE game (15/11/2026)

## Nguyên nhân
Trong `R29MountedPak.GameCompatible()`, app trước đây chặn cài nếu EXE của game không có SHA-256 đúng bản MAY-007 hoặc nếu thiếu manifest resource chính xác 3.8.11. Vì vậy một EXE thay đổi vẫn có layout Mount hợp lệ bị báo **"phiên bản game chưa khớp bản đã thử nghiệm"** và nút Cài bị khóa. Đây là lỗi logic của công cụ, không có bằng chứng PAK R29 bị lỗi.

## Thay đổi có giới hạn
- Bỏ điều kiện EXE SHA-256 cố định, bỏ điều kiện ManifestResource_3.8.11.txt và so phiên bản resource trong bước cài R29.
- Vẫn yêu cầu đúng thư mục game có `Client/Binaries/Win64/Client-Win64-Shipping.exe`, tệp Mount chính thức và SIG ngôn ngữ. Nếu thiếu, hiển thị cụ thể **"Không tìm thấy đủ tệp Mount/SIG"**, không báo "sai phiên bản".
- Tìm SIG ngôn ngữ mới nhất theo thư mục `Lang_en/<phiên bản>/pakchunk10-WindowsNoEditor_P.sig`, không ghim vào 3.8.6.
- Chỉ sao chép PAK R29 đã xác minh SHA-256 và SIG, viết sidecar Mount do VHWuWa quản lý. Không thay stock MountLang_en.txt, stock PAK/DLL hay xóa mod khác; không gỡ tệp không thuộc R29.
- Rút gọn hai vùng trạng thái Trang chủ/Cài đặt thành **"Gói Việt hóa R29 đã sẵn sàng"**; bỏ cảnh báo sai phiên bản.
- Chỉnh README và marker đóng gói ZIP cho đúng: không khóa theo bản EXE/resource; `appVersion` vẫn 3.0.19, `translationRelease` vẫn R29.

## QA thực tế trên WIN3
- Unit test Release **174/174 PASS** (56 Core, 118 Infrastructure), gồm hai bài mới kiểm tra EXE đổi hash và ManifestResource_3.8.12 hay SIG từ 3.9.1.
- Test tích hợp trên thư mục game giả lập bằng **PAK R29 thật** (125.532.999 byte): SHA PASS, EXE khác MAY-007 PASS, resource 3.8.12 PASS, SIG 3.9.12 PASS, cài Mount thành công, nhận diện đã cài, gỡ an toàn, stock Mount/SIG và mod riêng nguyên vẹn.
- Mở ZIP ứng viên bản mới trên WIN3: cửa sổ PC chạy; Trang chủ hiện **"Gói Việt hóa R29 đã sẵn sàng. Hãy chọn thư mục game."**, không còn dòng **"phiên bản game chưa khớp bản đã thử nghiệm"**.
- PAK R29 gốc không thay đổi, SHA-256 `38527e34ffeb8ef7538206d3d4dbd5cbac54a1e6cd9d0957501035ff41a8e598`.
- Gói ZIP portable v3.0.19 chứa **84 file**, 176.083.488 byte, SHA-256 `7e1b4a07399d548b3aea1e881442b1909e69d5872d637e873d17c21dfa27cabb`.
- Dùng updater công khai 3.0.18 (SHA đã kiểm) nâng ZIP mới 3.0.19 trong sandbox **PASS**; giữ user editor DB và file riêng, backup các tệp 3.0.18 cũ.

## Giới hạn
- Bỏ khóa số phiên bản **không bảo đảm** tất cả các bản game đều nạp được PAK nếu thay đổi cấu trúc Mount/encoding.
- Đã có biên bản MAY-007 chạy R29 trước đó (cảnh báo/đăng nhập bằng tiếng Việt). Build không ghim EXE mới này mới được test tích hợp trên **thư mục giả lập**, chưa vào gameplay thật ở mọi phiên bản.
- Không sửa gameplay, PAK, font, update logic, GitHub Latest hay Discord production. Release GitHub vẫn **Draft**, không Publish tự động.
