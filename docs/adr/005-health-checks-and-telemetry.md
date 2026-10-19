# ADR 005: Giám Sát Sức Khỏe Hệ Thống (Health Checks & Telemetry)

## 1. Trạng thái
**ĐÃ PHÊ DUYỆT (ACCEPTED)** — Ngày: 13/10/2026

## 2. Bối cảnh
Khi triển khai sản xuất trên hạ tầng Docker/Kubernetes hoặc môi trường Enterprise on-premise:
- Hệ thống cần cung cấp các probes tiêu chuẩn cho orchestrator để tự động tái khởi động hoặc định tuyến lưu lượng (Liveness & Readiness probes).
- Quản trị viên hệ thống (IT Admin) cần một giao diện trực quan theo dõi tức thì trạng thái kết nối CSDL SQL Server, dung lượng RAM đã cấp phát, dung lượng ổ đĩa còn trống, và thời gian hoạt động liên tục (Uptime).

## 3. Quyết định Kiến trúc
1. **Custom Health Checks**:
   - `DatabaseHealthCheck`: Thực thi `DbContext.Database.CanConnectAsync()` kiểm tra kết nối SQL Server.
   - `MemoryHealthCheck`: Giám sát `GC.GetTotalMemory()` với ngưỡng cảnh báo 1GB.
   - `DiskSpaceHealthCheck`: Kiểm tra `DriveInfo.AvailableFreeSpace` với ngưỡng an toàn $\ge 5\%$.
2. **Standard Probes**:
   - `/health/live`: Dành cho Kubernetes Liveness Probe.
   - `/health/ready`: Dành cho Kubernetes Readiness Probe.
   - `/health/status`: Trả về JSON theo chuẩn REST API với thời gian phản hồi từng probe.
3. **Health Dashboard UI (`/Health`)**:
   - Bảng điều khiển giám sát thời gian thực với các thẻ chỉ số (Metric Cards), bảng chi tiết kết quả probe, thông tin cấu hình môi trường máy chủ và cơ chế làm mới 1-click.

## 4. Hệ quả & Lợi ích
- Đảm bảo tính sẵn sàng cao (High Availability).
- Phát hiện sớm và chủ động cảnh báo các nguy cơ cạn kiệt tài nguyên bộ nhớ hoặc đĩa cứng.
- Tương thích 100% với các hệ thống giám sát Prometheus, Grafana, Datadog.
