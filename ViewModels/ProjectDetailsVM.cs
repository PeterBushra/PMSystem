namespace Jobick.ViewModels;

/// <summary>
/// View model for project details page including raw project data and derived KPIs.
/// </summary>
public class ProjectDetailsVM
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public string? ResponsibleForImplementing { get; set; }
    public string? SystemOwner { get; set; }
    public string? ProjectGoal { get; set; }
    public DateTime StartSate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal? TotalCost { get; set; }

    public List<Models.Task> Tasks { get; set; } = new();
    public ProjectKPIs KPIs { get; set; } = new();
}

/// <summary>
/// Encapsulates computed metrics for a single project.
/// Unless stated otherwise, percentages are 0..100 and task DoneRatio is stored as 0..1.
/// </summary>
public class ProjectKPIs
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int NotStartedTasks { get; set; }
    public int OverdueTasks { get; set; }

    public decimal CompletionPercentage { get; set; }
    public decimal AverageTaskCompletion { get; set; }

    public Dictionary<string, int> TasksByDepartment { get; set; } = new();
    public Dictionary<string, int> StageTaskCounts { get; set; } = new();

    /// <summary>
    /// Each value = Σ (w_i * done_i) / Σ w_i where w_i is task weight and done_i is DoneRatio (0..1).
    /// Values are fractions in [0,1] per stage.
    /// </summary>
    public Dictionary<string, decimal> StageCompletionByWeight { get; set; } = new();

    public int DaysRemaining { get; set; }
    public int TotalProjectDays { get; set; }
    public decimal ProjectProgressPercentage { get; set; }

    /// <summary>
    /// Returns a simple "completed/total" string for charting.
    /// </summary>
    public string GetTaskStatusChartData() => $"{CompletedTasks}/{TotalTasks}";

    /// <summary>
    /// Returns a simple "elapsed/total" string for charting.
    /// </summary>
    public string GetProjectProgressChartData()
    {
        var elapsed = TotalProjectDays - DaysRemaining;
        return $"{elapsed}/{TotalProjectDays}";
    }
}