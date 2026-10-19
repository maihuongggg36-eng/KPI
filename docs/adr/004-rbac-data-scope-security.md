# ADR 004: Phân Quyền RBAC và Phạm Vi Dữ Liệu (Data Scope Authorization)

## 1. Trạng thái
**ĐÃ PHÊ DUYỆT (ACCEPTED)** — Ngày: 13/10/2026

## 2. Bối cảnh
Trong môi trường giáo dục đại học và cơ quan hành chính nhà nước:
- Cán bộ thuộc khoa/phòng ban nào chỉ được xem và tự chấm điểm cho bản thân.
- Trưởng phòng/Trưởng khoa (Manager) chỉ được thẩm định điểm của cán bộ trong cùng đơn vị (`ScopeDepartment`).
- Ban Giám Đốc (Director) và Phòng Tổ chức Cán bộ (HR Admin) có quyền hạn toàn cơ quan (`ScopeOrganization`).
- Cần cơ chế bảo vệ phân quyền chặt chẽ ngay tại tầng API và tầng truy vấn dữ liệu (Row-level Security / Filter).

## 3. Quyết định Kiến trúc
1. **Mô hình RBAC kết hợp Data Scope**:
   - 6 vai trò chuẩn: `DIRECTOR`, `MANAGER`, `OFFICER`, `HR_ADMIN`, `ACCOUNTANT`, `ADMIN`.
   - 2 cấp Data Scope: `DataScope.Department` và `DataScope.Organization`.
2. **Authorization Handler**:
   Tạo `DataScopeHandler` kế thừa `AuthorizationHandler<DataScopeRequirement>` xác thực ngữ cảnh người dùng đang truy cập.
3. **Chống Brute Force**:
   Tích hợp ASP.NET Core Built-in RateLimiter với chính sách `loginLimiter`: Giới hạn 15 lần thử/phút cho mỗi IP client.
4. **Mật khẩu an toàn**:
   Mã hóa mật khẩu bằng thuật toán chuẩn công nghiệp `BCrypt.Net` với work factor cao, phòng chống tấn công bảng Rainbow Table.
5. **OWASP Security Headers**:
   Middleware `SecurityHeadersMiddleware` áp dụng đầy đủ: `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `X-XSS-Protection`, `Referrer-Policy: strict-origin-when-cross-origin`.

## 4. Hệ quả & Lợi ích
- Đảm bảo tính bảo mật và toàn vẹn dữ liệu cá nhân theo quy định của pháp luật.
- Ngăn chặn triệt để lỗ hổng IDOR (Insecure Direct Object References).
- Đạt tiêu chuẩn kiểm toán hệ thống thông tin ISO 27001.
