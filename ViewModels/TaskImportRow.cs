using System.ComponentModel.DataAnnotations;

namespace Jobick.ViewModels;

public class TaskImportRow
{
    // Raw values from Excel (for reference)
    public string? StageName { get; set; }
    public string? TaskName { get; set; }
    public string? ImplementorDepartment { get; set; }
    public string? DepartmentResponsible { get; set; }
    public string? DefinitionOfDone { get; set; }
    public string? ManyDaysToCompleteRaw { get; set; }
    public string? ExpectedStartDateRaw { get; set; }
    public string? ExpectedEndDateRaw { get; set; }
    public string? DoneRatioRaw { get; set; }
    public string? PlannedPercentRaw { get; set; }
    public string? PlannedCostRaw { get; set; }

    // Parsed values
    public int? ManyDaysToComplete { get; set; }
    public DateTime? ExpectedStartDate { get; set; }
    public DateTime? ExpectedEndDate { get; set; }
    public decimal? DoneRatio { get; set; } // percentage 0..100
    public decimal? PlannedPercent { get; set; } // percentage 0..100 used as Weight
    public decimal? PlannedCost { get; set; }

    public List<string> Errors { get; set; } = new();
}
