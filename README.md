# Block Crush — Unity 6

Project game xếp khối với Classic, Adventure 10 level, Lucky Wheel, Daily Tasks và Shop demo.

## Mở project

1. Clone repository và thêm thư mục project trong Unity Hub.
2. Dùng **Unity 6000.0.68f1** theo `ProjectSettings/ProjectVersion.txt`.
3. Đợi Unity tải packages và import assets ở lần mở đầu tiên.
4. Mở `Assets/Scenes/Home.unity`, sau đó nhấn Play.

Build Settings đã có thứ tự Home → Classic → Adventure. Giao diện thiết kế cho màn hình dọc, dùng Canvas 1080×1920 và Safe Area. `Assets`, `Packages`, `ProjectSettings` và các file `.meta` được lưu trong Git; cache Unity và file IDE được tạo lại tại máy người dùng.

## Chức năng

- Classic: kéo/thả khối bằng chuột hoặc cảm ứng, xóa hàng/cột, combo, điểm cao nhất, thua và chơi lại.
- Adventure: bản đồ chọn 10 level, mục tiêu điểm và giới hạn lượt, mở khóa tuần tự, lưu tiến trình và điểm cao nhất từng level.
- Lucky Wheel: 8 phần thưởng, một lượt miễn phí mỗi ngày UTC, animation và âm thanh, lưu lượt đang quay và cộng vật phẩm một lần. Popup “You get” nằm trọn, căn giữa trong panel vòng quay.
- Daily Tasks: hoàn thành 2/4/6/8/10 ván Adventure; mỗi mốc nhận 100 xu, reset lúc 00:00 UTC, không nhận trùng thưởng.
- Shop: tab Bundles/Items, cuộn danh sách, xác nhận/hủy mua demo, cộng đúng vật phẩm và trạng thái Owned cho Remove Ads.
- Settings: bật/tắt âm thanh, nhạc và rung, lưu tùy chọn; giao diện thích ứng kích thước màn hình.
- Booster dùng được trong Classic và Adventure: Bom dọn vùng 3×3; Reverse đổi các khối còn lại; +1 biến khối lớn nhất trên khay thành khối đơn 1×1. Chỉ trừ một vật phẩm khi có tác dụng, không cộng điểm hoặc lượt bằng booster.

**Shop chỉ chạy mua demo trong Unity Editor, chưa có thanh toán thật hoặc SDK quảng cáo.** Ví demo Editor tách khỏi ví bản build. Booster dùng vật phẩm đã nhận từ Shop hoặc Lucky Wheel và lưu số dư sau mỗi lần dùng thành công. Thời gian reset dùng đồng hồ UTC của thiết bị.

## Kiểm tra đã chạy

- Bộ kiểm tra toàn luồng sau căn chỉnh bố cục: **3.302 mục PASS**; riêng booster Classic/Adventure **344 mục PASS**; Daily/Shop **839 mục PASS**; core Classic bằng chuột/cảm ứng **65 mục PASS**; kiểm tra cache, HUD, layout và dữ liệu **43 mục PASS**.
- Dựng booster hai lần trên mỗi scene tạm không tạo trùng object/callback. Compile script Player Windows có **26 assembly PASS**; 27 tùy chọn/tiến trình và hai bản JSON ví/vòng quay được đối chiếu, khôi phục đúng dữ liệu trước kiểm tra.
- Công cụ dựng UI: **6 mục PASS** trong scene tạm. Compile Editor và script Player Windows PASS; Console cuối không lỗi hoặc cảnh báo.
- Các lần kiểm tra riêng trước đó cho bản sửa popup: **513 mục vòng quay PASS**, **838 mục Daily/Shop PASS**.
- Độ phân giải 1080×1920, 1440×2960, mô phỏng tai thỏ/vùng cử chỉ và đổi kích thước khi popup đang mở; Console cuối không lỗi hoặc cảnh báo.
- Các bài kiểm tra chạy trong Unity Editor, khôi phục ví, vòng quay, tùy chọn và tiến trình sau khi chạy. Chưa kiểm tra trên thiết bị Android/iOS thật.

Báo cáo: [căn chỉnh bố cục và bỏ chữ phụ](Captures/Figma/GameplayLayoutReview.md), [booster và phạm vi thay đổi](Captures/Figma/BoosterReview.md), [kiểm tra booster](Captures/Figma/BoosterVerification.md), [tối ưu code](Captures/Figma/OptimizationReview.md), [kiểm tra cache/layout](Captures/Figma/OptimizationVerification.md), [tổng hợp](Captures/Figma/CompletionChecklist.md), [vòng quay](Captures/Figma/LuckyWheelVerification.md), [Daily/Shop](Captures/Figma/CommerceVerification.md), [toàn luồng](Captures/Figma/MenuVerification.md). Ảnh booster: [Classic](Captures/Figma/Classic_Boosters_Unity_1080x1920.png), [Adventure](Captures/Figma/Adventure_Boosters_Unity_1080x1920.png); chọn vùng bom: [Classic](Captures/Figma/Classic_BombTarget_Unity_1080x1920.png), [Adventure](Captures/Figma/Adventure_BombTarget_Unity_1080x1920.png).

Để chạy lại bộ kiểm tra giao diện, mở Home trong Play Mode rồi gọi `BlockCrush.UI.Editor.BlockCrushMenuVerification.Start()`, `StartWheel()`, `StartCommerce()`, `StartOptimization()` hoặc `StartBoosters()`. Bộ toàn luồng có kiểm tra booster ở cả hai màn chơi. Có thể gọi qua Unity MCP khi Editor chạy ngầm; helper đổi độ phân giải dùng Game View đã có sẵn và không mở/focus tab. Không chạy các bộ kiểm tra cùng lúc. Core Classic có menu `Block Crush > Verify Current Scene in Play Mode`; bộ này chỉ chạy trong scene Classic.

Chi tiết kỹ thuật: [gameplay](Assets/Scripts/BlockCrush/README.md), [UI và dữ liệu](Assets/BlockCrush/Scripts/UI/README.md). Nguồn thiết kế Figma và thông tin xuất artwork được giữ trong `Captures/Figma`; font có giấy phép đi kèm trong Assets.

Daily và Lucky Wheel mở trên Home với lớp nền đen bán trong suốt, không dùng background riêng. Hai panel có cùng kích thước và căn giữa Safe Area. Popup nhận thưởng vẫn hoạt động nếu thiếu tham chiếu nút Cancel; scene đã được nối lại đầy đủ. Phần thưởng chưa xác nhận được hiện lại khi mở vòng quay và không cộng trùng vật phẩm.

Đã rà soát màu chữ trên toàn bộ giao diện: dùng chữ đậm màu trên nền trắng/tím nhạt/vàng/xanh, thêm viền cho chữ trên nền thay đổi. Số dư xu của popup thưởng và Shop đã đổi từ vàng sang tím đậm. [Báo cáo tương phản](Captures/Figma/TextContrastReview.md) có số đo và ảnh Unity mới.

Board Classic đã được nâng lên, booster chừa khoảng trống trên đáy Safe Area và khay block được căn lại để không che nút. Chữ kéo khối và nhãn phát triển “demo” đã được bỏ khỏi giao diện theo yêu cầu; cơ chế mua thử trong Editor vẫn giữ nguyên.
