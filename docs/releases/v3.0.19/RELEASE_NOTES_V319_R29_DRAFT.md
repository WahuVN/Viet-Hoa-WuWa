# WuWa Việt Hóa PC 3.0.19 — Nội dung thông báo phát hành (BẢN NHÁP)

**Trạng thái:** Chưa công bố. Chỉ đăng sau khi ZIP hoàn chỉnh và nâng cấp trực tiếp từ 3.0.18 đạt QA.

## Có gì mới?
- Bổ sung Việt hóa Wuthering Waves PC 3.8.0 (dữ liệu 3.8.11), bản dịch **R29**.
- Gói R29 được kiểm chứng ở màn hình đăng nhập/cảnh báo trên máy thử nghiệm MAY-007; lắp bằng PAK + SIG + Mount phụ, không cần DLL loader.
- Giao diện đồng bộ màu, icon, font và nút bấm, hướng dẫn cài đặt gọn và dễ hiểu hơn.
- Giữ **ZIP portable**: giải nén vào thư mục cố định, mở `VHWuWa.exe`, ứng dụng hỗ trợ tự kiểm tra và hỏi trước khi cập nhật.

## Cài đặt hoặc nâng cấp
1. Người dùng mới: tải `VietHoa-WuWa-v3.0.19.zip` chính thức khi được đăng; giải nén toàn bộ ra thư mục riêng, mở `VHWuWa.exe`.
2. Người đang dùng 3.0.18: chấp thuận thông báo cập nhật trong ứng dụng. Updater đối chiếu SHA-256, cập nhật ngay tại chỗ, kiểm tra khởi động và có cơ chế khôi phục khi lỗi.
3. Trong app: chọn đúng thư mục game, kiểm tra phiên bản được hỗ trợ, đóng game rồi cài R29. **Không tự ghi PAK vào game chỉ vì ứng dụng đã cập nhật.**
4. Nếu game mới cập nhật phiên bản chưa được xác minh, nút Cài sẽ bị khóa; chờ gói Việt hóa tương thích.

## Tương thích/giới hạn
- R29 hiện hỗ trợ **bản giữ tên quốc tế**; chưa có biến thể Hán Việt cho game PC 3.8.11.
- Không công bố hỗ trợ PAK font rời ở cơ chế Mount R29; chức năng này chưa được nghiệm thu trong game 3.8.11.
- Chỉ xác minh game EXE/resource có hash/manifest trùng môi trường thử nghiệm. Không bảo đảm toàn bộ lời thoại, nhiệm vụ hay mọi bản cập nhật sau của game đã được dịch hoàn chỉnh.
- Máy Windows có thể hiển thị cảnh báo SmartScreen vì EXE chưa ký số. Chỉ dùng nguồn phát hành chính thức và không tắt công cụ bảo vệ hệ thống.

**Lưu ý:** R29 là phiên bản dữ liệu; phiên bản ứng dụng của đợt phát hành này vẫn là **3.0.19**, không phải 3.0.20.

Tải về: https://github.com/WahuVN/Viet-Hoa-WuWa/releases (link cụ thể sẽ có sau khi xuất bản).
