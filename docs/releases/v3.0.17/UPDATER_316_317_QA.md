# Kiểm tra tự cập nhật 3.0.16 → 3.0.17

**Kết quả: PASS trong môi trường thử nghiệm riêng trên WIN3, chưa phát hành 3.0.17.**

Bản 3.0.16 có tính năng tự kiểm tra phiên bản qua GitHub. Mặc định bật khi mở app; nếu có phiên bản mới thì **hỏi trước**, người dùng chọn Đồng ý để ứng dụng tự tải ZIP, kiểm tra SHA và cập nhật.

Mình đã chạy thử bằng **updater phiên bản 3.0.16.0** của bộ QA và nguyên ZIP **3.0.17 RC1**:

- Cập nhật thật từ EXE/updater 3.0.16 lên EXE/updater 3.0.17.0: **PASS**.
- Mở app 3.0.17 sau cập nhật và gửi tín hiệu xác nhận khởi động: **PASS** (log updater ghi thành công).
- Cấu hình người dùng không mất: **PASS**.
- File có SHA sai bị từ chối trước khi ghi đè: **PASS**.
- Trình cập nhật tự rollback khi ứng dụng mới không khởi động được: **PASS** ở bài E2E giả lập lỗi.
- ZIP 3.0.17 đầy đủ và font Lexend 650 sau cập nhật đúng checksum: **PASS**.

**Chưa thể thử quy trình tải 3.0.17 trực tiếp từ GitHub:** phiên bản mới nhất đang công khai là 3.0.16. Khi phát hành 3.0.17 phải có **đúng tên** `VietHoa-WuWa-v3.0.17.zip` và **SHA-256 thật** trong GitHub Release; nếu sai tên hoặc thiếu SHA, bản 16 sẽ không chấp nhận.

**Phân biệt:** Đây là thử nghiệm cập nhật app trên WIN3 bằng gói ZIP thật, chưa phải nghiệm thu font, loader và bản dịch chạy bên trong game Wuthering Waves.

Bản ZIP QA đã kiểm tra: `dist/VietHoa-WuWa-v3.0.17-RC1-QA.zip` (SHA-256 `75364afbeb21981914b50026fc3050c8122faa323d741917de16036a5a8bb241`).

Không sửa GitHub release, chưa gửi thông báo Discord.
