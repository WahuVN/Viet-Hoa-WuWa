# VHWuWa — cài Việt hóa Wuthering Waves

VHWuWa là ứng dụng Windows giúp cài/gỡ bản Việt hóa Wuthering Waves, chọn tên
nhân vật kiểu **Tên Anh** hoặc **Hán Việt**, cài font tiếng Việt và kiểm tra đủ
file sau khi cài.

## Tải bản mới nhất

Vào **[GitHub Releases](https://github.com/WahuVN/wuwa-vietnamese-launcher/releases/latest)**
và tải một trong hai file:

- `VHWuWa_BanCai.zip`: dành cho người chơi, có sẵn app và dữ liệu Việt hóa.
- `WAHU_Community_BanCai.zip`: tool đọc/sửa/dịch, duyệt cốt truyện và tạo PAK
  dành cho cộng tác viên.

Không tải mục **Source code** nếu chỉ muốn cài chơi.

## Cài nhanh cho người chơi

1. Tải `VHWuWa_BanCai.zip` và **giải nén toàn bộ** ra một thư mục riêng.
2. Chạy `Chay VHWuWa.bat` hoặc `app\VHWuWa.exe`.
3. Ở **Thư mục game**, bấm **Tự tìm game**. Nếu app không tìm thấy, bấm
   **Chọn thư mục** và chọn đúng thư mục có thư mục con `Client`.
4. Mở **Cài Việt hóa**, chọn **Tên Anh** hoặc **Hán Việt**.
5. Giữ tùy chọn cài font tiếng Việt, sau đó bấm **Cài Việt hóa**.
6. Chỉ mở game khi app báo cài thành công và kiểm tra đủ file.
7. Trong game đặt ngôn ngữ chữ là **English** và chạy bằng **DirectX 11**.

### Mẫu đường dẫn đúng

Bản Kuro/Launcher thường có dạng:

```text
C:\Wuthering Waves\Wuthering Waves Game
D:\Games\Wuthering Waves\Wuthering Waves Game
E:\Games\Wuthering Waves\Wuthering Waves Game
```

Bản Steam thường có dạng:

```text
D:\SteamLibrary\steamapps\common\Wuthering Waves
```

Thư mục bạn chọn phải mở ra và thấy ngay `Client`, ví dụ:

```text
E:\Games\Wuthering Waves\Wuthering Waves Game\Client
```

Hãy chọn `...\Wuthering Waves Game`, **không chọn** `Client`, `Saved`, `Paks`
hoặc thư mục chỉ chứa launcher.

## Đổi bản và gỡ

- Đổi Tên Anh ↔ Hán Việt: chọn kiểu tên khác và bấm **Cài Việt hóa** lại.
- Gỡ sạch: tắt game, mở **Cài Việt hóa** và bấm **Gỡ Việt hóa**.
- Nếu đang có mod khác, app sẽ cảnh báo để tránh xung đột trước khi cài.

## Nội dung bản 2.0.0

- Cốt truyện chính 3.6: 1.827/1.827 câu đã cập nhật VI/HV.
- Mô tả kỹ năng: 3.4 = 1.766/1.766, 3.5 = 93/93, 3.6 = 97/97.
- Hai PAK riêng cho Tên Anh và Hán Việt.
- Ẩn UID bằng ký tự trắng.
- Bộ 85 DB đã kiểm tra, có hậu kiểm PAK/SIG/font/loader sau khi cài.

## WAHU Community

Tool cộng đồng cho phép:

- Duyệt cốt truyện 3.4/3.5/3.6 theo khu vực, tuyến và Part.
- Xem Trung/Anh/Việt/Hán Việt, người nói, người nghe và ngữ cảnh.
- Dịch tay nhiều dòng, đánh dấu **Dịch tay** và **Đã duyệt**.
- Tìm và sửa giao diện, tên, HP/ATK/DEF, kỹ năng, vật phẩm và thuật ngữ.
- Chạy QA, tạo PAK thử nghiệm và xuất gói đóng góp.

Xem [hướng dẫn đóng góp](Guides/vi-VN/09-dong-gop-ban-dich.md).

## Build từ mã nguồn

```powershell
dotnet restore VHWuWa.sln
dotnet test VHWuWa.sln -c Release
powershell -ExecutionPolicy Bypass -File .\scripts\build-dist.ps1
```

Mã nguồn công khai không chứa PAK/game asset. Máy phát hành phải có dữ liệu được
phép phân phối tại `wuwavh_tool/Wahu/dist` trước khi chạy `build-dist.ps1`.

## Giấy phép và lưu ý

Mã nguồn dùng giấy phép MIT. Bản Việt hóa/mod là nội dung không chính thức; hãy
tắt game trước khi cài/gỡ và tự cân nhắc rủi ro anti-cheat.
