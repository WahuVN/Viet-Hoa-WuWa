# VHWuWa — Việt hóa Wuthering Waves

VHWuWa là bộ cài chơi và tool dịch dành cho cộng đồng Việt hóa Wuthering Waves trên Windows.

[Tải bản mới nhất](https://github.com/WahuVN/wuwa-vietnamese-launcher/releases/latest) · [Báo lỗi](https://github.com/WahuVN/wuwa-vietnamese-launcher/issues) · [Discord PC](https://discord.gg/c9ws4q9U7)

## Tải đúng gói

### `01_CAI_VIET_HOA_WUWA.zip` — dành cho người chơi

1. Tải và giải nén toàn bộ file.
2. Mở thư mục `01_CAI_VIET_HOA_WUWA`.
3. Chạy `00_BAT_DAU_CAI.bat`.
4. Bấm **Tự tìm game** hoặc chọn thư mục `Wuthering Waves Game` (thư mục có `Client`).
5. Chọn **Tên Anh** hoặc **Hán Việt**, rồi bấm **Cài Việt hóa**.

Trong game, chọn ngôn ngữ hiển thị **English** trước khi cài. Font tiếng Việt mặc định đã được kèm trong gói, không cần tải font riêng.

### `02_TOOL_DICH_WAHU_COMMUNITY.zip` — dành cho người tham gia dịch

1. Tải và giải nén toàn bộ file.
2. Mở thư mục `02_TOOL_DICH_WAHU_COMMUNITY`.
3. Chạy `00_MO_TOOL_DICH.bat`.
4. Giữ nguyên `project.db` và `build_support` khi làm việc.

Tool hỗ trợ xem, tìm và sửa CN / EN / VI / HV; đánh dấu đã dịch / đã duyệt; quản lý thoại, NPC, UI, kỹ năng và tạo PAK thử nghiệm.

Không cần tải mục `Source code` do GitHub tự tạo nếu chỉ muốn chơi hoặc dùng tool.

## Nội dung dữ liệu hiện có

- 3.6: **1.827 câu cốt truyện chính**.
- 3.6: **280 thoại NPC** đã được nhập và rà soát bổ sung.
- Hai biến thể tên nhân vật: **Tên Anh** và **Hán Việt**.
- UI, thuật ngữ và mô tả kỹ năng đã được bổ sung ở các phần dữ liệu hiện có.

Đây là dự án cộng đồng đang được cập nhật. Vẫn có thể còn text sót theo phiên bản game hoặc ngữ cảnh. Khi báo lỗi, hãy gửi ảnh chụp, đoạn text còn thiếu và phiên bản game để cộng đồng bổ sung đúng chỗ.

## Kiểm tra file tải về

File `03_KIEM_TRA_SHA256.txt` trong Release chứa mã kiểm tra cho hai gói tải xuống. Chỉ cần dùng khi bạn nghi file bị tải thiếu hoặc hỏng.

## Lưu ý

- VHWuWa là dự án do cộng đồng thực hiện, không phải sản phẩm chính thức của Kuro Games.
- Bản Việt hóa thay đổi tệp của game; người dùng tự cân nhắc trước khi cài.

## Build từ mã nguồn

Yêu cầu: Windows 10/11 x64 và .NET 8 SDK.

```bash
dotnet restore VHWuWa.sln
dotnet test VHWuWa.sln -c Release
dotnet build VHWuWa.sln -c Release
```

## Giấy phép

Mã nguồn được phân phối theo [MIT License](LICENSE).


