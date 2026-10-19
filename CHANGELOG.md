# LỊCH SỬ THAY ĐỔI & NHẬT KÝ PHÁT TRIỂN (CHANGELOG)
## HỆ THỐNG QUẢN LÝ VÀ ĐÁNH GIÁ KPI - HỌC VIỆN HÀNH CHÍNH VÀ QUẢN TRỊ CÔNG

Tất cả các thay đổi đáng chú ý của dự án qua 12 phiên làm việc được ghi nhận chi tiết tại đây.

---

### [Phiên 12/12] — Tối Ưu Hóa, SEO, Testing & Production-Ready (Phiên Hoàn Thiện & Bàn Giao)
**Ngày:** 13/10/2026
- **Performance Optimization**:
  - Tích hợp `AddResponseCompression` hỗ trợ thuật toán Brotli & Gzip (giảm 60-80% băng thông).
  - Tối ưu EF Core queries: Bổ sung `.AsNoTracking()` cho toàn bộ truy vấn đọc trong `CatalogService` và `SearchService`.
  - Cấu hình `IMemoryCache` (Cache-Aside Pattern) cho 18 danh mục nghiệp vụ dùng chung.
  - Tạo kịch bản tối ưu hóa chỉ mục CSDL `optimize_indexes.sql` cho 10 bảng thực thể lớn.
  - Thêm `RequestPerformanceMiddleware` giám sát thời gian thực thi request và gắn header `X-Response-Time-Ms`.
- **SEO Best Practices**:
  - Bổ sung động `title`, `description`, `keywords`, `canonical` trong `_Layout.cshtml`.
  - Tích hợp Open Graph tags (`og:title`, `og:image`, `og:type`) & Twitter Cards.
  - Tích hợp Structured Data JSON-LD (`EducationalOrganization`, `WebApplication`).
  - Phát sinh tự động `/sitemap.xml` chuẩn Schema 0.9 và file điều hướng `/robots.txt`.
  - Chuẩn hóa HTML5 ngữ nghĩa (`header`, `nav`, `main`, `section`, `article`, `footer`, phân cấp h1 duy nhất).
- **Health Checks & Telemetry**:
  - Triển khai custom health checks: `DatabaseHealthCheck`, `MemoryHealthCheck`, `DiskSpaceHealthCheck`.
  - Cung cấp endpoints: `/health/live`, `/health/ready`, `/health/status` (JSON REST API).
  - Xây dựng giao diện Dashboard Giám Sát Sức Khỏe Chuyên Nghiệp tại `/Health`.
- **Testing & Quality Assurance**:
  - Viết bộ Integration Tests `HealthAndSeoEndpointsTests` bao phủ Controllers, HealthChecks, SEO và ca biên BR-01, BR-10.
  - Tổng số **113/113 Unit & Integration Tests PASSED 100%**.
  - Kịch bản E2E Python: **11/11 kịch bản kiểm thử PASSED 100%**.
- **Docker & Deployment**:
  - Xây dựng `Dockerfile` multi-stage build cho .NET 8 LTS (chạy user không đặc quyền `app`).
  - Xây dựng `docker-compose.yml` tích hợp trọn vẹn Stack: Web App + SQL Server 2022 + Redis 7 Cache.
  - Cấu hình đa môi trường: `appsettings.Development.json`, `appsettings.Staging.json`, `appsettings.Production.json`.
  - Xử lý trang lỗi tùy biến: `/Home/Error/{statusCode}` hỗ trợ 404, 403, 500, 401.
- **Documentation**:
  - Tích hợp Swagger / OpenAPI Specification và Swagger UI tại `/swagger`.
  - Cập nhật trọn vẹn `README.md`, `CHANGELOG.md`, và 5 bộ hồ sơ kiến trúc `ADR/`.

---

### [Phiên 11/12] — Notifications, Email & Tính Năng Bổ Sung Enterprise
**Ngày:** 13/10/2026
- Triển khai Real-time Notifications bằng SignalR Hub (`/hubs/notification`).
- Thiết kế Bell icon trên topbar với badge số đếm chưa đọc thời gian thực.
- Phân loại 4 nhóm thông báo: Info, Warning, Success, Urgent.
- Web Audio API tổng hợp âm thanh thông báo và toast popup slide-in animation.
- Tự động phát sinh 6 sự kiện thông báo: KPI được giao, Nộp tự chấm, Hoàn thành thẩm định, Phê duyệt, Nhắc hạn chót, Cảnh báo trần 20%.
- Email Background Service (`EmailQueueBackgroundService`) quét hàng đợi bảng `email_queues` mỗi 30s với cơ chế retry 3 lần.
- Trang Hồ sơ & Cài đặt cá nhân (`/Profile`): Upload avatar, đổi mật khẩu BCrypt, cài đặt loại thông báo, theme Light/Dark, lịch sử 10 lần đăng nhập gần nhất.
- Command Palette (`Ctrl+K`): Tìm kiếm đa chiều nhanh chóng cán bộ, KPI, danh mục, trang.

---

### [Phiên 10/12] — Module Báo Cáo, Phân Tích & Chuông Gauss
**Ngày:** 12/10/2026
- Xây dựng trang Báo cáo Tổng hợp (`/Report/Index`) với bộ lọc dính đa tiêu chí.
- Bảng vinh danh Leaderboard Top 3 cá nhân và xếp hạng hiệu suất toàn viện (`/Report/Leaderboard`).
- Phân tích biểu đồ phân phối chuẩn Bell Curve (Gauss Distribution) tương tác (`/Report/Gauss`).
- Xuất báo cáo chuyên nghiệp định dạng Excel (.xlsx qua ClosedXML) và PDF (.pdf A4 qua QuestPDF).

---

### [Phiên 09/12] — Module Lương 3P (P1, P2, P3) & Chi Tiền Mặt
**Ngày:** 12/10/2026
- Bảng tính lương theo kết quả KPI (`Lương_P3 = Lương_Cơ_Bản × Hệ_Số_Lương × Hệ_Số_KPI`).
- Áp dụng hệ số KPI theo xếp loại: Xuất sắc (1.3), Tốt (1.15), Đạt (1.0), Không đạt (0.7).
- In Phiếu chi tiền mặt Mẫu 02-TT theo Thông tư 133/2016/TT-BTC kèm dịch số thành chữ tiếng Việt chuẩn xác.
- In Bảng kê ký nhận tiền mặt tại quỹ Mẫu 05a.
- Phiếu lương cá nhân (My Payslip) tra cứu 3P minh bạch.

---

### [Phiên 08/12] — Phân Loại Cán Bộ Công Chức Theo Nghị Định 90 & 48
**Ngày:** 12/10/2026
- Triển khai bảng theo dõi cán bộ công chức với mã ngạch bậc, phòng ban, xếp loại.
- Đồng hồ kiểm soát trần 20% (Gauge Meter) thực thi quy tắc BR-06: Khống chế tỷ lệ Hoàn thành xuất sắc nhiệm vụ $\le 20\%$.
- Kiểm tra điều kiện điểm liệt / kỷ luật (BR-07) và thẩm quyền quyết định (BR-08).
- Xuất biểu mẫu Quốc gia Mẫu 01 (Cán bộ lãnh đạo) & Mẫu 02 (Cán bộ chuyên môn) khổ in A4.

---

### [Phiên 07/12] — Module Đánh Giá KPI & Workflow Engine
**Ngày:** 12/10/2026
- Phiếu Tự Đánh Giá (Self-Assessment) dành cho Giảng viên / Nhân viên.
- Thực thi BR-10: Tự động tính completion rate, chặn trần MaxCap 150%.
- Thực thi BR-09: Khi đạt $\ge 120\%$, bật cảnh báo và bắt buộc đính kèm minh chứng.
- Quản lý thẩm định điểm (Manager Review) và giải quyết khiếu nại (BR-14, BR-15).

---

### [Phiên 01/12 đến 06/12] — Khởi Tạo Nền Tảng, Clean Architecture, Danh Mục & Thiết Lập KPI
**Ngày:** 10/10/2026 – 12/10/2026
- Khởi tạo kiến trúc Clean Architecture .NET 8 LTS (Domain, Application, Infrastructure, Web, Tests).
- Thiết kế và chuẩn hóa 40 bảng thực thể CSDL SQL Server.
- Quản trị 18 danh mục nghiệp vụ dùng chung & Ngân hàng chỉ tiêu KPI chuẩn BSC.
- Phân hệ Giao chỉ tiêu KPI (Planning Wizard) kiểm soát tổng trọng số 100% (BR-01, BR-02).
- Xác thực Cookie Authentication, phân quyền RBAC 6 vai trò kết hợp Data Scope.
