# VHWuWa v3.0.9

3.0.9 mình giữ lại lâu hơn dự tính vì cứ tới lúc đóng bản lại lòi thêm câu dịch hoặc case cài/update cần sửa. Lần này mình làm cho sạch rồi mới đưa lên.

**Nếu đang dùng 3.0.8:** bản này sửa cả PAK dịch chứ không chỉ đổi app.

So với PAK 3.0.8:
- **17.445 text đang có thay đổi**, thêm **112 text mới**, bỏ **39 text** theo source;
- **270 text kỹ năng** từng bị `test/test` ghi đè đã được phục hồi;
- **35 text cốt truyện/hảo cảm** bị rơi về English đã được sửa;
- **7 text Hổ Khẩu** bị exact override cũ co sai nội dung đã được khôi phục; trong đó có 4 text nhiệm vụ từng chỉ còn mỗi tên địa danh;
- **43 câu mình rà tay theo ngữ cảnh** đã kiểm lại ngay trong PAK cuối;
- thuật ngữ được đồng bộ theo MASTER **V6.2.1** và 79 correction đã xác minh.

Con số 17.445 là diff dữ liệu, **không phải 17 nghìn câu sửa tay**.

Một vài câu trước → sau:

> “Quả thật rất mang phong cách của thi nhân.”
> → **“Đúng là rất ra dáng một nhà thơ.”**

> “Đã có điều Bất thường, vậy thì đi xem thử đi.”
> → **“Nếu đã thấy có gì bất thường thì cứ đi xem thử.”**

> “Đều không phải.”
> → **“Không. Hôm nay tôi chẳng hẹn gặp ai, cũng không chờ ai đến cả.”**

> “Tại sao nó lại khao khát trở thành Loài người đến vậy?”
> → **“Tại sao nó lại muốn trở thành con người đến vậy?”**

> “Chú Lôi lại tự nói gì thế...”
> → **“Chú Lôi đang nói gì vậy?”**

> “Ấy, cậu không phải là {PlayerName} đó chứ!”
> → **“Ồ, cậu chẳng phải là {PlayerName} sao!”**

> “Ôm sao?”
> → **“Ôm á?”**

Ngoài ra còn các chỗ **ngươi → cậu**, `tôi/cậu`, `ta/cháu`, `bọn tôi` được sửa theo đúng người nói và cảnh.

**Phần app mình sửa:**
- updater kiểm SHA-256, có health-check và rollback;
- xử lý mod xung đột khi cài/update nhưng không đụng file gốc `pakchunk*`;
- PAK/.sig/font/loader do app quản lý bị thiếu hoặc ghi đè thì có thể tự phục hồi;
- PAK Hán Việt lấy đúng tag phiên bản;
- Exit Watchdog dọn process game do app mở còn treo;
- sửa nguồn font và link Discord cũ.

Cài mới: `VietHoa-WuWa-v3.0.9.zip`.

Nếu gặp câu nghe kỳ, sai xưng hô, còn English hoặc app lỗi thì gửi **ảnh + tên nhiệm vụ/cảnh** là được. Có thể dùng `/bao-loi` hoặc `/gop-y` để bot lưu report lại, đỡ bị trôi.

Discord: https://discord.gg/Gy5YQ84Yc2
