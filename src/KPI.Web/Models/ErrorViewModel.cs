namespace KPI.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public int? StatusCode { get; set; }

    public string ErrorTitle => StatusCode switch
    {
        404 => "404 - Không Tìm Thấy Trang",
        403 => "403 - Quyền Truy Cập Bị Từ Chối",
        401 => "401 - Phiên Đăng Nhập Hết Hạn",
        500 => "500 - Lỗi Máy Chủ Nội Bộ",
        _ => "Đã Xảy Ra Lỗi Xử Lý"
    };

    public string ErrorDescription => StatusCode switch
    {
        404 => "Địa chỉ trang web bạn yêu cầu không tồn tại, đã bị di chuyển hoặc đổi tên.",
        403 => "Tài khoản của bạn không có đủ đặc quyền hoặc quyền hạn dữ liệu (Data Scope) để xem tài nguyên này.",
        401 => "Vui lòng đăng nhập lại để tiếp tục thao tác trong hệ thống.",
        500 => "Hệ thống gặp sự cố không mong muốn trong khi xử lý yêu cầu. Sự cố đã được tự động ghi nhận vào hệ thống giám sát.",
        _ => "Đã xảy ra sự cố kỹ thuật trong quá trình thực thi yêu cầu của bạn."
    };
}
