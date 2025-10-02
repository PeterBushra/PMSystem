using System;
using System.Collections.Generic;

namespace Jobick.Models;

public partial class Task
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string? StageName { get; set; }

    public string StageNameAr { get; set; } = null!;

    public string? Task1 { get; set; }

    public string TaskAr { get; set; } = null!;

    public string ImplementorDepartment { get; set; } = null!;

    public string? DepartmentResponsible { get; set; }

    public string? DefinationOfDone { get; set; }

    public int ManyDaysToComplete { get; set; }

    public DateTime ExpectedStartDate { get; set; }

    public DateTime ExpectedEndDate { get; set; }

    public DateTime? ActualEndDate { get; set; }

    public decimal? DoneRatio { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Project Project { get; set; } = null!;
}
