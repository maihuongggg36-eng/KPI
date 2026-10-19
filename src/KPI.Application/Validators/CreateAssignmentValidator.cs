using FluentValidation;
using KPI.Application.DTOs;

namespace KPI.Application.Validators;

public class CreateAssignmentValidator : AbstractValidator<CreateAssignmentRequest>
{
    public CreateAssignmentValidator()
    {
        RuleFor(x => x.PeriodId)
            .GreaterThan(0)
            .WithMessage("Vui lòng chọn kỳ đánh giá hợp lệ.");

        RuleFor(x => x.EmployeeId)
            .GreaterThan(0)
            .WithMessage("Vui lòng chọn nhân viên được giao chỉ tiêu.");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Bản giao KPI phải chứa ít nhất 01 chỉ tiêu.");

        When(x => !x.IsDraft, () =>
        {
            // BR-01: Tổng trọng số các chỉ tiêu trong bản giao bắt buộc bằng 100.00%
            RuleFor(x => x.Items)
                .Must(items => items != null && items.Sum(i => i.Weight) == 100.00m)
                .WithMessage(x => $"[BR-01] Tổng trọng số các chỉ tiêu phải bằng chính xác 100.00%. Hiện tại đang là: {x.Items?.Sum(i => i.Weight) ?? 0}%.");

            // BR-02: Trọng số của từng chỉ tiêu đơn lẻ phải nằm trong khoảng [5%, 50%]
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.Weight)
                    .InclusiveBetween(5.00m, 50.00m)
                    .WithMessage(i => $"[BR-02] Trọng số của chỉ tiêu '{i.KpiName}' ({i.Weight}%) không hợp lệ. Quy định phải từ 5% đến 50%.");

                item.RuleFor(i => i.TargetValue)
                    .GreaterThan(0)
                    .WithMessage(i => $"Mục tiêu (Target) của chỉ tiêu '{i.KpiName}' phải lớn hơn 0.");
            });
        });
    }
}
