# ADR 002: Hệ thống Thông báo Thời gian thực (SignalR & Dependency Inversion)

## 1. Trạng thái
**ĐÃ PHÊ DUYỆT (ACCEPTED)** — Ngày: 13/10/2026

## 2. Bối cảnh
Quy trình đánh giá KPI diễn ra theo nhiều bước liên hoàn (Giao chỉ tiêu $\to$ Tự chấm $\to$ Thẩm định $\to$ Phê duyệt $\to$ Khiếu nại). Người dùng cần nhận được thông báo ngay lập tức trên giao diện web mà không phải tải lại trang (F5).
Đồng thời, theo nguyên tắc Dependency Inversion (DIP) của Clean Architecture:
- `KPI.Infrastructure` không được phép phụ thuộc trực tiếp vào `KPI.Web`.
- `KPI.Infrastructure` là nơi phát sinh sự kiện thông báo (ví dụ: `NotificationService`).

## 3. Quyết định Kiến trúc
1. Khai báo giao diện `IRealtimeNotifier` tại tầng `KPI.Application`:
   ```csharp
   public interface IRealtimeNotifier
   {
       Task SendNotificationToUserAsync(long userId, NotificationDto notification);
       Task BroadcastNotificationAsync(NotificationDto notification);
   }
   ```
2. Thực thi `SignalRRealtimeNotifier` tại tầng `KPI.Web`, inject `IHubContext<NotificationHub>`.
3. `NotificationService` (nằm trong `KPI.Infrastructure`) chỉ giao tiếp thông qua `IRealtimeNotifier`.
4. Client Web kết nối WebSocket tại `/hubs/notification` kèm âm thanh Web Audio API tổng hợp (không phụ thuộc file âm thanh ngoài) và slide-in Toast animations.

## 4. Hệ quả & Lợi ích
- Tuân thủ 100% Dependency Rule của Clean Architecture.
- Độ trễ thông báo $< 50$ ms.
- Fallback mượt mà sang Server-Sent Events (SSE) hoặc Long Polling nếu mạng chặn WebSockets.
