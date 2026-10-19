# HỆ THỐNG QUẢN LÝ VÀ ĐÁNH GIÁ KPI CÁN BỘ, GIẢNG VIÊN
## HỌC VIỆN HÀNH CHÍNH VÀ QUẢN TRỊ CÔNG (NAPA)

[![.NET 8 LTS](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-007ACC?logo=csharp&logoColor=white)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-8.0.11-512BD4?logo=nuget)](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore)
[![SignalR Real-time](https://img.shields.io/badge/SignalR-Real--time-blue?logo=signalr)](https://learn.microsoft.com/aspnet/core/signalr)
[![Docker Ready](https://img.shields.io/badge/Docker-Compose%20Ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![Test Coverage](https://img.shields.io/badge/Unit%20%26%20Integration%20Tests-113%20Passed%20(100%25)-success?logo=xunit)](file:///d:/KPI/src/KPI.Tests)
[![WCAG 2.1 AA](https://img.shields.io/badge/Accessibility-WCAG%202.1%20AA-brightgreen)](https://www.w3.org/WAI/WCAG21/quickref/)

---

## 🌟 TỔNG QUAN HỆ THỐNG

**Hệ thống Quản lý và Đánh giá KPI Cán bộ, Giảng viên** là nền tảng số hóa quản trị hiệu suất toàn diện được thiết kế và phát triển chuyên biệt cho **Học viện Hành chính và Quản trị công**, kết hợp hài hòa hai cơ chế quản trị:
1. **Khung khổ Pháp lý Hành chính Nhà nước**: Tuân thủ nghiêm ngặt **Nghị định số 90/2020/NĐ-CP** và **Nghị định số 48/2023/NĐ-CP** của Chính phủ về đánh giá, xếp loại chất lượng cán bộ, công chức, viên chức (Khống chế trần 20% Xuất sắc, xuất biểu mẫu Quốc gia Mẫu 01/02 A4).
2. **Quản trị Hiệu quả Công việc Hiện đại (Doanh nghiệp & Đại học Tự chủ)**: Áp dụng thẻ điểm cân bằng **BSC** (Tài chính, Khách hàng, Quy trình nội bộ, Học hỏi & Phát triển), liên thông trực tiếp điểm đánh giá KPI sang quỹ thu nhập tăng thêm (**Lương 3P: P1 - Vị trí, P2 - Năng lực, P3 - Hiệu suất**), hỗ trợ lập phiếu chi tiền mặt chuẩn **Mẫu 02-TT** (Thông tư 133/2016/TT-BTC) và bảng kê ký nhận **Mẫu 05a**.

---

## 🚀 HƯỚNG DẪN KHỞI CHẠY HỆ THỐNG

### Cách 1: Khởi chạy trực tiếp bằng .NET CLI (Khuyến nghị cho Development)

**Yêu cầu môi trường:**
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB hoặc SQL Server 2019/2022)

```bash
# 1. Di chuyển vào thư mục mã nguồn
cd d:\KPI\src

# 2. Khôi phục packages và kiểm tra bản build
dotnet build KPI.sln

# 3. Khởi chạy ứng dụng Web trên cổng 5000
dotnet run --project KPI.Web/KPI.Web.csproj --urls "http://localhost:5000"
```

Mở trình duyệt web truy cập:
👉 **[http://localhost:5000](http://localhost:5000)**

---

### Cách 2: Khởi chạy bằng Docker Compose (Khuyến nghị cho Staging / Production)

Hệ thống đã được đóng gói trọn vẹn gồm 3 dịch vụ: Web App (.NET 8), SQL Server 2022 và Redis 7:

```bash
cd d:\KPI

# Khởi động toàn bộ cụm container
docker compose up -d --build
```
- **Web App**: `http://localhost:5000`
- **Swagger API**: `http://localhost:5000/swagger`
- **Health Check Dashboard**: `http://localhost:5000/Health`

---

## 👥 TÀI KHOẢN THỬ NGHIỆM 6 VAI TRÒ (1-CLICK SWITCH)

Tại trang đăng nhập **`/Auth/Login`**, hệ thống tích hợp sẵn các nút chuyển đổi vai trò 1-Click để kiểm thử nhanh chóng (Mật khẩu mặc định: `123456`):

| STT | Tài khoản | Họ và Tên | Vai trò (Role) | Thẩm quyền & Phạm vi Dữ liệu |
|:---:|:---|:---|:---|:---|
| 1 | `director` | **Trần Lãnh Đạo** | `DIRECTOR` | Giám đốc Học viện: Phê duyệt kết quả toàn viện, kiểm soát trần Nghị định 90, ký cam kết. |
| 2 | `manager` | **Lê Trưởng Phòng** | `MANAGER` | Trưởng khoa / Phòng: Giao chỉ tiêu KPI, chấm thẩm định điểm cấp đơn vị, giải trình chênh lệch. |
| 3 | `officer1` | **Nguyễn Chuyên Viên** | `OFFICER` | Giảng viên / Chuyên viên: Nhập kết quả tự chấm, đính kèm minh chứng, xem phiếu lương P3. |
| 4 | `hr_admin` | **Vũ Thị Nhân Sự** | `HR_ADMIN` | Ban Tổ chức Cán bộ: Theo dõi nhân sự, ngạch bậc, giám sát đồng hồ trần 20% Xuất sắc (BR-06). |
| 5 | `accountant` | **Hoàng Kế Toán** | `ACCOUNTANT` | Phòng Kế hoạch Tài chính: Tính lương P3 theo KPI, lập phiếu chi Mẫu 02-TT, ký nhận quỹ tiền mặt. |
| 6 | `admin` | **Quản trị viên Hệ thống** | `ADMIN` | Quản trị viên: Quản trị 18 danh mục, cấu hình hệ thống, theo dõi Audit Log ISO 27001. |

---

## 🏗️ KIẾN TRÚC MÃ NGUỒN (CLEAN ARCHITECTURE)

Dự án áp dụng chặt chẽ mô hình kiến trúc sạch 5 tầng phân tách trách nhiệm:

```
d:\KPI\src\
├── KPI.Domain/                 # [Tầng 1 - Cốt lõi Nghiệp vụ]
│   ├── Common/                 # BaseEntity, Auditable, ISoftDeletable
│   ├── Entities/               # 40 Thực thể CSDL (Identity, Catalogs, Planning, Evaluation, Payroll)
│   └── Enums/                  # Định nghĩa Enum (PeriodStatus, ScoringFormula, NotificationType...)
│
├── KPI.Application/            # [Tầng 2 - Logic Ứng dụng]
│   ├── Common/                 # Exceptions (BusinessRuleException), Security, Interfaces
│   ├── DTOs/                   # Data Transfer Objects (Kpi, Evaluation, Payroll, Notification)
│   └── Services/               # Interface dịch vụ (IKpiCalculation, IDecree90, INotification...)
│
├── KPI.Infrastructure/         # [Tầng 3 - Hạ tầng & Dữ liệu]
│   ├── Data/                   # KpiDbContext (EF Core 8), Entity Configurations, Seed Data
│   ├── Security/               # BCryptPasswordHasher, DataScopeHandler
│   └── Services/               # Triển khai dịch vụ (EmailQueueBackgroundService, CatalogService...)
│
├── KPI.Web/                    # [Tầng 4 - Giao diện & Trình bày]
│   ├── Controllers/            # MVC Controllers (KpiPlanning, Evaluation, Payroll, Health, Seo...)
│   ├── Hubs/                   # SignalR NotificationHub (/hubs/notification)
│   ├── Health/                 # Custom HealthChecks (Database, Memory, Disk Space)
│   ├── Middleware/             # Exception Handling, Security Headers, Request Performance
│   ├── Views/                  # Razor Views (Theme Dark/Light, WCAG 2.1 AA)
│   └── wwwroot/                # JavaScript (SignalR, Command Palette Ctrl+K, Audio API) & CSS
│
└── KPI.Tests/                  # [Tầng 5 - Kiểm thử Tự động]
    ├── Integration/            # Health and SEO Endpoints Integration Tests
    ├── Security/               # AuthLockout, DataScope Authorization Tests
    └── Services/               # Unit Tests (KpiCalculation, Decree90, Payroll, Evaluation...)
```

---

## 📋 HỆ THỐNG QUY TẮC NGHIỆP VỤ (BUSINESS RULES)

| Mã BR | Tên quy tắc nghiệp vụ | Mô tả chi tiết thực thi trong mã nguồn |
|:---|:---|:---|
| **BR-01** | Tổng trọng số chỉ tiêu KPI | Tổng trọng số của các mục tiêu trong phiếu cam kết bắt buộc phải bằng chính xác **100%**. |
| **BR-02** | Khung trọng số từng chỉ tiêu | Trọng số mỗi chỉ tiêu nằm trong khoảng **5% $\le$ Weight $\le$ 50%**. |
| **BR-03** | Khóa phiếu sau khi ký cam kết | Khi cả Cán bộ và Lãnh đạo ký cam kết số, bản giao KPI chuyển trạng thái `Approved` và khóa chỉnh sửa. |
| **BR-04** | Thời hạn tự đánh giá | Cán bộ chỉ được nộp tự đánh giá trong thời gian kỳ mở và trước `SelfDeadline`. |
| **BR-05** | Giải trình chênh lệch điểm số | Nếu điểm Quản lý thẩm định lệch $\ge 15$ điểm so với Tự chấm, bắt buộc nhập giải trình $\ge 50$ ký tự. |
| **BR-06** | Trần 20% Hoàn thành xuất sắc | Tỷ lệ Hoàn thành xuất sắc nhiệm vụ không vượt quá **20%** tổng số cán bộ hoàn thành tốt theo NĐ 90/48. |
| **BR-07** | Điểm liệt & Kỷ luật công chức | Cán bộ bị kỷ luật hoặc có chỉ tiêu điểm liệt không được xếp loại Tốt hoặc Xuất sắc. |
| **BR-08** | Thẩm quyền phê duyệt xếp loại | Ban Giám đốc và Hội đồng thi đua có thẩm quyền ra quyết định phân loại cán bộ công chức cuối cùng. |
| **BR-09** | Cảnh báo vượt chỉ tiêu $\ge 120\%$ | Khi tỷ lệ hoàn thành $\ge 120\%$, bật cảnh báo vàng và bắt buộc tải lên tệp minh chứng đính kèm. |
| **BR-10** | Chặn trần điểm tối đa (MaxCap) | Tỷ lệ hoàn thành tính điểm bị chặn trần tối đa ở mức **150%** (ngăn chặn méo mó điểm số). |
| **BR-11** | Công thức tính lương 3P (P3) | `Lương_P3 = Lương_Cơ_Bản × Hệ_Số_Lương × Hệ_Số_KPI` (Hệ số KPI: 1.3, 1.15, 1.0, 0.7). |
| **BR-12** | Dịch số thành chữ tiếng Việt | Thuật toán chuyển đổi số tiền thành chữ tiếng Việt chuẩn kế toán (hàng tỷ, triệu, nghìn, đồng chẵn/lẻ). |
| **BR-13** | Quy chuẩn Mẫu 02-TT | Phiếu chi tiền mặt xuất bản theo chuẩn Thông tư 133/2016/TT-BTC của Bộ Tài chính. |
| **BR-14** | Khiếu nại đánh giá (Dispute) | Cho phép cán bộ khiếu nại điểm trong vòng 7 ngày kể từ khi thẩm định, kích hoạt workflow tái xét. |
| **BR-15** | Phân phối chuẩn Gauss | Kiểm soát phân bổ điểm số toàn viện tiệm cận đường cong chuông chuẩn Gauss (Bell Curve). |

---

## 🧪 KIỂM THỬ TỰ ĐỘNG (AUTOMATED TESTING)

Toàn bộ **113 tests** đều pass 100% với thời gian chạy trung bình 7 giây:

```bash
dotnet test d:\KPI\src\KPI.Tests\KPI.Tests.csproj
```

**Kết quả kiểm thử:**
```text
Passed!  - Failed: 0, Passed: 113, Skipped: 0, Total: 113, Duration: 7 s - KPI.Tests.dll (net8.0)
```

---

## 📊 TỐI ƯU HÓA HIỆU NĂNG & SẢN XUẤT (PRODUCTION-READY)

1. **Response Compression**: Kích hoạt Brotli và Gzip nén dữ liệu mạng (tiết kiệm băng thông đến 80%).
2. **EF Core AsNoTracking & Memory Cache**: Loại bỏ ChangeTracker overhead cho các truy vấn Read-only; Caching 18 danh mục nghiệp vụ dùng chung.
3. **Database Indexing**: Kịch bản [optimize_indexes.sql](file:///d:/KPI/database/optimize_indexes.sql) tối ưu 10 chỉ mục Non-clustered.
4. **Giám sát Sức khỏe (/Health)**: Đo lường thời gian thực tình trạng CSDL SQL Server, bộ nhớ RAM (GC Heap), dung lượng ổ đĩa.
5. **SEO & Accessibility**: Đầy đủ Meta Tags, Open Graph, Twitter Cards, Schema JSON-LD, `sitemap.xml`, `robots.txt`, đạt chuẩn WCAG 2.1 AA.
6. **Command Palette (`Ctrl+K`)**: Tra cứu tức thì đa chiều cán bộ, KPI, danh mục, trang chức năng.

---

## 📜 BẢN QUYỀN & PHÁT TRIỂN

- **Đơn vị thụ hưởng:** Học viện Hành chính và Quản trị công
- **Phiên bản:** 3.0 Enterprise Release (Hoàn thành trọn vẹn 12/12 phiên phát triển)
- **Năm phát hành:** 2026
