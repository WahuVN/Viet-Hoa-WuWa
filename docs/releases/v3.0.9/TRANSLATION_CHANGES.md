# Chi tiết thay đổi bản dịch v3.0.9

File này ghi lại các câu **đã kiểm tra và có mặt trong PAK cuối (R7)**. Những câu mới chỉ là đề xuất nhưng chưa được đưa vào PAK không nằm ở đây.

PAK EN R7 SHA256: B08B334B3CCAB2AD7005F4F465B645E3222975B11F6447F81A6C0892353695DF

## Tổng quan

- 43 câu được rà lại từng câu theo ngữ cảnh và đã xác minh có mặt đúng trong PAK cuối.
- Bản cuối cũng giữ các phần đã dọn trước đó: 270 dòng dev `test/test`, 1 `test/` bị lộ, 35 text rơi về English, 1 câu Rogue giữ đúng `{0}` + `{1}`; đồng thời áp 79 term correction theo MASTER V6.2.1: 3 `Du Long Tích` + 76 `Quyền Giáp`.
- So với GitHub v3.0.8: **17.445 text đổi, 112 text mới, 39 text bỏ theo nguồn**; tổng 17.596 triple khác.
- Sau vòng chốt cuối, phát hiện thêm **7 row Hổ Khẩu** bị exact override cũ ghi đè sai từ 3.0.8. Bản 3.0.9 khôi phục đủ nội dung theo EN/CN live source và sửa authority trong project để build sau không tái phát.

## 43 ví dụ câu cũ -> câu mới đã xác minh

### 1. Event_DRSL_1_14 — Maqi

**Trước:** Đi theo góc nhìn của nhân vật chính, mình như thể chính mình cũng đã trải qua hết cuộc phiêu lưu sử thi này đến cuộc phiêu lưu sử thi khác trong thế giới của <i>Đệ Nhị SOL-3</i>...

**Sau:** Càng đọc theo góc nhìn của nhân vật chính, tôi càng có cảm giác như chính mình cũng đang phiêu lưu trong thế giới của <i>Solaris Thứ Hai</i>...

**Vì sao sửa:** JP-first Maqi: use toi-cau consistently.

### 2. Event_DRSL_1_21 — Maqi

**Trước:** Trong cuộc họp hôm qua, có vài đồng nghiệp nhắc đến chuyện gần đây Kim Châu vừa đón một vị khách hiểu biết rộng rãi... thế nên đã nhờ mình tìm gặp cậu.

**Sau:** Trong cuộc họp hôm qua, mấy đồng nghiệp có nhắc đến một vị khách mới tới Kim Châu, nghe nói rất từng trải, hiểu biết rộng... nên mọi người nhờ tôi tìm đến cậu.

**Vì sao sửa:** CN+JP+EN scene review: natural game-development Vietnamese; Maqi uses toi-cau, Rover uses minh in options; preserve source meaning and branch intent.

### 3. Side_CXLR_7_18 — Lianfeng

**Trước:** Này, ngươi đã từng nghe nói về hiện tượng Vượt Tần của Người Cộng Hưởng chưa?

**Sau:** Này, cậu đã từng nghe nói về hiện tượng Vượt Tần của Người Cộng Hưởng chưa?

**Vì sao sửa:** Thuật ngữ theo project: Vượt Tần / Người Cộng Hưởng.

### 4. Side_LFZYDSPH_39_1 — ???

**Trước:** Cứu mạng với! Cứu mạng với...

**Sau:** Cứu với! Cứu tôi với—!

**Vì sao sửa:** Danh tính speaker bị ẩn; không suy đoán tên.

### 5. Character_JiYan_17_1

**Trước:** Nơi này là... thành phố Kim Châu?

**Sau:** Đây là... Kim Châu sao?

**Vì sao sửa:** Option/player line; không có speaker source.

### 6. Character_JiYan_23_16 — Jiyan

**Trước:** Tàn Tượng vẫn luôn dung hợp với tần số của họ, dần dần tự coi mình là Dạ Quy... Ngày qua ngày lặp lại những chuyện đã phát sinh trong ký ức của Dạ Quy.

**Sau:** Những Tàn Tượng liên tục hòa vào tần số của họ, rồi dần tin rằng chính mình là Dạ Quy... ngày qua ngày tái hiện những gì từng xảy ra trong ký ức của Dạ Quy.

**Vì sao sửa:** Đối chiếu CN+JP+EN và ngữ cảnh cho cùng một ý; câu cũ bị sượng/calque, câu mới tự nhiên hơn mà không đổi nghĩa.

### 7. POI_SMBD_5_86

**Trước:** Để mình cũng phụ giúp một tay nào.

**Sau:** Để tôi giúp một tay.

**Vì sao sửa:** Đối chiếu CN+JP+EN và ngữ cảnh; sửa cách diễn đạt sượng/lệch nghĩa và giữ đúng ý của cảnh.

### 8. Huanglong_main_1_2EX_43_13 — Timid Civilian?

**Trước:** Những thứ này... đều cho chúng tớ sao? Cảm ơn... cảm ơn!

**Sau:** Mấy thứ này... đều cho bọn tôi hết sao? Cảm ơn... cảm ơn!

**Vì sao sửa:** CN 都给我们 và EN all of these: thêm 'hết' cho khẩu ngữ Việt tự nhiên.

### 9. POI_HKSM_XZ_9_6 — Huaxi

**Trước:** Chú Lôi lại tự nói gì thế, em được anh trai nhờ giúp {PlayerName} cùng giải quyết vấn đề khởi động lại Tháp Tinh Luyện.

**Sau:** Chú Lôi đang nói gì vậy? Anh trai nhờ tôi cùng {PlayerName} giải quyết chuyện khởi động lại Tháp Tinh Luyện.

**Vì sao sửa:** CN/EN là hỏi trực tiếp Chú Lôi; bỏ calque 'lại tự nói gì thế'.

### 10. Character_LingYang_38_31

**Trước:** Tại sao nó lại khao khát trở thành Loài người đến vậy?

**Sau:** Tại sao nó lại muốn trở thành con người đến vậy?

**Vì sao sửa:** CN/JP cùng nhấn mức độ “đến vậy”.

### 11. Event_DRSL_39_3

**Trước:** Ở vị trí nào?

**Sau:** Người đó đang ở đâu?

**Vì sao sửa:** JP có その人 = người đó; EN giải thích đó là người mê sách.

### 12. Daliy_DYBL_20_7

**Trước:** Mình sẽ khuyên ông ấy.

**Sau:** Tôi sẽ khuyên ông ấy.

**Vì sao sửa:** Rover giữ ngôi thứ nhất là “tôi”; CN có ngôi thứ nhất, JP là “wakatta”, và cách xưng hô này khớp các câu cùng cảnh.

### 13. Event_THCQDYBF_37_12

**Trước:** Đã có điều Bất thường, vậy thì đi xem thử đi.

**Sau:** Nếu đã thấy có gì bất thường thì cứ đi xem thử.

**Vì sao sửa:** CN 既然有异常 / JP おかしな点があるなら: điều kiện 'nếu/đã' tự nhiên hơn 'đã có điểm bất thường'.

### 14. POI_QSMG_16_5 — NPC #83

**Trước:** Abby thuật lại lời lảm nhảm của vị Hiệp Sĩ Điên một cách có mẫu có dạng, ta quan sát Cosimo, sự thay đổi trong đồng tử của ông ấy đã cho ta câu trả lời mà ta muốn.

**Sau:** Abby bắt chước lại lời lảm nhảm của Hiệp Sĩ Điên một cách ra trò. Bạn quan sát Cosimo; chỉ từ phản ứng trong ánh mắt của cậu ta, bạn đã có được câu trả lời mình cần.

**Vì sao sửa:** CN nói biến đổi đồng tử; JP nói nhìn đôi mắt là biết Cosimo đang biết điều gì đó.

### 15. Flow_6700001_31 — Uncle Dong

**Trước:** {PlayerName}, đến xem vận may hôm nay đi, tôi rất tò mò hôm nay bạn sẽ kết duyên với vị Tuế Chủ nào đó?

**Sau:** {PlayerName}, lại đây xem vận may hôm nay nào. Ta cũng tò mò không biết hôm nay cháu sẽ có duyên với vị Tuế Chủ nào.

**Vì sao sửa:** Chú Đổng dùng giọng lão trong JP (わし/〜じゃ/〜のう); giữ ta, đổi cậu→cháu để khớp cách ông gọi Rover/Jianxin ở các cảnh khác.

### 16. Character_Encore_49_31 — Encore

**Trước:** Không sao đâu, tuy câu chuyện này đã kết thúc, nhưng lần sau gặp lại, Encore sẽ chuẩn bị một cuộc phiêu lưu hoàn toàn mới!

**Sau:** Không sao đâu! Cuộc phiêu lưu này có kết thúc thì cũng đừng buồn nhé. Lần sau gặp lại, Encore sẽ chuẩn bị một câu chuyện phiêu lưu hoàn toàn mới!

**Vì sao sửa:** JP có sắc thái an ủi rõ hơn CN; giữ chất giọng vui của Encore.

### 17. Main_Linaxita_2_2_54_6 — Gate Keeper

**Trước:** Tôi sẽ sắp xếp ngay. Hai vị cứ đi thẳng đến cổng chính, sẽ có người ra đón.

**Sau:** Tôi sẽ lập tức sắp xếp. Hai vị cứ đi thẳng đến cổng chính; khi tới nơi sẽ có người ra đón.

**Vì sao sửa:** JP ghi rõ お二人 = hai người; CN chỉ nói chung “các vị”.

### 18. POI_LGNJR_4_4 — Phoebe

**Trước:** Hử? {PlayerName} vừa nãy có phát hiện gì sao?

**Sau:** Ừm? {PlayerName}, vừa rồi cậu có phát hiện gì sao?

**Vì sao sửa:** JP dùng `{PlayerName}-san` và câu hỏi lịch sự; đổi sang “Ừm?” cho mềm hơn, đồng thời giữ cách gọi “cậu” theo cảnh.

### 19. Event_AYKXBRZM_99_10

**Trước:** Chúng ta rời khỏi đây trước đã.

**Sau:** {Male=Trước hết, chúng ta rời khỏi đây đã.;Female=Trước hết, chúng ta rời khỏi đây đã.}

**Vì sao sửa:** CN 我们 và JP ここを出よう: Rover đề nghị cả hai rời đi; thêm chúng ta để câu Việt rõ và tự nhiên.

### 20. Guide_QDMFDYX_2_2 — "Cosmos"

**Trước:** Ờ, có lẽ cậu sẽ cảm thấy bối rối trước bộ dạng hiện tại của tôi… Tôi đúng là Người Gác Cổng Hắc Ám đã liên lạc với cậu trước đó.

**Sau:** À... có lẽ cậu sẽ thấy khó hiểu vì bộ dạng hiện giờ của tôi. Nhưng đúng vậy, tôi chính là Người Gác Cổng Hắc Ám đã liên lạc với cậu trước đó.

**Vì sao sửa:** CN/EN nói rõ “đã liên lạc trước đó”; JP ngắn hơn nhưng cùng nhận dạng.

### 21. HD_XK_14_5

**Trước:** Quả thật rất mang phong cách của thi nhân.

**Sau:** {Male=Đúng là rất ra dáng một nhà thơ.;Female=Đúng là rất ra dáng một nhà thơ.}

**Vì sao sửa:** JP có nhánh khẩu khí nam/nữ; giữ cấu trúc runtime.

### 22. kamola_shengtai_1_8 — Carlotta

**Trước:** Đều không phải.

**Sau:** Không. Hôm nay tôi chẳng hẹn gặp ai, cũng không chờ ai đến cả.

**Vì sao sửa:** CN+JP+EN + cùng state: làm rõ hai câu hỏi Rover mà Carlotta đang phủ định; giữ giọng Carlotta tao nhã, thư thái; Việt hóa tự nhiên, không dịch rời từng câu.

### 23. Main_Linaxita_2_8_6_133

**Trước:** Ném chiếc mũ bảo hiểm cho Viktor.

**Sau:** Ném chiếc mũ giáp cho Viktor.

**Vì sao sửa:** Option/action line, không phải lời thoại trực tiếp.

### 24. BVBHD_42_9

**Trước:** Furius nhận được một cuộc liên lạc, ban đầu lộ ra vẻ vô cùng kinh ngạc, rồi liên tục gật đầu và cúi chào, vẻ mặt dần trở nên hân hoan.

**Sau:** Furius nhận được một cuộc liên lạc. Ban đầu anh ta lộ vẻ vô cùng kinh ngạc, rồi liên tục gật đầu và cúi chào; nét mặt dần chuyển sang vui mừng.

**Vì sao sửa:** Narration; CN/JP thống nhất chuỗi biểu cảm.

### 25. Main_LahaiRoi_3_1_33_1

**Trước:** Phương án vừa rồi... liệu có kịp đợt thử nghiệm cuối cùng không? Liệu có quá bảo thủ không?

**Sau:** Phương án vừa rồi... liệu có kịp đợt thử nghiệm cuối không? Có phải hơi dè dặt quá không?

**Vì sao sửa:** CN 保守 / JP 控えめ: 'dè dặt' hợp giọng tự cân nhắc hơn cấu trúc 'có phải hơi thận trọng quá không'.

### 26. lmzxst_29_1 — Actor No. 1

**Trước:** Ấy, cậu không phải là {PlayerName} đó chứ!

**Sau:** Ồ, cậu chẳng phải là {PlayerName} sao!

**Vì sao sửa:** CN/JP đều là nhận ra người chơi.

### 27. GNNPC_HEIHAIC_7_4

**Trước:** ...

**Sau:** ……

**Vì sao sửa:** Không có nội dung ngôn ngữ để dịch.

### 28. Main_LahaiRoi_3_1_5001_15 — Lynae

**Trước:** Vậy à... Mong là sớm tìm ra nguyên nhân.

**Sau:** Vậy à... mong là sớm tìm ra nguyên nhân.

**Vì sao sửa:** CN/JP cùng là lời chúc sớm tìm được nguyên nhân.

### 29. Side_ZLBQDJB_20_12 — Mechascout (?)

**Trước:** Cảnh báo...... lập tức sơ tán. Đếm ngược...... 5...... 4......

**Sau:** Cảnh báo... lập tức rời khỏi khu vực. Đếm ngược... 5... 4...

**Vì sao sửa:** Mechascout/system-like warning.

### 30. NPC_LHLYMJS_16_2

**Trước:** Ôm sao?

**Sau:** Ôm á?

**Vì sao sửa:** Đối chiếu CN/JP/EN; giữ sắc thái thân thiện, ấm áp và tránh dịch từng chữ.

### 31. TWTPOI_27_35 — Talkie

**Trước:** "Trước khi tôi đến Đài Trắc Giới Thâm Không làm việc, Sephira luôn không rời tôi nửa bước. Cả khi tôi tĩnh dưỡng tại Học Viện, cô ấy cũng bận rộn ngược xuôi, giúp tôi xử lý đủ loại thủ tục." Dhalifa nói vậy đấy!

**Sau:** “Trước khi tôi đến làm việc ở Đài Trắc Giới Thâm Không, Sephira luôn ở bên tôi, gần như chẳng rời nửa bước. Hồi tôi dưỡng bệnh ở Học Viện, cô ấy cũng tất tả ngược xuôi, lo liệu đủ thứ thủ tục cho tôi.” Dhalifa nói thế đấy!

**Vì sao sửa:** Đọc theo nhánh Sephira: Dhalifa nói ngắn; Talkie đọc ý nghĩ và diễn đạt đầy đủ. Đối chiếu CN/JP/EN.

### 32. Main_HuangLong_XLZHDLY_390_6 — ???

**Trước:** …"Kẻ tế thanh", ngươi… đã nghĩ thông chưa?

**Sau:** …“Người Tế Thanh”, cô… đã quyết định chưa?

**Vì sao sửa:** Giọng ẩn đang nói với Yangyang. JP dùng thể lịch sự でしょうか/です/ます và あなた ở các câu kế; ngươi quá thô/cổ phong.

### 33. Side_Coop_StingerDate_5_13 — NPC #83

**Trước:** Có vẻ không phải thể loại phim Mornye sẽ thích... Nghĩ lại đã.

**Sau:** Có vẻ đây không phải kiểu phim Mornye thích... thôi, rủ người khác vậy.

**Vì sao sửa:** JP 他の人を誘おう nói rõ rủ người khác; sửa liên từ cho tự nhiên.

### 34. AYJJZS_11_7

**Trước:** Tại sao lại không cho các em dùng?

**Sau:** {Male=Sao Linnea lại không cho các em dùng?;Female=Sao Linnea lại không cho các em dùng?}

**Vì sao sửa:** Bada ngay trước nói Linnea không cho chúng em dùng; CN 你们 là Bada + nhóm Nhật Linh. Giữ trực tiếp Bada nhưng VI phải là Linnea/các em, không phải họ/các cậu.

### 35. XDQY_14_3 — NPC #83

**Trước:** Mình nhìn chằm chằm anh ta... Anh ta khựng lại, ánh mắt né tránh đầy vi diệu.

**Sau:** Bạn nhìn chằm chằm vào anh ta... anh ta khựng lại một nhịp rồi khẽ lảng ánh mắt đi.

**Vì sao sửa:** Narration/player perspective; CN/JP thống nhất hành vi tránh ánh nhìn.

### 36. Side_XBDXD_15_6

**Trước:** Đưa ảnh cho các TARD-E xem.

**Sau:** Bạn đưa ảnh cho các TARD-E xem.

**Vì sao sửa:** Option/narration; người nhận là nhóm TARD-E.

### 37. SIDE_HADSJ_16_1 — Zenus

**Trước:** Lâu rồi không gặp, {PlayerName}, cậu còn nhớ mình không?

**Sau:** Lâu rồi không gặp, {PlayerName}. Cậu vẫn nhớ tôi chứ?

**Vì sao sửa:** JP 僕 + khẩu khí thân mật; giữ “tôi/cậu” trung tính.

### 38. Main_HuangLong_XLZHDLY_470_24 — Qiuhong

**Trước:** Đã vậy, tôi tạm thời không làm phiền nữa. Có việc gấp, tôi sẽ liên lạc lại với hai vị.

**Sau:** Nếu vậy, tôi xin phép không làm phiền hai vị nữa. Có việc khẩn cấp, tôi sẽ liên lạc lại.

**Vì sao sửa:** JP dùng お二方 = hai vị, rõ số lượng người nghe.

### 39. 35TUOZUI_16_1 — Jiuwu

**Trước:** Ngài đến rồi, {PlayerName}... đây là Sonija, cô ấy, cô ấy đã tra ra được một số chuyện. Bên Thiên Công Lục...

**Sau:** {PlayerName}, bạn đến rồi... Đây là Sonija. Cô ấy... đã tìm hiểu được một số chuyện rồi. Còn chuyện Thiên Công Lục...

**Vì sao sửa:** CN dùng 你 bình thường; JP dùng `{PlayerName}-san` và thể lịch sự nhưng không đến mức phải gọi “ngài”. Giữ Jiuwu lịch sự, hơi ấp úng và bỏ cách nói calque “tra ra được”.

### 40. Side_XJBGZMTX_25_6 — Black-Haired Girl

**Trước:** "Ước gì có ngày nào đó mình có thể phiêu lưu khắp nơi như {PlayerName} nhỉ."

**Sau:** “Ước gì một ngày nào đó anh cũng được đi đây đi đó phiêu lưu như {PlayerName}.”

**Vì sao sửa:** Đối chiếu CN/JP/EN và toàn scene: hai NPC là anh trai–em gái; giữ anh/em nhất quán, làm tự nhiên lời cô em nhại lại lời anh trai.

### 41. STNPC_JZST_55_6

**Trước:** Được thôi.

**Sau:** {Male=Ừ, không vấn đề gì.;Female=Ừ, được thôi.}

**Vì sao sửa:** JP có hai nhánh khẩu khí nam/nữ khác nhau; giữ khác biệt nhẹ.

### 42. Side_DESLGYMZ_24_16 — Bertolt

**Trước:** Xong rồi, thế này được chưa?

**Sau:** Xong rồi, {PlayerName}. Như thế này ổn chứ?

**Vì sao sửa:** JP có さん, lịch sự nhẹ; EN bỏ tên nhưng CN/JP hỗ trợ giữ người nghe.

### 43. Side_DESLGYMZ_31_12

**Trước:** Bertolt xử lý lỗi theo đề xuất của bạn.

**Sau:** Bertolt xử lý lỗi chương trình theo phương án bạn đề xuất.

**Vì sao sửa:** Narration; CN nói rõ xử lý “program error”, EN diễn giải “modifications”.

## Các nhóm sửa khác

- **Raw English -> VI:** phục hồi các FavorStory/FavorWord và một số mô tả Rogue bị rơi ngược về English.
- **Xưng hô/register:** chuẩn hóa tôi/cậu, ta/cháu, bọn tôi, người đó... theo speaker/listener và CN/JP/EN.
- **Câu calque/máy móc:** viết lại câu Việt theo ý nghĩa cảnh, không bám cấu trúc Trung/Anh từng chữ.
- **Thuật ngữ:** đồng bộ nhiều nhóm như Đánh Thường, Tấn Công Trên Không, Quyền Giáp, Shell Credit, Du Long Tích, Khí, giây...
- **Runtime:** giữ placeholder, gender branch, tag và option/action line; không đổi cấu trúc runtime chỉ để làm câu đẹp.
- **Dev/source sentinel:** không bịa dịch cho source test/test mới chưa có authority.

## Text mới

112 text mới được liệt kê đầy đủ trong V308_GITHUB_TO_R5_ADDED_TEXTS.tsv; nổi bật gồm nhạc nền/nhiệm vụ Yên Vân U Viễn Tâm Kiếm Minh, Ngự Kiếm, Huyền Phương và các error/quest tip mới.

## Sửa thêm ở vòng chốt cuối: Hổ Khẩu

Đây là lỗi carry-over từ 3.0.8: một exact override cũ coi 虎口山脉 / Tiger's Maw là whole-cell ngay cả khi nó chỉ nằm bên trong một câu dài, làm một số text bị co thành mỗi tên địa danh.

Các dòng đã khôi phục:

- Area_12_Title: Mỏ Mỏ Hổ Khẩu → Hổ Khẩu
- QuestChapter_2010_ChapterNum: Mỏ Mỏ Hổ Khẩu → Hổ Khẩu
- QuestChapter_2011_ChapterNum: Mỏ Mỏ Hổ Khẩu → Hổ Khẩu
- Quest_106800006_ChildQuestTip_0_2: Mỏ Mỏ Hổ Khẩu → Giao "Bộ Dữ Liệu Cũ Của Hổ Khẩu" cho người kể chuyện
- Quest_106800006_HandInItemsDesc_0_2: Mỏ Mỏ Hổ Khẩu → Vui lòng giao "Bộ Dữ Liệu Cũ Của Hổ Khẩu" cho người kể chuyện
- Quest_106800006_InteractContent_0_2: Mỏ Mỏ Hổ Khẩu → Mình thu thập được vài bộ dữ liệu cũ ở Hổ Khẩu.
- Quest_106800006_QuestName_0_2: Mỏ Mỏ Hổ Khẩu → Bao Câu Chuyện Cổ Kim · Hổ Khẩu

Bốn text nhiệm vụ trên được đối chiếu với EN/CN live source và các quest cùng series trước khi sửa. Exact override trong project.db và các file translation liên quan cũng đã được sửa, không chỉ vá PAK cuối.
