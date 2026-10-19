using KPI.Domain.Common;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Enums;

namespace KPI.Domain.Entities.Organization;

/// <summary>
/// 9. Cơ quan, Doanh nghiệp (organizations)
/// </summary>
public class Organization : BaseEntity, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public OrgType OrgType { get; set; } = OrgType.PublicAdministration;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
}

/// <summary>
/// 10. Phòng ban, Đơn vị trực thuộc (departments)
/// </summary>
public class Department : BaseEntity, ISoftDeletable
{
    public long OrgId { get; set; }
    public virtual Organization Organization { get; set; } = null!;

    public long? ParentId { get; set; }
    public virtual Department? Parent { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TreePath { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual ICollection<Department> Children { get; set; } = new List<Department>();
    public virtual ICollection<EmployeeProfile> Employees { get; set; } = new List<EmployeeProfile>();
}

/// <summary>
/// 11. Bảng bao đóng phân cấp phòng ban (department_hierarchy - Closure Table)
/// </summary>
public class DepartmentHierarchy
{
    public long AncestorId { get; set; }
    public virtual Department Ancestor { get; set; } = null!;

    public long DescendantId { get; set; }
    public virtual Department Descendant { get; set; } = null!;

    public int Depth { get; set; }
}

/// <summary>
/// 12. Chức danh vị trí việc làm (positions)
/// </summary>
public class Position : BaseEntity, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? RankLevel { get; set; }
    public bool IsLeadership { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual ICollection<EmployeeProfile> Employees { get; set; } = new List<EmployeeProfile>();
}

/// <summary>
/// 13. Hồ sơ nhân viên, cán bộ (employee_profiles)
/// </summary>
public class EmployeeProfile : BaseEntity, ISoftDeletable
{
    public long UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public long DepartmentId { get; set; }
    public virtual Department Department { get; set; } = null!;

    public long PositionId { get; set; }
    public virtual Position Position { get; set; } = null!;

    public string StaffCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime JoinDate { get; set; }
    public bool IsConcurrent { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual CivilServantProfile? CivilServantProfile { get; set; }
}

/// <summary>
/// 14. Danh mục ngạch bậc công chức Bộ Nội Vụ (position_ranks)
/// </summary>
public class PositionRank
{
    public string RankCode { get; set; } = string.Empty;
    public string RankName { get; set; } = string.Empty;
    public string RankGroup { get; set; } = string.Empty;
    public int MaxGrade { get; set; } = 9;

    // Navigation properties
    public virtual ICollection<CivilServantProfile> CivilServants { get; set; } = new List<CivilServantProfile>();
}

/// <summary>
/// 15. Hồ sơ cán bộ công chức chuyên biệt (civil_servant_profiles - Kế thừa từ employee_profiles)
/// </summary>
public class CivilServantProfile : BaseEntity
{
    public long EmployeeId { get; set; }
    public virtual EmployeeProfile Employee { get; set; } = null!;

    public string CivilServantCode { get; set; } = string.Empty;
    public string OfficialRankCode { get; set; } = string.Empty;
    public virtual PositionRank OfficialRank { get; set; } = null!;

    public int SalaryGrade { get; set; } = 1;
    public decimal SalaryCoefficient { get; set; } = 2.34m;
    public DateTime? AppointmentDate { get; set; }
    public PartyStatus PartyStatus { get; set; } = PartyStatus.None;
    public PoliticalTheory PoliticalTheory { get; set; } = PoliticalTheory.None;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
