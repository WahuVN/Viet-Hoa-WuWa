# Kiểm tra trước khi công bố v3.0.19

**Trạng thái: CHƯA ĐỦ ĐIỀU KIỆN PHÁT HÀNH.** Chỉ dùng bản QA nội bộ; không đổi trạng thái `latest` hoặc đăng thông báo Discord.

- [x] Mã nguồn PC khai báo 3.0.19, app/updater QA có FileVersion 3.0.19.0.
- [x] Test Release chạy lại ngày 15/11/2026: **172/172 PASS** (Core 56, Infrastructure 116).
- [x] PAK PC R29 đã được test nạp tiếng Việt trên PC thật, 62/62 DB khớp nguồn. Đã kiểm thử gỡ/cài lại bằng app; không dùng DLL loader cũ.
- [x] Đã đóng gói và kiểm tra cấu trúc ZIP QA **v3.0.19 + PAK R29** (82 tệp, EXE `3.0.19.0`, đúng `VHWuWa.Updater.exe`, đủ PAK R29 và manifest, không trùng/không có đường dẫn nguy hiểm). ZIP SHA-256 `F0F137D354A47F20A06EB260C8435743DE85D276358E647380076DCA1FF4AA03`.
- [ ] Chưa thử end-to-end bản ZIP mới trên máy game; đây là bản QA, không phải ZIP production.
- [x] Đã kiểm thử updater 3.0.18 → 3.0.19 bằng fixture có health/rollback.
- [x] Đã kiểm thử đúng updater **3.0.18 công khai** (SHA khớp GitHub) nâng ZIP ứng viên 3.0.19 R29 trên bản sao nguyên bộ ứng dụng; hai EXE mới đúng phiên bản, user DB/file riêng giữ nguyên, 6 PAK/loader cũ backup có SHA. Chế độ QA `--no-relaunch` không can thiệp Desktop/game.
- [x] Đã chuẩn bị nhật ký và nội dung thông báo bản nháp.
- [x] Đã xác minh client PC thật `Client/Binaries/Win64/Client-Win64-Shipping.exe`, dữ liệu 3.8.11.
- [x] Đã thay luồng loader DLL bằng Mount phụ và thử gỡ/cài lại R29 trên game thật.
- [ ] Kiểm tra đầy đủ PAK R29 trong gameplay: hội thoại, nhiệm vụ, font, dấu tiếng Việt và các lựa chọn tên.
- [ ] Chuẩn bị bản Hán Việt có nghiệm thu và manifest phù hợp (không tái dùng PAK 3.7).
- [x] Tạo ZIP **ứng viên portable** v3.0.19 + R29 từ hai EXE self-contained, nội dung R29/ảnh/font/tools/guide; cấu trúc ZIP round-trip và updater v3.0.18 thật đạt sandbox QA. SHA ZIP `e4023c657bc60894c1965846dbb53394d0f3477062d6b38c3316ddc8060c94bc`. 83 tệp.
- [x] Bản cài mới bảo đảm không ghi đè `editor/project.db`: seed riêng từ `project.default.db`, bộ legacy content 3.7 không được đóng kèm.
- [ ] Kiểm tra đầy đủ vòng cài mới trên Windows sạch, vòng chấp thuận cập nhật GUI từ GitHub khi release đã được tạo, và QA ứng viên trên máy game thật trước khi công bố.
- [ ] Xác minh SHA-256 của các asset trên GitHub và cập nhật README/changelog theo release đã công bố.
- [ ] Sau khi hoàn tất: tạo/cập nhật draft GitHub, duyệt lại notes, mới publish và đăng thông báo.

Gói `VietHoa-WuWa-v3.0.19-LOCAL-QA.zip` không phải asset chính thức và không được đổi tên để phát hành.
