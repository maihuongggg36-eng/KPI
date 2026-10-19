# ADR 001: Clean Architecture và Tech Stack .NET 8 LTS

## 1. Trạng thái
**ĐÃ PHÊ DUYỆT (ACCEPTED)** — Ngày: 13/10/2026

## 2. Bối cảnh
Hệ thống Quản lý và Đánh giá KPI Cán bộ, Giảng viên Học viện Hành chính và Quản trị công đòi hỏi:
- Khả năng mở rộng cao, phục vụ đồng thời khối cơ quan hành chính công (Nghị định 90/2020/NĐ-CP & Nghị định 48/2023/NĐ-CP) và cơ chế tự chủ tài chính khối đại học (Lương 3P).
- Phân tách rõ ràng giữa quy tắc nghiệp vụ (Business Rules), tầng dữ liệu (Data Access), và tầng giao diện (Presentation).
- Khả năng kiểm thử độc lập (Testability) với mục tiêu kiểm thử tự động $\ge 80\%$.

## 3. Quyết định Kiến trúc
Chúng tôi quyết định áp dụng mô hình **Clean Architecture (Onion Architecture)** trên nền tảng **.NET 8 LTS / C# 12**:
1. **`KPI.Domain`**: Chứa toàn bộ Thực thể (Entities), Giá trị đối tượng (Value Objects), Enums, và Logic cốt lõi độc lập với bất kỳ framework nào.
2. **`KPI.Application`**: Chứa các DTOs, Interface dịch vụ, CQRS/Service contracts, và Business Exceptions. Không phụ thuộc vào tầng lưu trữ dữ liệu hay Web.
3. **`KPI.Infrastructure`**: Thực thi lưu trữ CSDL thông qua EF Core 8 (SQL Server), In-Memory Caching, Background Hosted Services, Email SMTP, và BCrypt Password Hashing.
4. **`KPI.Web`**: Tầng Presentation ASP.NET Core MVC kết hợp SignalR Hubs, OpenAPI Swagger, WCAG 2.1 AA UI, và OWASP Security Headers.
5. **`KPI.Tests`**: Bộ kiểm thử tự động xUnit, FluentAssertions, Moq với hơn 110 tests bao phủ các quy tắc nghiệp vụ.

## 4. Hệ quả & Lợi ích
- **Độc lập cơ sở dữ liệu**: Dễ dàng chuyển đổi hoặc hỗ trợ đồng thời SQL Server, PostgreSQL, In-Memory DB.
- **Dễ bảo trì**: Mọi quy tắc nghiệp vụ (BR-01 đến BR-15) được đóng gói tập trung, không bị phân tán vào View hay Controller.
- **Hiệu năng cao**: Tận dụng triệt để các tối ưu hóa mới nhất của .NET 8 Runtime, Response Compression (Brotli/Gzip), và AsNoTracking.
