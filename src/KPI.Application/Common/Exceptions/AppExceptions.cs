namespace KPI.Application.Common.Exceptions;

/// <summary>
/// Ngoại lệ vi phạm quy tắc nghiệp vụ (Business Rules: BR-01 -> BR-20)
/// </summary>
public class BusinessRuleException : Exception
{
    public string RuleCode { get; }

    public BusinessRuleException(string ruleCode, string message) : base(message)
    {
        RuleCode = ruleCode;
    }
}

/// <summary>
/// Ngoại lệ không tìm thấy bản ghi
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"Không tìm thấy bản ghi '{name}' với khóa định danh ({key}).")
    {
    }
}

/// <summary>
/// Ngoại lệ kiểm thực dữ liệu đầu vào (FluentValidation)
/// </summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("Đã xảy ra một hoặc nhiều lỗi kiểm tra tính hợp lệ của dữ liệu.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }
}
