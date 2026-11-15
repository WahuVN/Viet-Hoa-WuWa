# QA giao diện chọn tên và font — PC v3.0.19 (15/11/2026)

**Trạng thái: QA nội bộ, chưa phát hành.** Worktree: `fix/v319-font-choices-ui-20261115` (tách biệt nhánh `prep/v3.0.19-20261113`).

## Nguyên nhân
- Khi nhận PAK R29, `InstallViewModel.Refresh()` chủ động đặt `IsHanVietAvailable=false`, `FontAvailable=false`, ép `VariantEnglish=true` và `ApplyFont=false`. Radio và checkbox không thể nhấn hoặc bị tự xóa lựa chọn.
- Tab Font kiểm tra trạng thái font theo marker của loader cũ trong `~WuWaMods`, không nhận diện R29 đang cài bằng Mount phụ. Font PAK cũ chưa được chứng nhận cho R29.
- Ảnh xem trước font có bitmap cố định tối thiểu 1400 px nhưng WPF để `Stretch=None` nên chỉ thấy một phần bên trái; nhãn độ đậm có màu chữ sáng cố định trên nền sáng, hai nút dùng accent xanh của hệ thống.

## Đã sửa
- Tùy chọn **Tên Hán Việt** và **Cài font tiếng Việt** đều thao tác được trên Trang chủ và màn hình Cài đặt. Không xóa lựa chọn sau Refresh hoặc chuyển trang.
- Khi thiếu PAK R29 Hán Việt/font riêng, ứng dụng hiện lý do ngay bên dưới, không cài lén bản EN hoặc âm thầm bỏ tick. Nút Cài không bật cho phương án chưa có dữ liệu tương thích.
- Tách `GroupName` cho hai trang, tránh hai bộ radio ảnh hưởng nhau.
- Tab Font nhận diện R29 và giải thích chưa hỗ trợ thay font bằng PAK của loader cũ, không báo nhầm là chưa cài Việt hóa. Vẫn xem trước và chọn font được.
- Bitmap preview được co đúng tỷ lệ vào khung, còn nút **Xem lớn** giữ tùy chọn xem kích thước lớn. Màu chữ nhãn độ đậm theo light/dark theme; nút xanh đổi sang style xám có tương phản.
- Giữ nguyên số phiên bản app `3.0.19` (FileVersion `3.0.19.0`), không đụng loader, PAK gốc hay updater.

## Kiểm thử
- WIN3: build Release và `dotnet test` **174/174 PASS** (56 Core + 118 Infrastructure).
- Khởi chạy ứng dụng QA thực tế từ bản ZIP portable đầy đủ kèm R29.
- UI Automation xác nhận radio Hán Việt và checkbox Font `enabled=true`; click chọn Hán Việt, tick Font, thông báo đổi thành cảnh báo cả hai lựa chọn; chuyển tab Font rồi về Trang chủ vẫn giữ trạng thái; chọn lại tên Anh và bỏ Font thì cảnh báo trở lại bình thường.
- Chụp và kiểm tra trực quan tab Font: câu xem trước không còn bị cắt bên phải, các nút không còn xanh hệ thống, nhãn độ đậm dễ đọc trên theme tối.
- `VHWuWa.Updater.exe` và PAK R29 giữ nguyên trong gói QA. Chưa nghiệm thu bản cài này trên MAY-007 (connector hiện không kết nối).

## Giới hạn chưa vượt qua
- Tùy chọn Hán Việt và PAK font riêng **đã chọn được**, nhưng không thể cài hai biến thể vì chưa có PAK R29 tương thích đã nghiệm thu. Cố tình chặn trước khi thay dữ liệu game.
- Chưa kiểm thử gameplay/font R29 đủ 100%; chưa công bố ZIP này thành GitHub Release, không nâng app lên 3.0.20.
