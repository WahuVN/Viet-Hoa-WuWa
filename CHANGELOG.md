# Nhật Ký Thay Đổi (Changelog)

Tất cả các thay đổi đáng chú ý của dự án VHWuWa được ghi lại tại đây theo chuẩn [SemVer](https://semver.org/lang/vi/).

## [3.0.9] - 2026-09-18

### Ứng dụng
- Làm mới logo trong cửa sổ và icon ứng dụng, dùng nền trong suốt để hiển thị gọn trên Desktop.
- Shortcut Desktop tự tạo có tên **WuWa**; lần mở đầu của bản mới tự thay shortcut `VHWuWa` cũ.
- Tinh gọn trang chủ/cài Việt hóa, giữ các thao tác chính và bỏ thông tin lặp.
- Củng cố quy trình tự cập nhật: kiểm tra đúng asset + SHA-256, dùng updater self-contained, cài theo transaction, health-check và rollback nguyên bản cũ nếu bản mới không khởi động đúng.
- Khi cài/cập nhật Việt hóa, tự phát hiện và xóa các mod cũ/mod ngoài thuộc tập xung đột rồi để PAK/loader của bản hiện tại thắng; không đụng các `pakchunk*` gốc của game.
- Cập nhật nhanh PAK có cơ chế **self-heal**: tự dựng lại PAK, `.sig`, font, `version.dll`, `verorg.dll`, `WuWaVH.dll`, đồng bộ marker SHA-256 và hậu kiểm sau khi ghi.
- PAK Hán Việt tải theo đúng release tag của phiên bản app, bắt buộc SHA-256 hợp lệ và PAK V12 hợp lệ.

### Dữ liệu Việt hóa
- So với PAK EN GitHub v3.0.8, dữ liệu hiện tại vẫn giữ 110 DB nhưng tăng từ **351.866** lên **351.939** ô nội dung; có **112 text mới**, **39 text bỏ theo nguồn** và hàng nghìn text/thuật ngữ được cập nhật.
- Bổ sung nội dung mới cho nhiệm vụ/nhạc nền như chuỗi `ItemInfo_80110539..80110546` liên quan **Yên Vân U Viễn Tâm Kiếm Minh**, Ngự Kiếm Phi Hành, Vân Bạch Cơ Kỵ và Thiên Khôi Kiếp Sát.
- Làm sạch hàng trăm fallback/annotation lộ ra giao diện, gồm raw English, `(Tý)`, `(Nguyện)`, `(Academy)`, `(Long)`, `(Tình yêu)` và các annotation nội bộ sai ngữ cảnh.
- Sửa regression QA trước phát hành: phục hồi **270** text `RoleResonanceGrouth` bị dev source `test/test` ghi đè, bỏ `test/` lộ trong hướng dẫn, khôi phục **35** text cốt truyện/hảo cảm bị rơi về English, và sửa lựa chọn Rogue giữ đủ placeholder `{0}` + `{1}`.
- Xác minh **43 câu P6 trước → sau thực sự nằm trong PAK R6**, tập trung vào câu dịch sượng/calque, xưng hô, register và ngữ cảnh; ví dụ “Quả thật rất mang phong cách của thi nhân.” → “Đúng là rất ra dáng một nhà thơ.”, “Đã có điều Bất thường...” → “Nếu đã thấy có gì bất thường...”, cùng nhiều câu Maqi/Jiyan/Encore/Carlotta/Phoebe. Danh sách đầy đủ: `TRANSLATION_CHANGES_v3.0.9.md`.
- MASTER nâng lên **V6.2.1 LOCKED**; đồng bộ `Shell Credit`, `Quyền Giáp`, `Du Long Tích` và current-source alias. R6 áp đúng **79** exact term corrections có source/topology guard.
- Thêm **Exit Watchdog** cho nút Mở game: sau khi `UnrealWindow` ổn định, nếu người chơi đóng cửa sổ nhưng `Client-Win64-Shipping` còn treo nền quá 5 giây thì watchdog dọn đúng process tree đã launch và wrapper liên quan; fail-open khi không đủ bằng chứng, không inject/patch game.

## [3.0.6] - 2026-08-25

### Dữ liệu Việt hóa
- Rà soát lại trọn bộ 12.935 record thuộc 10 part bằng đúng định danh `DB + bảng + key`; không nhập hàng loạt các câu chỉ vượt qua kiểm tra hình thức nhưng còn sai ngữ cảnh.
- Đồng bộ quy chuẩn tên nhân vật, địa danh, tổ chức, boss và thuật ngữ giữa bản Tên Anh và bản Hán Việt.
- Bổ sung phần phụ đề Vocal OST còn thiếu: hoàn thiện 186 key phụ đề ở cả hai biến thể, giữ nguyên 186 key lời gốc, token `{KeepOrigin}` và thẻ định dạng.
- Tăng kiểm tra placeholder, thứ tự token, nhánh giới tính, nội dung trong tag và các câu còn lẫn Trung/Anh trước khi đóng PAK.
- Hai PAK Tên Anh/Hán Việt đã được build V12 và hậu kiểm trên 97 cơ sở dữ liệu, 351.866 ô nội dung.

### WAHU Community
- Hoàn thiện bảng đặt tên nhân vật riêng cho Tên Tiếng Việt và Tên Hán Việt; ẩn các biến Rover nam/nữ nội bộ không dành cho chỉnh sửa.
- Thêm khu vực Thuật ngữ theo nhóm với các cột CN, EN, VI và HV; hỗ trợ sao chép tiếng Anh, sửa đúng key và tạo lại PAK sau khi lưu.
- Bổ sung kiểm tra đồng bộ thuật ngữ/boss theo MASTER, xem trước thay đổi và chỉ sửa các vị trí khớp đúng dữ liệu nguồn.
- Bảo đảm bốn trường UID riêng tư được ẩn ở bước build cuối cho cả hai biến thể, không bị hiện lại khi đổi font hoặc tạo lại PAK.
- Tinh gọn bố cục, thông báo và thao tác chính để phần đặt tên, thuật ngữ, font và hướng dẫn dễ dùng hơn.

## [3.0.5] - 2026-08-24
### Hot fix
- Hot fix.

## [3.0.4] - 2026-08-24
### Dữ liệu Việt hóa
- Rà soát 11.788 key từ bộ part 01–09 trên các mốc game 3.4, 3.5 và 3.6.
- Xác nhận 4.845 bản dịch trùng hoặc tương đương với dữ liệu hiện tại; bổ sung 5 mô tả thuộc tính đã đối chiếu chắc chắn.
- Loại khỏi lần nhập các câu còn trống, còn chữ Trung/Anh, sai placeholder, sai thẻ định dạng hoặc chưa đủ ngữ cảnh.
- Giảm số dòng thiếu chắc chắn trong PAK thực tế từ 12.935 xuống 8.203.
- Áp dụng MASTER V5.1 cho tên nhân vật, địa danh và tổ chức trên cả bản Tên Anh lẫn Hán Việt.
- Sửa 603 vị trí đã xác nhận trong 342 key: tên cũ, viết hoa, câu trống, nội dung fallback, thẻ định dạng, placeholder và số phần trăm.
- Chuẩn hóa `Sí Hà`, `Phục Linh`, `Black Shores`/`Hắc Hải Ngạn`, `Vùng Huyền Phương` và các địa danh liên quan.
- Hoàn thiện Part 10: kiểm tra 1.147 key, nhập 915 bản dịch mới/đã chỉnh và xác nhận lại 226 bản dịch hiện có.
- Sửa lỗi đóng file làm lặp trường VI/HV ở 35 block; chỉ giữ ngoài PAK 6 nhãn nội bộ `dnt/` không có nguồn EN.
- Chỉnh lại 8 câu thoại của Rebecca trong hồ sơ, vật phẩm và câu chuyện 3.4 để thống nhất giọng nói thẳng, mạnh và cách xưng hô `tao/mày`.

### Phát hành
- Đồng bộ phiên bản ứng dụng, tài liệu và liên kết tải gói Hán Việt cho v3.0.4.
- Thêm lớp hậu kiểm khóa tên sau khi gộp dữ liệu để nguồn lịch sử không ghi đè quy chuẩn mới.
- Gói Hán Việt được tải từ asset của release mới nhất, không còn khóa cứng vào một phiên bản cũ.

## [2.0.3] - 2026-08-23
### Cài đặt và quản lý xung đột
- Cài, gỡ và chuyển đổi Việt hóa/font an toàn hơn, theo dõi đúng các file do ứng dụng quản lý.
- Báo rõ mod có thể xung đột và cho phép người dùng chọn giữ bản sao hoặc xóa sau khi xác nhận.
- Cải thiện thao tác gỡ Việt hóa khi file đã bị thay đổi, tránh báo lỗi khó hiểu và không tự ý xóa file lạ.
- Cập nhật đúng liên kết tải gói Hán Việt của bản 2.0.3.
- Hiển thị đúng phiên bản Việt hóa đã cài thay cho giá trị `v2.0.0` ghi cứng.
- Sửa lỗi tiếng Việt bị mất dấu trong cửa sổ trình cập nhật.

### Khởi chạy game
- Thêm nút mở trực tiếp `Client-Win64-Shipping.exe` và nút tắt game đặt cạnh nhau, không bắt buộc đi qua launcher.
- Có công tắc tùy chọn `-ForceEnableCSharpEnvironment`, mặc định tắt và cảnh báo rõ đây là môi trường C# thử nghiệm.
- Thêm nút sao chép tham số cho Steam Launch Options; app không tự sửa cấu hình Steam.
- Hiển thị hướng dẫn kiểm tra dấu `*` ở cuối phiên bản và cách quay về chế độ thường.

### WAHU Community
- Làm mới màn hình khởi động, logo, bố cục và các liên kết Discord/GitHub.
- Tinh gọn mục nhân vật, giữ riêng phần đặt tên và mặc định tải đầy đủ dữ liệu khi chọn `Tất cả`.
- Tách ba bộ tên độc lập: Hán Việt, tên Anh và tên tự đặt; có thể tạo PAK riêng cho từng bộ.
- Bản tên tự đặt tự dùng tên Hán Việt cho những nhân vật chưa được nhập tên riêng.
- Giảm cảnh báo QA sai đối với placeholder, thẻ định dạng và câu dài; sửa cách nhận diện dữ liệu khi nhiều bảng dùng cùng mã key.
- Bổ sung dữ liệu giao diện hệ thống vào PAK và Việt hóa màn cảnh báo nhạy cảm ánh sáng khi mở game.

## [2.0.2] - 2026-08-23
### Cải thiện
- Tự tìm đúng thư mục `Wuthering Waves Game` có `Client\Binaries\Win64\Client-Win64-Shipping.exe`.
- Nhận diện được đường dẫn ở thư mục cha, thư mục `Client`, `Content\Paks` hoặc file EXE của game.
- Bổ sung vị trí cài dạng `D:\Game\...`, thư mục Kuro Games tùy chọn và game đang chạy.
- Không nhận nhầm thư mục chỉ có tên giống game nhưng thiếu Client thực tế.
- Sửa tạo font tùy chọn: bỏ tham số `repak -s` không tồn tại, đóng gói V11 rồi chuyển và xác minh đúng định dạng PAK V12 của game.
- Làm rõ DirectX 11 chỉ là phương án thay thế khi DirectX 12 gặp lỗi.

## [2.0.0] - 2026-08-21 (Cộng đồng 3.6)
### Thêm mới & Nâng cấp
- **Dữ liệu Việt hóa Wuthering Waves 3.6:** 100% Cốt truyện chính 3.6 (1.827 câu) + Kỹ năng các mốc 3.4, 3.5, 3.6.
- **Hệ thống Font chữ 2.0:** Tích hợp hơn 70+ bộ font tiếng Việt chuẩn Unicode, hỗ trợ tìm kiếm/lọc thời gian thực, xem trước chữ mẫu có dấu tiếng Việt trực quan, chuyển đổi và khôi phục font 1-click.
- **Hai biến thể Tên Nhân Vật độc lập:** Tùy chọn chuyển đổi linh hoạt giữa Tên Tiếng Anh canonical (*Jinhsi, Changli, Yangyang...*) và Tên Hán Việt (*Kim Tịch, Trường Ly, Ương Ương...*).
- **Bộ công cụ dịch thuật WAHU Community 3.6:** Hỗ trợ duyệt cốt truyện theo tuyến, dịch tay nhiều dòng, nhận diện ngữ cảnh và xưng hô nhân vật.
- **Bảo mật & Ẩn UID:** Tự động ẩn UID bằng ký tự trắng khi quay chụp màn hình, tích hợp kiểm tra mã băm SHA-256 hậu kiểm sau khi cài đặt.

## [1.0.0] - 2026-07-14
### Thêm mới
- Ứng dụng WPF (.NET 8, MVVM, DI, Wpf.Ui) với các trang chức năng: Trang chủ, Cài Việt hóa, Quản lý mod, Font chữ, Đồ họa, Hướng dẫn, Cài đặt.
- Hệ thống gói `.vhwpack` (manifest + SHA-256 + chữ ký RSA), bảo vệ chống path traversal / zip-slip.
- Tự động sao lưu và khôi phục khi cài/gỡ; quản lý mod (bật/tắt, phát hiện xung đột); chỉnh đồ họa theo cấu hình mẫu.
- Tự động kiểm tra bản cập nhật mới qua GitHub Releases kèm trình cập nhật độc lập `VHWuWa.Updater`.
- Công cụ CLI đóng gói và ký số `VHWuWa.PackageTool`.

