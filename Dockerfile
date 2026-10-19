# =========================================================================================
# HỌC VIỆN HÀNH CHÍNH VÀ QUẢN TRỊ CÔNG
# HỆ THỐNG QUẢN LÝ VÀ ĐÁNH GIÁ KPI CÁN BỘ, GIẢNG VIÊN
# DOCKERFILE: MULTI-STAGE PRODUCTION-READY CONTAINER IMAGE (.NET 8 LTS)
# =========================================================================================

# Stage 1: Build & Restore Environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ file định nghĩa dự án .csproj để tối ưu Docker Layer Caching
COPY ["src/KPI.Domain/KPI.Domain.csproj", "KPI.Domain/"]
COPY ["src/KPI.Application/KPI.Application.csproj", "KPI.Application/"]
COPY ["src/KPI.Infrastructure/KPI.Infrastructure.csproj", "KPI.Infrastructure/"]
COPY ["src/KPI.Web/KPI.Web.csproj", "KPI.Web/"]

RUN dotnet restore "KPI.Web/KPI.Web.csproj"

# Copy toàn bộ mã nguồn
COPY src/ .

# Biên dịch dự án Web
WORKDIR "/src/KPI.Web"
RUN dotnet build "KPI.Web.csproj" -c Release -o /app/build

# Stage 2: Publish Artifacts
FROM build AS publish
RUN dotnet publish "KPI.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime Environment (Lightweight & Secure)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Thiết lập biến môi trường chạy ứng dụng
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=vi_VN.UTF-8 \
    LANG=vi_VN.UTF-8

# Cài đặt gói hỗ trợ định dạng Tiền tệ / Tiếng Việt ICU
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    icu-devtools \
    && rm -rf /var/lib/apt/lists/*

# Copy kết quả biên dịch
COPY --from=publish /app/publish .

# Tạo thư mục lưu trữ uploads và logs với quyền truy cập an toàn
RUN mkdir -p /app/wwwroot/uploads/avatars /app/wwwroot/uploads/evidences /app/logs \
    && chown -R app:app /app

# Chuyển sang tài khoản không đặc quyền (Non-root user) theo tiêu chuẩn bảo mật container
USER app

EXPOSE 8080

# Health check định kỳ bên trong container
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "KPI.Web.dll"]
