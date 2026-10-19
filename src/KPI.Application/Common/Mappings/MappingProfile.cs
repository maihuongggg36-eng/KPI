using AutoMapper;
using KPI.Application.DTOs;
using KPI.Domain.Entities.Catalogs;
using KPI.Domain.Entities.Evaluation;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;
using KPI.Domain.Entities.Planning;

namespace KPI.Application.Common.Mappings;

/// <summary>
/// AutoMapper Profile ánh xạ Entity sang DTO và ngược lại
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.Role, opt => opt.MapFrom(s => s.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault() ?? string.Empty));

        CreateMap<Department, DepartmentDto>();

        CreateMap<KpiMasterItem, KpiMasterItemDto>()
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category.Name))
            .ForMember(d => d.UnitName, opt => opt.MapFrom(s => s.Unit.Name))
            .ForMember(d => d.ScoringFormula, opt => opt.MapFrom(s => s.ScoringFormula.ToString()));

        CreateMap<KpiAssignment, KpiAssignmentDto>()
            .ForMember(d => d.PeriodName, opt => opt.MapFrom(s => s.Period.Name))
            .ForMember(d => d.EmployeeName, opt => opt.MapFrom(s => s.Employee.FullName))
            .ForMember(d => d.Details, opt => opt.MapFrom(s => s.AssignmentDetails));

        CreateMap<KpiAssignmentDetail, KpiAssignmentDetailDto>();

        CreateMap<Evaluation, EvaluationDto>()
            .ForMember(d => d.EmployeeName, opt => opt.MapFrom(s => s.Employee.FullName));
    }
}
