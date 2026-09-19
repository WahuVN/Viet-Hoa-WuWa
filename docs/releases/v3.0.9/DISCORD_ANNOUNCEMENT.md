# VHWuWa v3.0.9

@everyone 📢 **VHWuWa v3.0.9 đã có rồi!**

Xin lỗi mọi người vì **3.0.9 ra chậm hơn dự kiến**. Mình giữ bản này lại thêm một thời gian để sửa cho kỹ hơn, nên so với **3.0.8** thì cả phần Việt Hóa lẫn app đều thay đổi khá nhiều.

## 📝 Việt Hóa

- Có hơn **17.000 text thay đổi so với 3.0.8**, chủ yếu là chỉnh câu dịch sượng, câu máy móc, sai xưng hô và các chỗ dùng thuật ngữ chưa thống nhất.
- Sửa nhiều đoạn xưng hô chưa đúng nhân vật/ngữ cảnh như `ngươi → cậu`, `tôi/cậu`, `ta/cháu`, `bọn tôi`...
- Fix **35 text** cốt truyện/hảo cảm bị rơi ngược về tiếng Anh.
- Phục hồi **270 text kỹ năng** từng bị lỗi thành `test/test`.
- Sửa một số đoạn bị mất nội dung, thậm chí có câu nhiệm vụ trước đó chỉ còn mỗi tên địa danh.
- Đồng bộ lại nhiều thuật ngữ để tránh tình trạng cùng một thứ nhưng mỗi chỗ gọi một kiểu.
- Thêm **112 text mới** theo dữ liệu game mới.

**Một vài câu sượng đã được sửa lại:**

> “Đi theo góc nhìn của nhân vật chính, mình như thể chính mình cũng đã trải qua hết cuộc phiêu lưu sử thi này đến cuộc phiêu lưu sử thi khác...”
> → **“Càng đọc theo góc nhìn của nhân vật chính, tôi càng có cảm giác như chính mình cũng đang phiêu lưu...”**

> “Mình nhìn chằm chằm anh ta... Anh ta khựng lại, ánh mắt né tránh đầy vi diệu.”
> → **“Bạn nhìn chằm chằm vào anh ta... anh ta khựng lại một nhịp rồi khẽ lảng ánh mắt đi.”**

> “Chú Lôi lại tự nói gì thế, em được anh trai nhờ giúp {PlayerName} cùng giải quyết vấn đề...”
> → **“Chú Lôi đang nói gì vậy? Anh trai nhờ tôi cùng {PlayerName} giải quyết chuyện...”**

> “Abby thuật lại lời lảm nhảm của vị Hiệp Sĩ Điên một cách có mẫu có dạng...”
> → **“Abby bắt chước lại lời lảm nhảm của Hiệp Sĩ Điên một cách ra trò...”**

> “{PlayerName}, đến xem vận may hôm nay đi, tôi rất tò mò hôm nay bạn sẽ kết duyên với vị Tuế Chủ nào đó?”
> → **“{PlayerName}, lại đây xem vận may hôm nay nào. Ta cũng tò mò không biết hôm nay cháu sẽ có duyên với vị Tuế Chủ nào.”**

Ngoài mấy ví dụ trên còn nhiều câu khác đã được rà lại theo ngữ cảnh, đặc biệt là các đoạn hội thoại có xưng hô thay đổi theo nhân vật và diễn biến cốt truyện.

## 🛠️ App / Updater

- Tự xử lý trường hợp **game đã tắt nhưng process vẫn còn treo nền**, đỡ phải mở Task Manager tắt tay.
- Updater mới kiểm tra **SHA-256** trước khi cài.
- Nếu update xong mà bản mới mở lỗi, app có thể **rollback về bản trước**.
- Sửa cách xử lý khi cài/update để hạn chế xung đột với mod đang có.
- Nếu PAK Việt Hóa, `.sig`, font hoặc loader bị thiếu/bị ghi đè thì app có thể **tự phục hồi lại**.
- Sửa lỗi chọn/lấy sai **PAK Hán Việt theo phiên bản**.
- Sửa lại một số link, font và các lỗi nhỏ còn sót trong app.

⚠️ Nếu gặp câu nào còn sượng, sai xưng hô, còn English hoặc app có lỗi thì dùng **/bao-loi**.
💡 Có cách dịch khác thấy hợp hơn thì dùng **/gop-y**.

📥 **Tải VHWuWa v3.0.9:** https://github.com/WahuVN/Viet-Hoa-WuWa/releases/tag/v3.0.9

Đã kiểm thử với **Wuthering Waves 3.6**.
