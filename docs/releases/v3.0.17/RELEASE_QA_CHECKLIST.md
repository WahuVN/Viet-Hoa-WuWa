# Checklist phát hành / QA — v3.0.17 (CHƯA THỰC HIỆN GO)

Tất cả ô dưới đây là **điều kiện cho bản 3.0.17 chính thức**, không tự đánh dấu PASS theo kết quả unit test source 3.0.16.

## A. Quản lý phiên bản & source
- [ ] Review toàn bộ Git diff worktree và file untracked; định danh bản build chính xác, tránh lẫn source khác.
- [ ] Chốt danh sách các thay đổi thật sự mới so với GitHub PC 3.0.16 theo `SOURCE_CHANGE_MATRIX.md`.
- [ ] So sánh DB/PAK EN & Hán Việt bằng `dbfile/table/key`, log mọi sửa text 3.7 đã duyệt.
- [ ] Lock nội dung thoại, xưng hô nhân vật qua CN/JP/EN nếu cần; kiểm placeholder/tag/newline, unicode và biến Rover nam/nữ.
- [ ] Chốt trường hợp giữ PAK không đổi: ghi rõ chỉ update installer/font nếu dữ liệu dịch không có diff.
- [ ] Sau GO mới bump version trong `Directory.Build.props`, app/updater, README, CHANGELOG chính.
- [ ] Review shell scripts tránh nhầm `~mods` (mod ngoài) với `~WuWaMods` (WAHU), đường dẫn phát hành và asset.

## B. Build / XAML / font
- [ ] `dotnet test VHWuWa.sln -c Release`: tất cả PASS, 0 Skip ngoài chủ đích; test XAML ResourceValidation.
- [ ] `dotnet build VHWuWa.sln -c Release`: 0 error, 0 warning.
- [ ] Publish app + updater win-x64 single-file self-contained đúng file version 3.0.17.
- [ ] Test mở GUI thật: không XamlParseException, icon/InfoBar/nút không crop, update panel đúng trạng thái.
- [ ] Kiểm `Fonts/fonts.json` có đúng 73 mục; 72 font gốc giữ nguyên, Lexend mặc định 650.
- [ ] Lexend variable trục 100–900, slider tăng theo 25; static font vô hiệu, không làm giả độ đậm.
- [ ] Export TTF instance Lexend 650 & 700 từ `font-weight-instance.exe` trong build folder máy sạch; license OFL có sẵn.
- [ ] Test live preview tiếng Việt có dấu, đổi cỡ chữ, đổi weight liên tiếp, font ngoài và cache/delay.
- [ ] Test tạo, đóng gói, áp dụng font TTF vào PAK thực và rollback/gỡ font an toàn.

## C. Cài đặt / dọn xung đột / gỡ
- [ ] Folder game chính xác có `Client\Binaries\Win64\Client-Win64-Shipping.exe` và `Client\Content\Paks`.
- [ ] Cài PAK WAHU vào `~WuWaMods` cùng SIG + marker, loader 3 DLL, font PAK nếu chọn.
- [ ] Quét mod gốc Paks `*_P.pak`, thư mục `~mods`/`Mods`/`_mods`/nested, proxy DLL Win64/WWMI/3DMigoto/UE4SS.
- [ ] Giữ `pakchunk*` + file manifest official, tên file WAHU quản lý và phần mở rộng backup; không đi vào symlink/junction.
- [ ] Test marker hỏng, legacy thiếu marker, riêng một DLL trùng tên; không xóa nhầm WAHU.
- [ ] Manual **Xóa mod xung đột** hiển thị danh sách, Yes mới xóa; mặc định No.
- [ ] Manual **Cách ly mod cũ** giữ manifest/SHA, rollback nếu thất bại; mở đúng vùng cách ly gần nhất.
- [ ] AUTO CLEAN khi cài/quick update có cảnh báo xóa vĩnh viễn và báo số file; không hứa tự quarantine.
- [ ] Preflight source/destination và quyền, game-running, locked file, symlink; fail trước xóa file đầu tiên.
- [ ] `version.dll` ngoài được backup và hash; `version_goc.dll` giữ lại và không auto-restore proxy chưa xác minh.
- [ ] Reinstall/update idempotent, hậu kiểm source+marker SHA cho PAK/SIG/font/loader; success chỉ khi verify PASS.
- [ ] Uninstall yêu cầu marker/legacy, cảnh báo nếu file bị ghi đè, không xóa mod khác.
- [ ] Busy UI khóa cài/gỡ/xóa/cách ly/tải/quét trùng lệnh, progress/cancel hợp lý.

## D. Runtime Wuthering Waves 3.7
- [ ] Test thực client Kuro/Steam, **mở bằng launcher chính thức**, không lỗi `kuro: Use launcher to start game!`.
- [ ] Kiểm ổn định DX11, DX12, start/restart/game exit, không crash do loader/xung đột cũ.
- [ ] Xác minh PAK EN và Hán Việt nạp thực tế, không raw key/mất chữ/font trắng.
- [ ] Kiểm font Lexend 650/700 đúng sắc độ, dấu tiếng Việt, xuống dòng UI, placeholder.
- [ ] Soát một số scene 3.7, thoại Rover/quan hệ/xưng hô nhạy cảm, kiểm nội dung trên PC thật; Mobile QA độc lập nếu cần.
- [ ] Lưu log, screenshot, hash và kết quả PASS/FAIL + blocker; không đánh dấu client PASS nếu chưa chạy.

## E. Updater 3.0.16 → 3.0.17
- [ ] Test `scripts\test-client-self-update.ps1` với đúng current/next version và 3.0.17 package thật.
- [ ] Kiểm GitHub asset exact-name + digest SHA-256, thiếu/sai SHA không chạm bản cũ.
- [ ] Đảm bảo handshake updater sẵn sàng trước shutdown app, candidate transaction, health-check file đúng version.
- [ ] App mới crash/timeout -> rollback toàn bộ, giữ config người dùng, không còn file chỉ thuộc bản mới.
- [ ] Rà phiên bản app/updater, version marker và tải Hán Việt đúng release tag.

## F. Release / BotDis
- [ ] Player ZIP chính thức + 2 PAK + SHA256SUMS (nếu build theo quy trình), checksum thực khớp GitHub digest.
- [ ] Không ghép App-Dịch bản khác version bằng đổi tên; Android cập nhật độc lập.
- [ ] Đồng bộ README/download links, CHANGELOG, docs; bỏ nhãn DRAFT sau khi đủ gates.
- [ ] GitHub tag/release chỉ sau khi người dùng cấp lệnh GO phát hành.
- [ ] Khi public mới đổi PC block `D:\TOOL\Bot Dis\WuWaVHBot\release-manifest.json`.
- [ ] Sửa hardcode announcement cũ, test `npm run check` + `CHECK_RELEASE.bat` + Discord dry-run 7 kênh.
- [ ] Kênh `📢・thông-báo` đúng đích, chống gửi trùng, không @everyone/@here/role ping.
- [ ] Có GO gửi Discord riêng mới sync+announce và hậu kiểm `/download`/`/guide`/`/faq`/`/bao-loi`/`/gop-y`.

### Evidence hiện có (không được tính thay cho v3.0.17)
- Baseline source 3.0.16: 41 Core + 89 Infrastructure = **130/130 PASS**.
- Bundle QA v3.0.16 AUTO CLEAN + Lexend đã được zip/test SHA; compiler Lexend 650 QA thành công.
- **CHƯA** có game QA runtime, updater E2E 3.0.16→3.0.17, phiên bản app 3.0.17 hay digest v3.0.17.
