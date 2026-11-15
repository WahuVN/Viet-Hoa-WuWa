# Kiểm tra thiết kế sidebar PC — 15/11/2026

## Vấn đề đã xác minh
- Ở phiên bản `735ea23`, `MainWindow.xaml` dùng 26 màu hex riêng lẻ; màu chọn menu, thẻ phiên bản và khung logo nghiêng xanh xám, khác với khu vực nội dung trung tính.
- Ảnh `app-logo-transparent.ico` được vẽ ở 28x28 trong khung 38x38 nên nét chi tiết nhân vật rất nhỏ, tạo cảm giác mờ.
- Nhãn cập nhật có emoji và các khối footer sử dụng kiểu hiển thị không nhất quán.

## Nguyên tắc cải tiến
- Fluent 2: sử dụng bảng màu trung tính cho mặt nền và chữ; dành điểm nhấn cho trạng thái tương tác, tránh nhiều loại khung màu.
- Dùng nhãn + icon cùng kích thước, một thanh chỉ báo trang đang chọn; giữ phản hồi hover và focus cho bàn phím.
- Giữ nhận diện WAHU bằng **ảnh gốc** chứ không vẽ logo mới. Dùng PNG 1254x1254 có alpha thay vì thu nhỏ ICO 28px.
- Chỉ sửa trang sidebar trong `src/VHWuWa.App/MainWindow.xaml`; **không thay đổi** PAK, game detection, installer, updater hay mã chức năng các trang.

## Thực hiện
- Thêm 9 brush `WahuSide*` theo vai trò, giảm số mã màu hex trong MainWindow từ 26 xuống 10 (gồm màu nền cửa sổ).
- Mặt nền sidebar `#171A1F`, hover `#24272C`, chọn `#2D3036`; màu chữ và viền tách theo vai trò.
- Đổi ảnh sidebar sang `/Assets/logo-app-transparent.png` ở 48px, HighQuality; bỏ hộp nền/viền xanh.
- Bỏ card xanh ở footer, thống nhất nút Discord/GitHub theo kiểu phẳng; giữ nhãn phiên bản, tình trạng game và thông báo nâng cấp.
- Không xóa các mục điều hướng. Giữ nguyên 8 route và ba nhóm chức năng.

## Nghiệm thu
- XAML parse: PASS; `git diff --check`: PASS.
- Build Release: PASS, 0 warnings, 0 errors.
- Unit tests: **162/162 PASS** (Core 50; Infrastructure 112).
- App thật trên WIN3: PASS ở 1180x740 và kiểm tra co cửa sổ xuống 980x620. Khi thiếu chiều cao, menu có thanh cuộn; không che chân sidebar.
- Navigation thực tế: Trang chủ, UID, Font game, Cài đặt đều PASS.
- Tỷ lệ tương phản màu đã tính: chữ thường/sidebar **10.28:1**; chữ phụ/sidebar **6.37:1**; chữ chọn/nền chọn **11.89:1**.
- Mã nguồn nằm ở nhánh chuẩn bị 3.0.19. Chưa phát hành Release công khai.

## Nguồn đối chiếu
- Microsoft Fluent 2 Color: https://fluent2.microsoft.design/color
- Microsoft Fluent 2 Iconography: https://fluent2.microsoft.design/iconography
- Microsoft Fluent 2 Accessibility: https://fluent2.microsoft.design/accessibility
- Microsoft Fluent 2 Navigation: https://fluent2.microsoft.design/components/web/react/core/nav/usage
- Microsoft Learn — WPF bitmap/DPI: https://learn.microsoft.com/en-us/visualstudio/extensibility/addressing-dpi-issues2
