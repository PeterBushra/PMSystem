namespace Jobick.ViewModels;

public class TaskImportPreviewVM
{
    public int ProjectId { get; set; }
    public List<TaskImportRow> Rows { get; set; } = new();
}

public class TaskImportConfirmRequest
{
    public int ProjectId { get; set; }
    public List<TaskImportConfirmRow> Rows { get; set; } = new();
}

public class TaskImportConfirmRow
{
    public string? StageName { get; set; }
    public string? TaskName { get; set; }
    public string? ImplementorDepartment { get; set; }
    public string? DepartmentResponsible { get; set; }
    public string? DefinitionOfDone { get; set; }
    public int? ManyDaysToComplete { get; set; }
    public DateTime? ExpectedStartDate { get; set; }
    public DateTime? ExpectedEndDate { get; set; }
    public decimal? DoneRatio { get; set; } // percent 0..100
    public decimal? PlannedPercent { get; set; } // percent 0..100
    public decimal? PlannedCost { get; set; }
}
