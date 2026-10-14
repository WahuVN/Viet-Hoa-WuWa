# WuWa Việt Hoá 3.0.17 — RC2 text QA (14/10/2026)

**CHỈ DÀNH CHO QA. CHƯA PHÁT HÀNH GITHUB/DISCORD.**

## Đã làm thật
- RC1: app PC 3.0.17.0, updater và giao diện gọn, Lexend Variable mặc định 650, điều chỉnh 100–900, danh mục 73 font; 130/130 test PASS theo báo cáo RC1.
- Khôi phục cả nguồn Trung/Nhật của 134 bản ghi bị `****`: đủ 134/134 từ `lang_multi_text_1sthalf.db`. Tổng mẫu tuyến Qingxiao là 243 bản ghi; riêng 3 bản ghi tin nhắn không có chữ Nhật là dữ liệu không chứa văn bản.
- PC EN: dùng PAK QA ngày 13/10 đã vá 12 key Qingxiao theo đối chiếu CN/JP, đồng thời mang các điều chỉnh tích lũy từ QA 09/10. **Không gọi tất cả thay đổi tích lũy là 12 lỗi riêng của 3.0.17.**
- PC Hán Việt: vá đúng 12 key trong cả `lang_multi_text.db` và `lang_multi_text_1sthalf.db`, PAK V12 + giải nén lại PASS.
- Android EN/Hán Việt: đã tạo 2 PAK V12 riêng, 12 key / 24 row mỗi biến thể, giải nén lại PASS; đồng bộ vào source app kèm backup nguyên bản.
- Android source đã đặt label `WuWa Việt Hoá`, versionCode 15, versionName `0.2.0-alpha.15`; **chưa build được APK alpha.15**.
- PC RC2 có 569 file, kiểm tra ZIP hoàn chỉnh bằng 7-Zip PASS; băm SHA-256 và đúng 2 PAK EN/Hán Việt + Lexend 650 trong ZIP đã xác minh.
- **Updater thực từ PC 3.0.16 → ZIP RC2**: sai SHA bị từ chối, đúng SHA cài được, giữ cấu hình và font Lexend 650. Bằng chứng trong `dist/VietHoa-WuWa-v3.0.17-UPGRADE-AUDIT.json`; script báo tên RC1 cũ nhưng hash gói là hash RC2 dưới đây.

## Gói QA nội bộ PC
`D:\WAHU_WORK\WUWA_MOD_CLEAN_SAFE_20261013\dist\VietHoa-WuWa-v3.0.17-RC2-TextQA.zip`

SHA-256: `35c4c24dccd82f3923a74a16457691a178d4198bc364a4cc6d4e98d87f756a2e`

PC EN PAK SHA-256: `44350c79cbf9cb307752682f0e0f3a31af983cdc1fea4b24403b7395237df547`

PC Hán Việt PAK SHA-256: `e41177b5860465dfb40fbe2f3fdb4504be1a814a3bfd9bfa4a9da736d0a5e9e2`

Font Lexend 650 PAK SHA-256: `ffc75bd057e15e47f288f2c1861b1f63b6443916e0cf255a987a055bd7da3632`

## Android source QA
`D:\Tim hieu\WuwaVH\VHWuWa.Android\android-app\app\src\main\assets\payload\`

EN PAK SHA-256: `f2e47d503d2be2d55d51552481e270b03a143d86652f680535bb51475de53e42`

Hán Việt PAK SHA-256: `f31f40f7444c6e893338bbee0b2ab721e29b536cb8c07e3ba653e5a665206b98`

Backups tại `D:\WAHU_WORK\WUWA_COMPARE_V3016_20261013\WUWA_317_RC2_QA\BACKUP_BEFORE_RC2_*.pak`.

**Blocker Android:** Gradle AGP 9.4.0 không nằm trong cache dùng offline; JVM nhiều lần hết RAM/native memory khi thử build. Bộ APK alpha.14 cũ giữ nguyên; chưa có bản alpha.15 đã ký và cài được. Mọi APK unsigned thử nghiệm chỉ nằm trong khu vực QA, không cấp cho người dùng.

## Chưa có bằng chứng PASS
- Chưa cài/test Wuthering Waves thật trên PC DX11/DX12: không xác nhận gameplay, text render, font/placeholder sau khi game load.
- Chưa thử trên thiết bị Android/Shizuku thật với hai PAK mới.
- Chưa thử updater tự tìm/tải gói từ GitHub public.
- Chưa gắn tag/push GitHub, chưa sửa manifest BotDis production hay gửi Discord.
- Chưa có kết luận mọi thoại 3.7 đều chuẩn 100%; 12 key Qingxiao là phạm vi đã duyệt trong gói QA.

**Phát hành chính thức = HOLD** đến khi có kiểm thử trong game, APK đã build/ký đúng và đối chiếu manifest. Không ghi đã cập nhật cho người chơi.
