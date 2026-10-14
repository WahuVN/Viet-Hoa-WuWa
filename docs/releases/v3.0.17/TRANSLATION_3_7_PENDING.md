# Nội dung dịch 3.7 — khu vực chờ chốt cho v3.0.17

**HOLD.** Dữ liệu thoại/text phải được tách khỏi cập nhật giao diện/installer. Không đưa thành tích lịch sử vào thông báo v3.0.17 nếu chưa chứng minh PAK public v3.0.16 và PAK candidate v3.0.17 thực sự khác nhau.

## Nguồn và quy trình kiểm tra
- Lấy PAK đã public của v3.0.16 làm **baseline**, không dùng file ghi nhãn tùy ý.
- Lấy project authority 3.7 đúng nhánh, xuất candidate EN và Hán Việt riêng.
- Diff identity `dbfile/table/key` và văn bản CN/JP/EN/VI/HV, không so mỗi tên key.
- QA xưng hô và quan hệ theo diễn biến cốt truyện (ví dụ Suoming/Rover/Qingxiao, Mashan/Lingzhi, Rover nam/nữ), xét cốt truyện 3.6 đã kết thúc trước khi áp quy ước 3.7.
- Thẩm định các thay đổi đã tích lũy (lịch sử từng ghi 373 key/586 triple xưng hô, 25.278 identity, v.v.) bằng hash/diff của bản hiện tại. **Không mặc nhiên xem đây là thay đổi riêng 3.0.17.**
- Test số dòng thay đổi, source quote, token/placeholder, `{KeepOrigin}`, line breaks, font dấu, tự nhiên của Việt ngữ.
- Chỉ sau build PAK mới đưa thống kê chính xác vào `CHANGELOG_DRAFT.md`, `RELEASE_NOTES.md`, Discord.

## Mẫu bảng ghi sửa thoại (chưa điền khi không có bằng chứng)
| dbfile/table/key | Text trước (3.0.16 public) | Text sau (3.0.17 candidate) | CN/JP căn cứ | Lý do & QA |
| --- | --- | --- | --- | --- |
| [PENDING] | [PENDING] | [PENDING] | [PENDING] | [PENDING] |

## Điều kiện công bố
- PAK EN và Hán Việt chính xác, V12/fresh-unpack/QA/pass; DB nội dung tốt; runtime scene xác minh thực.
- Nếu không có thay đổi PAK, mô tả **“Bản này chỉ nâng cấp ứng dụng/installer/font; dữ liệu bản dịch giữ nguyên.”**
- Không nêu “xong 100% 3.7” hoặc “không còn lỗi xưng hô” nếu không có bằng chứng.
