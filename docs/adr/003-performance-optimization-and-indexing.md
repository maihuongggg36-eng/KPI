# ADR 003: Tối ưu Hóa Hiệu Năng, Caching & Chỉ Mục CSDL

## 1. Trạng thái
**ĐÃ PHÊ DUYỆT (ACCEPTED)** — Ngày: 13/10/2026

## 2. Bối cảnh
Hệ thống quản lý 40 bảng thực thể, phục vụ hàng nghìn cán bộ, giảng viên cùng truy vấn trong cao điểm đánh giá cuối kỳ hoặc tính lương. Các yêu cầu tối ưu hóa gồm:
- Giảm thiểu tài nguyên tiêu tốn trên máy chủ (CPU, RAM).
- Giảm thời gian phản hồi API và tải trang web xuống dưới 100ms.
- Tối ưu hóa các truy vấn danh mục tĩnh và cây phòng ban phân cấp.

## 3. Quyết định Kiến trúc
1. **EF Core AsNoTracking**:
   Toàn bộ truy vấn đọc dữ liệu (Read-only queries) trong `CatalogService`, `SearchService`, `ReportService` đều kích hoạt `.AsNoTracking()`, loại bỏ chi phí theo dõi Entity của ChangeTracker.
2. **In-Memory Caching (`IMemoryCache`)**:
   Áp dụng mô hình Cache-Aside cho 18 danh mục nghiệp vụ dùng chung (`KpiCategories`, `MeasurementUnits`, `MeasurementMethods`, `Departments`) với thời gian tồn tại (TTL) 10-15 phút và chủ động xóa cache (Cache Invalidation) khi có hành động thêm/sửa/xóa.
3. **Response Compression**:
   Kích hoạt nén phản hồi Brotli và Gzip ở tầng Middleware cho MIME types: `application/json`, `text/html`, `text/css`, `application/javascript`.
4. **Database Indexing**:
   Thiết lập các Non-clustered Indexes trên các trường khóa ngoại và tiêu chí tìm kiếm thường xuyên (`evaluations`, `payrolls`, `kpi_assignments`, `notifications`, `audit_logs`).
5. **Gắn Header Giám Sát**:
   Middleware `RequestPerformanceMiddleware` đo đạc thời gian thực thi của từng request và gắn header `X-Response-Time-Ms`, đồng thời ghi log cảnh báo khi truy vấn vượt quá ngưỡng 500ms.

## 4. Hệ quả & Lợi ích
- Tốc độ phản hồi API trung bình đạt 2ms - 15ms.
- Dung lượng truyền tải mạng giảm từ 60% đến 80% nhờ nén Brotli.
- Tránh nghẽn I/O cơ sở dữ liệu trong các kỳ cao điểm nộp tự chấm và tính lương.
