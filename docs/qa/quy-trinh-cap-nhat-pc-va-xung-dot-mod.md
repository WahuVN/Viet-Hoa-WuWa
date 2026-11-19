# Quy trình cập nhật Việt hóa PC và kiểm soát xung đột mod

**Tài liệu kỹ thuật nội bộ – 19/11/2026.** Áp dụng cho trình cài Việt hóa Wuthering Waves trên PC. Mục tiêu: nâng cấp có thể kiểm chứng, tránh xóa nhầm file game, không cài chồng loader/PAK cũ, bảo toàn bản sao và không phát hành khi còn lỗi khởi động.

## 1. Trạng thái phải phân biệt

- **Ứng dụng (EXE):** phiên bản góc ứng dụng, ví dụ `v3.0.22`. Cài PAK mới **không** có nghĩa EXE được nâng cấp.
- **Bản dịch đã cài:** version từ marker cũ, hoặc release `R29` sau khi nhận diện được bộ PAK/SIG/Mount. Badge `v3.0.16` có thể chỉ là marker cũ, **không đủ bằng chứng** ZIP mới chứa PAK cũ.
- **Dữ liệu game và dữ liệu đóng PAK:** hai phiên bản phải đối chiếu riêng. PAK R29 công khai được xây từ resource **3.8.11**, còn một số máy đã có manifest game **3.8.12**. **Chưa có chứng cứ runtime tương thích**; không tự ghi PASS.
- **Mã nguồn và bản đã phát hành:** bộ tự dọn nâng cấp ở commit `e067d684` (19/11) và vá tiếp theo trong nhánh này **không nằm trong ZIP công khai v3.0.24 (18/11)**. Không khẳng định người dùng ZIP cũ có tính năng mới.

### Cơ chế cũ theo lịch sử

| Giai đoạn app PC | Cách nạp | Lỗi chuyển tiếp thường gặp |
| --- | --- | --- |
| v3.0.10–3.0.18 | PAK/font trong `Client/Content/Paks/~WuWaMods`; proxy/loader trong `Client/Binaries/Win64` | Loader/PAK tồn dư, mod khác ghi đè DLL, game đổi tài nguyên |
| v3.0.19–3.0.21 | R29 ở `Client/Saved/Resources/3.8.0/wahuviethoa` + `Mount/wahuviethoa.txt` | Hai cơ chế cũ/mới cùng tồn tại, sidecar/sig thiếu |
| v3.0.22–3.0.24 | R29 + font Lexend qua `wahufont` + `Mount/wahufont.txt` | Font cũ cùng nạp, trạng thái cài/EXE cũ gây hiểu lầm, tương thích game chưa chứng nhận |

**Không xóa hoặc sửa bừa các mod của người dùng.** Bộ cài mới có thể **vô hiệu hóa mod xung đột** trước khi cài nhưng bắt buộc sao lưu nguyên byte, ghi manifest và SHA-256 để có thể khôi phục. Hành động dọn mod **không** đồng nghĩa xóa vĩnh viễn bản sao.

## 2. Quy trình an toàn chuẩn: Preflight → Scan → Backup → Install → Verify → Runtime

### Bước 0 – kiểm tra bản nguồn và phạm vi

1. Mở đúng repo `WahuVN/Viet-Hoa-WuWa`, ghi commit SHA, trạng thái `git status`, EXE version, release tag, ZIP SHA-256, mã PAK SHA-256. Không lấy version trong badge marker làm EXE version.
2. Tách **PC/Android**, **bản ứng dụng/bản dịch** và **resource game/resource đóng gói** thành các trường riêng. Không gộp hai platform hoặc tăng version vì chỉnh tài liệu.
3. Không đóng gói source khác commit hoặc nhét PAK cũ bằng tên ZIP mới. Asset trên GitHub release là bất biến: không ghi đè ZIP cũ rồi giữ nguyên tag/checksum.
4. Phải xác định release mới có thật sự cần thiết; gộp các bản sửa và không tạo bản phát hành liên tục.

### Bước 1 – tiền kiểm bắt buộc, không thay đổi game

1. **Đóng hẳn game và launcher**, kể cả Steam/Kuro launcher và tiến trình `Client-Win64-Shipping.exe`; xác nhận trong Task Manager. Cài/gỡ R29 mới phải từ chối khi game/launcher chạy.
2. Xác minh `GameRoot`: có `Client/Binaries/Win64/Client-Win64-Shipping.exe`, `Client/Content/Paks` và SIG gốc hợp lệ. Không chấp nhận đường dẫn symlink/junction trỏ ra ngoài.
3. Xác minh PAK trong bộ cài có đúng byte count + SHA-256 và font thực sự là PAK hợp lệ. R29 PC SHA-256 PAK hiện biết: `38527E34FFEB8EF7538206D3D4DBD5CBAC54A1E6CD9D0957501035FF41A8E598`.
4. Xác minh phiên bản game/resource tương thích bằng bằng chứng chạy game, không suy luận chỉ từ tên thư mục `3.8.0`.
5. Đảm bảo đủ quyền đọc/ghi, đĩa còn đủ dung lượng **cho bản sao lưu và staging**, không đụng production; kiểm tra antivirus/quyền file nếu copy thất bại.
6. Tạo snapshot/báo cáo **trước khi cài**: các file trong Win64, Paks, ~WuWaMods, thư mục mod khác và Resources/3.8.0/Mount. Lưu đường dẫn, kích thước, SHA, số file, tình trạng game. **Không tự đưa file log/token cá nhân vào tài liệu công khai.**

### Bước 2 – quét xung đột (read-only)

Các vị trí cần quét: `Client/Content/Paks`, `~WuWaMods`, `~mods`, các thư mục mod được phát hiện, `Client/Binaries/Win64` (proxy DLL/loader, thư mục mod), `Client/Saved/Resources/3.8.0/Mount` và bộ `wahuviethoa/wahufont`. Xem marker `vhwuwa_install.json` và manifest game `OriginResource.json`.

**Nhóm phải giữ nguyên:** `Client-Win64-Shipping.exe`, PAK/SIG `pakchunk*` gốc, mọi file được manifest game chính thức ghi nhận, `MountLang_*.txt`, `MountOption_Pack_*.txt`, `MountLauncher.txt`, file stock SIG, backup `version_goc.dll`/`*.bak`/`*.orig`/`*.old`, symlink không thể xác minh, dữ liệu game nằm ngoài phạm vi dọn.

**Nhóm có thể vô hiệu hóa sau sao lưu:** PAK/SIG mod bên ngoài không thuộc game gốc, proxy/loader ngoại lai đã nhận diện, sidecar Mount của mod thứ ba, bộ dịch/font WAHU cũ còn hoạt động; chỉ file đã thuộc kế hoạch và xác minh đường dẫn nằm trong `GameRoot`.

**HOLD thay vì tự xóa:** marker lỗi/không đọc được; game path không chắc; file chính thức không xác minh được; liên kết ngoài; file bị lock; thư mục quét lỗi quyền; thiếu SIG; manifest game đổi không rõ; không có bản sao lưu có SHA. Bộ dò không chứng minh được mọi plugin/injector tồn tại – không hứa “100% sạch mod” dựa vào đếm file.

### Bước 3 – bản sao nguyên byte, rồi mới vô hiệu hóa

1. Tạo bản sao **ngoài thư mục game**. Trình cài dùng `%LOCALAPPDATA%\VHWuWa\Quarantine\<timestamp>`; script sửa thủ công dùng `%LOCALAPPDATA%\WAHU\WuWaConflictBackups` (**hai định dạng backup khác nhau, không đổi chéo lệnh Restore**).
2. Sao chép tất cả file trong danh sách; xác minh SHA-256 **từng file**; ghi `quarantine-manifest.json`/manifest của script chứa đúng `GameRoot` và đường dẫn tương đối.
3. Chỉ khi **toàn bộ sao lưu PASS** mới xóa file xung đột khỏi thư mục game. Không quét/chuyển file stock hoặc sao lưu cũ. Nếu có lỗi giữa chừng, cố gắng hoàn tác và giữ bản sao để khôi phục bằng tay.
4. Không đặt Quarantine trong game; không lưu trực tiếp ở thư mục Mount; không tự xóa backup khi cập nhật xong.
5. **Cảnh báo quan trọng:** nếu cài PAK/font thất bại *sau khi đã cách ly mod*, mã hiện tại **chưa tự phục hồi tất cả mod**. Dừng, lấy đúng đường dẫn Quarantine và kiểm tra/Restore bằng lệnh hoặc UI sau khi hoàn tác trạng thái R29. Không tự báo đã rollback trọn vẹn.

### Bước 4 – cài hoặc cập nhật

- Với R29: sau bước tiền kiểm và dọn mod, cài PAK `wahuviethoa/WuWaVH_99_P.pak`, SIG và `Mount/wahuviethoa.txt`; kiểm tra SHA và nội dung sidecar. Cài font trong `wahufont` + receipt + sidecar `wahufont.txt`.
- **Hiện R29 luôn chọn font Lexend 660** dù tham số `withFont=false`; không ghi trên UI rằng tùy chọn tắt font có hiệu lực cho R29. Muốn kiểm thử không font phải sửa cơ chế đó riêng và QA lại.
- Không đổi/sửa stock `MountLang_*`, `MountOption_Pack_*`, `MountLauncher.txt`, SIG game, các PAK gốc và loader stock. Không cài chồng cấu trúc v3.7 cũ.
- Gói legacy (trước R29) có quy trình loader khác. Không dùng kết quả unit test legacy để chứng nhận R29.
- Chỉ cho người dùng **Cài thành công** khi hậu kiểm file đã ghi hoàn tất; không coi file tồn tại hoặc đếm mod đã xóa là game sẽ mở được.

### Bước 5 – hậu kiểm không được bỏ qua

- `R29MountedPak.IsInstalled` xác minh **PAK+SIG+sidecar đồng bộ**. `R29FontMountedPak` phải xác minh receipt/font/sidecar.
- Scan lần hai không còn bộ dịch cũ hay mod nằm trong danh sách xung đột. Nếu vẫn còn → **FAIL/HOLD**, hiển thị đường dẫn, không mở game.
- So SHA-256 file stock trước/sau; không đổi EXE game, PAK/SIG stock, Mount game, file người dùng không thuộc kế hoạch.
- Nếu cài lại cùng bản, kết quả không tạo nhiều mount/PAK bản dịch. Test gỡ và cài lại nhiều lần, copy thất bại giữa chừng, file read-only, marker hỏng.
- Quarantine phải giữ lại và chức năng Restore không được ghi đè mod khác được cài sau đó.

### Bước 6 – kiểm thử khởi động game thật (bắt buộc để RELEASE)

Kiểm thử trên **cài đặt sạch** và **nâng cấp từ máy có bản 3.0.16/3.0.18/3.0.19/3.0.22/3.0.24**, có/không có mod phụ; Kuro và Steam; DirectX 11 và 12 khi có hỗ trợ; game đúng phiên bản tài nguyên dự kiến.

Với mỗi case phải ghi: launcher mở → tiến trình game xuất hiện → vào menu/login → vào thế giới → hiện chữ Việt/font → đóng/mở lần 2 → gỡ Việt hóa → game gốc vẫn chạy → cài lại. **Kiểm thử filesystem hoặc unit test không thay thế được kiểm thử runtime trên game thật.**

Nếu **cài bản dịch không mở được game, gỡ lại chạy**: đánh dấu FAIL, giữ log game/launcher cùng mã lỗi, đối chiếu SIG/PAK/Mount và bộ mod đã cách ly, thử không font, thử R29 disabled, kiểm tra lệch resource. Không kết luận “do mod” nếu chưa có reproducer; báo cáo game phiên bản, exe app, resource data, hash của R29 và kết quả hai lần mở.

## 3. Khôi phục và xử lý sự cố

| Triệu chứng | Hướng xử lý, không đoán nguyên nhân |
| --- | --- |
| App hiện `v3.0.22`, badge `v3.0.16` | EXE cũ + marker legacy cũ có thể cùng tồn tại; kiểm tra EXE/ZIP và SHA PAK độc lập |
| Cài xong game không mở; gỡ thì mở | Dừng phát hành; lấy log runtime, kiểm tra mod/loader/PAK/SIG/font/Mount, game resource 3.8.11 vs 3.8.12; không tự kết luận PASS |
| Thiếu SIG gốc, PAK/Font sai SHA, sai thư mục | Fail-closed trước khi dọn mod; sửa package/game path hoặc cập nhật nguồn đúng |
| Marker hỏng, DLL không rõ nguồn, symlink | HOLD và sao lưu thủ công; không ghi đè/xóa file không xác minh được |
| Đã tự dọn mod nhưng cài R29 không thành công | Backup còn trong Quarantine; phải phục hồi có đối chiếu, không hứa tự rollback đầy đủ |
| Gỡ xong nhưng file mod cũ quay lại | Kiểm tra có dùng Restore/launcher/mod manager khác; không tự xóa backup đang giữ |
| Gỡ R29 còn file ở mount/font | Chỉ gỡ file WAHU đã xác minh; nếu receipt hoặc sidecar bị thay đổi thì HOLD |
| Cài trên phiên bản game mới | Kiểm thử tương thích và build lại PAK nếu cần; không đổi mỗi số version hoặc chỉ sửa tên file |

**Lệnh chỉ đọc** từ gốc source repo:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\CHECK_WUWA_MOD_CONFLICT.ps1 -GameRoot "D:\Games\Wuthering Waves Game" -OutputJson "D:\QA\wuwa-mod-audit.json"
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\FIX_WUWA_CONFLICT.ps1 -GameRoot "D:\Games\Wuthering Waves Game" -Mode Scan
```

**Thao tác chủ động có sao lưu** chỉ khi đã kiểm tra kết quả quét và đúng bản:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\FIX_WUWA_CONFLICT.ps1 -GameRoot "D:\Games\Wuthering Waves Game" -Mode Repair
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\FIX_WUWA_CONFLICT.ps1 -GameRoot "D:\Games\Wuthering Waves Game" -Mode Repair -Apply
```

Script Repair xử lý **những file WAHU cũ được xác minh**, không đồng nghĩa quét/xóa tất cả mod bên ngoài. Các lệnh DisableFont/DisableR29/Restore được mô tả trong [hướng dẫn khắc phục](kiem-tra-xung-dot-viet-hoa-pc.md). Không chạy script hoặc test trên tài khoản/game đang mở.

## 4. Release gate: FAIL/HOLD là không phát hành

**Chạy cổng kiểm thử nguồn, không đụng game thật**, từ gốc repository:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\QA_TRUOC_PHAT_HANH_PC.ps1 -Mode SourceOnly -R29Pak "D:\QA\WuWaVH_PC_R29_99_P.pak"
```

Script chạy bộ quét PowerShell read-only trong game giả, unit hạ tầng/lõi, build WPF và smoke R29 bằng PAK thật trên thư mục QA tạm; xuất JSON tại `%LOCALAPPDATA%\VHWuWa\QaReports`. Nếu không cung cấp PAK R29 đúng SHA, SourceOnly trả **HOLD/exit code khác 0**. `-Mode Release` **cố ý fail-closed**, kể cả khi có file ghi nhận runtime, để bắt buộc người phụ trách xem log và ký duyệt trực tiếp; không dùng exit code của build để tự cho phép phát hành.

Các cổng bắt buộc và bằng chứng:

| Cổng | Tiêu chí | Nếu chưa chứng minh |
| --- | --- | --- |
| G0 – Nguồn và ZIP | Git SHA/EXE version/ZIP hash/PAK SHA/version tài nguyên khớp, không có file cũ ngoài ý muốn | HOLD |
| G1 – Unit | `dotnet test tests/VHWuWa.Infrastructure.Tests/VHWuWa.Infrastructure.Tests.csproj -c Release` và Core Tests: 100% PASS | FAIL |
| G2 – R29 thật trên fixture | Chạy smoke với R29 PAK thật, kiểm tra quarantine SHA, stock Mount, reinstall/gỡ, game-running guard | FAIL |
| G3 – Cài/gỡ/khôi phục | Ma trận v3.0.16→R29, mod khác, font, proxy, backup, locked files, rollback, marker, Steam/Kuro | HOLD nếu chưa đủ |
| G4 – Runtime | Game thật vào được menu/thế giới và hoạt động sau 2 lần restart trên phiên bản hiện tại, không dịch vẫn mở được | **HOLD** nếu chưa test |
| G5 – UI/updater | EXE version đúng; font/XAML và nút gỡ/cài; updater lấy đúng repo, không tự xóa mod ngoài, không giả installed | FAIL |
| G6 – Phát hành | Chỉ khi G0–G5 PASS, tạo **một** bản RC đã đóng băng, checksum + release notes + rollback, không sửa ZIP dưới tag cũ | HOLD |

**Kết quả kiểm tra ngày 19/11/2026 (chỉ áp dụng cho source local):**
- Unit hạ tầng **154/154 PASS** và Core **69/69 PASS** trước bổ sung regression.
- Regression `MountOption_Pack_*` + `MountLauncher.txt` đã **FAIL trước sửa / PASS sau sửa** khi thiếu manifest game.
- Smoke dùng PAK R29 thật trên filesystem mô phỏng: **PASS** dọn 9 file xung đột có sao lưu, stock EXE/MountLang/SIG không đổi, chặn cài/gỡ lúc game chạy.
- **Chưa có runtime trên máy người báo lỗi**; resource game 3.8.12 với PAK 3.8.11 còn **HOLD**. Mã nguồn vá mới **chưa được phát hành thành bản ZIP PC mới**.
- **Chưa có rollback tự động toàn bộ quarantine khi R29/font cài thất bại sau bước dọn**; phải giữ làm blocker trước khi tuyên bố an toàn hoàn toàn.

## 5. Nguyên tắc cho các lần làm tiếp

- Đọc tài liệu này **trước mỗi lần sửa installer hoặc updater**. Không đụng project game, mod người khác, APK Android, tài khoản thật khi chạy test PC.
- Thêm regression cho **mỗi lỗi thực tế**, chứng minh **FAIL trước / PASS sau**, không chỉ sửa theo cảm giác.
- Tách **những gì test máy giả** với **game chạy thật**, chỉ ghi PASS theo đúng phạm vi.
- Khi sửa cơ chế cài/gỡ: ưu tiên fail-closed, ghi báo cáo file:line, diff, exit code, SHA và đường dẫn backup; không tự xóa lạ để ép thành công.
- Không tự tạo nhiều bản public nhỏ lẻ. Gom sửa, QA nguồn; chỉ release khi thật sự cần và người phụ trách chốt.
- Không dùng hardlink/copy bản ZIP cũ dưới tên version mới rồi khẳng định đã có code fix. Mỗi gói release phải được build lại và kiểm thử với chính source commit đó.

**Tài liệu kèm:** [Khắc phục game không mở](kiem-tra-xung-dot-viet-hoa-pc.md), [quy tắc build/release](../../BUILD_AND_RELEASE_RULES.md), [release v3.0.24](../releases/v3.0.24/RELEASE_NOTES.md).
