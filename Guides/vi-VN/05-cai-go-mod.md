# Xử lý xung đột mod

## Bản 3.0.16 xử lý tự động

- Khi bấm **Cài Việt hóa** hoặc khi chạy cập nhật nhanh, VHWuWa tự quét các file/mod ngoài có khả năng xung đột.
- Các file ngoài đã được detector xác định sẽ được dọn khỏi thư mục game trước khi chèn lại bộ VHWuWa chuẩn.
- Không cần bấm **Cách ly** hoặc **Xóa mod ngoài** thủ công trong luồng cài bình thường.
- File official của game được bảo vệ bằng `OriginResource.json`; `pakchunk*` và file official không nằm trong tập dọn.
- Bộ VHWuWa legacy của chính dự án được nhận diện riêng và nâng cấp/migrate, không bị coi là mod ngoài.

## Lineage VHWuWa được bảo vệ

Các bản VHWuWa cũ dùng họ file:

- `~WuWaMods\WuWaVH_99_P.pak`
- `WahuFont_100_P.pak` ở các bản cũ
- `Win64\version.dll`
- `Win64\verorg.dll`
- `Win64\WuWaVH.dll`
- `vhwuwa_install.json`
- `version_goc.dll` khi đó thực sự là backup do installer quản lý

Các tên `wuwaVietHoa*`, WWMI/3DMigoto, UE4SS, proxy DLL ngoài và các thư mục mod tương tự không phải lineage managed của VHWuWa và được xử lý như mod ngoài.

> Luôn tắt hoàn toàn game và launcher trước khi cài, cập nhật hoặc gỡ Việt hóa.
