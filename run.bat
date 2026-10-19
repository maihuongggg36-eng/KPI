@echo off
chcp 65001 > nul
cls
echo =======================================================================
echo   HỆ THỐNG QUẢN LÝ KPI HỌC VIỆN HÀNH CHÍNH & QUẢN TRỊ CÔNG (.NET 8)
echo   Phiên bản: Clean Architecture Enterprise - SQL Server
echo =======================================================================
echo.
echo [1] Đang mở trình duyệt tới địa chỉ: http://localhost:5272 ...
echo [2] Đang khởi chạy máy chủ ASP.NET Core trên cổng 5272...
echo.
echo Danh sách tài khoản thử nghiệm:
echo   - Admin (Toàn quyền):       admin       / 123456
echo   - Giám đốc Học viện:        director    / 123456
echo   - Trưởng khoa / Trưởng phòng: manager    / 123456
echo   - Kế toán:                  accountant  / 123456
echo   - Cán bộ / Nhân viên:       officer1    / 123456
echo.
echo Nhấn tổ hợp phím [Ctrl + C] để dừng máy chủ web khi cần.
echo =======================================================================
echo.

start "" "http://localhost:5272/Auth/Login"
dotnet run --project "%~dp0src\KPI.Web\KPI.Web.csproj" --urls "http://localhost:5272"
pause

