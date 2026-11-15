# Kiểm tra trước khi công bố v3.0.19

**Trạng thái: CHƯA ĐỦ ĐIỀU KIỆN PHÁT HÀNH.** Chỉ dùng bản QA nội bộ; không đổi trạng thái `latest` hoặc đăng thông báo Discord.

- [x] Mã nguồn PC khai báo 3.0.19, app/updater QA có FileVersion 3.0.19.0.
- [x] Test Release chạy lại ngày 15/11/2026: **174/174 PASS** (Core 56, Infrastructure 118).
- [x] PAK PC R29 đã được test nạp tiếng Việt trên PC thật, 62/62 DB khớp nguồn. Đã kiểm thử gỡ/cài lại bằng app; không dùng DLL loader cũ.
- [x] Lỗi khóa theo SHA game EXE/manifest 3.8.11 đã được bỏ; thử cài/gỡ PAK thật trên game giả lập có EXE/resource khác **PASS**, không thay tệp gốc. Xem `R29_NO_EXE_VERSION_PIN_QA_20261115.md`.
- [x] Đã đóng gói và kiểm tra cấu trúc ZIP QA **v3.0.19 + PAK R29** (82 tệp, EXE `3.0.19.0`, đúng `VHWuWa.Updater.exe`, đủ PAK R29 và manifest, không trùng/không có đường dẫn nguy hiểm). ZIP SHA-256 `F0F137D354A47F20A06EB260C8435743DE85D276358E647380076DCA1FF4AA03`.
- [ ] Chưa thử end-to-end bản ZIP mới trên máy game; đây là bản QA, không phải ZIP production.
- [x] Đã kiểm thử updater 3.0.18 → 3.0.19 bằng fixture có health/rollback.
- [x] Đã kiểm thử đúng updater **3.0.18 công khai** (SHA khớp GitHub) nâng ZIP ứng viên 3.0.19 R29 trên bản sao nguyên bộ ứng dụng; hai EXE mới đúng phiên bản, user DB/file riêng giữ nguyên, 6 PAK/loader cũ backup có SHA. Chế độ QA `--no-relaunch` không can thiệp Desktop/game.
- [x] Đã chuẩn bị nhật ký và nội dung thông báo bản nháp.
- [x] Đã xác minh client PC thật `Client/Binaries/Win64/Client-Win64-Shipping.exe`, dữ liệu 3.8.11.
- [x] Đã thay luồng loader DLL bằng Mount phụ và thử gỡ/cài lại R29 trên game thật.
- [ ] Kiểm tra đầy đủ PAK R29 trong gameplay: hội thoại, nhiệm vụ, font, dấu tiếng Việt và các lựa chọn tên.
- [ ] Chuẩn bị bản Hán Việt có nghiệm thu và manifest phù hợp (không tái dùng PAK 3.7).
- [x] Tạo ZIP **ứng viên portable** v3.0.19 + R29 từ hai EXE self-contained, nội dung R29/ảnh/font/tools/guide; cấu trúc ZIP round-trip và updater v3.0.18 thật đạt sandbox QA. SHA ZIP `7e1b4a07399d548b3aea1e881442b1909e69d5872d637e873d17c21dfa27cabb`. 84 tệp, gồm 10 mục Trợ giúp mới và Lexend Variable mặc định 650.
- [x] Bản cài mới bảo đảm không ghi đè `editor/project.db`: seed riêng từ `project.default.db`, bộ legacy content 3.7 không được đóng kèm.
- [ ] Kiểm tra đầy đủ vòng cài mới trên Windows sạch, vòng chấp thuận cập nhật GUI từ GitHub khi release đã được tạo, và QA ứng viên trên máy game thật trước khi công bố.
- [x] ZIP Draft GitHub có SHA-256 khớp ứng viên v3.0.19 mới (**e97b213c…b98b2d**); file SHA256SUMS cũng được kiểm tra. BotDis staging đã đồng bộ SHA mới và 7/7 thẻ PASS.
- [ ] Sau khi công bố Release: xác minh lại `releases/latest`, link tải và cập nhật README/changelog chính thức.
- [ ] Sau khi hoàn tất: tạo/cập nhật draft GitHub, duyệt lại notes, mới publish và đăng thông báo.

Gói `VietHoa-WuWa-v3.0.19-LOCAL-QA.zip` không phải asset chính thức và không được đổi tên để phát hành.
