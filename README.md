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

**Shop chỉ chạy mua demo trong Unity Editor, chưa có thanh toán thật hoặc SDK quảng cáo.** Ví demo Editor tách khỏi ví bản build. Vật phẩm được lưu và hiển thị; sử dụng booster trong gameplay chưa được triển khai. Thời gian reset dùng đồng hồ UTC của thiết bị.

## Kiểm tra đã chạy

- Bộ kiểm tra toàn luồng: **2.711 mục PASS** trước điều chỉnh cuối của popup.
- Kiểm tra vòng quay sau điều chỉnh: **503 mục PASS**.
- Kiểm tra Daily/Shop và popup dùng chung sau điều chỉnh: **814 mục PASS**.
- Độ phân giải 1080×1920, 1440×2960, mô phỏng tai thỏ/vùng cử chỉ và đổi kích thước khi popup đang mở; Console cuối không lỗi hoặc cảnh báo.
- Các bài kiểm tra chạy trong Unity Editor, khôi phục ví, vòng quay, tùy chọn và tiến trình sau khi chạy. Chưa kiểm tra trên thiết bị Android/iOS thật.

Báo cáo: [tổng hợp](Captures/Figma/CompletionChecklist.md), [vòng quay](Captures/Figma/LuckyWheelVerification.md), [Daily/Shop](Captures/Figma/CommerceVerification.md), [toàn luồng](Captures/Figma/MenuVerification.md). Ảnh sau điều chỉnh: [1080×1920](Captures/Figma/LuckyWheel_CoinReward_Unity_1080x1920.png), [1440×2960](Captures/Figma/LuckyWheel_CoinReward_Unity_1440x2960.png).

Để chạy lại bộ kiểm tra giao diện, mở Home trong Play Mode rồi gọi `BlockCrush.UI.Editor.BlockCrushMenuVerification.Start()`, `StartWheel()` hoặc `StartCommerce()`. Không chạy các bộ kiểm tra cùng lúc. Core Classic có menu `Block Crush > Verify Current Scene in Play Mode`.

Chi tiết kỹ thuật: [gameplay](Assets/Scripts/BlockCrush/README.md), [UI và dữ liệu](Assets/BlockCrush/Scripts/UI/README.md). Nguồn thiết kế Figma và thông tin xuất artwork được giữ trong `Captures/Figma`; font có giấy phép đi kèm trong Assets.
