# QA bản VHWuWa 3.0.17 RC1

Trạng thái: **Đạt kiểm tra code, bộ cài và giao diện Windows; chưa nghiệm thu game thật.**

| Kiểm tra | Kết quả |
| --- | --- |
| App Windows version 3.0.17.0 | PASS |
| Updater version 3.0.17.0 | PASS |
| Dotnet build Release | PASS, 0 warning |
| Unit test Core/Infrastructure | PASS, 130/130 |
| Font Lexend 650 tạo từ TTF, PAK V12 | PASS |
| Font mặc định trong ZIP là WahuFont_100_P.pak Lexend650 | PASS |
| Thư viện 73 font, Lexend Variable weight 650 | PASS |
| Mở app Windows thật, thấy version 3.0.17 | PASS |
| Mở trang Font, weight 650, nút Tạo & áp dụng | PASS |
| Trang chủ mặc định thu gọn Gỡ/Quét | PASS |
| Tùy chọn khác mở/đóng trên app thật | PASS |
| ZIP đủ 569 entry + CRC | PASS |
| SHA ZIP, SHA font | PASS |
| Updater fixture 3.0.16→3.0.17, rollback, bad SHA | PASS |
| Client Wuthering Waves thật, DX11/DX12, font/dialog | CHƯA TEST |
| Updater dùng ZIP RC1 trên máy chơi thật | CHƯA TEST |
| PAK thoại mới 3.0.17 so với bản 3.0.16 | KHÔNG CÓ, GIỮ NỘI DUNG CŨ |
| GitHub release / Discord | CHƯA ĐĂNG |

ZIP QA: `dist/VietHoa-WuWa-v3.0.17-RC1-QA.zip`

ZIP SHA-256: `75364afbeb21981914b50026fc3050c8122faa323d741917de16036a5a8bb241`

Font SHA-256: `ffc75bd057e15e47f288f2c1861b1f63b6443916e0cf255a987a055bd7da3632`

**Lưu ý:** Chưa được gọi là bản phát hành hoàn chỉnh cho người chơi cho tới khi chạy nghiệm thu game thật và chốt nội dung dịch.
