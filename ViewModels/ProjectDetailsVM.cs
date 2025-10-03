namespace Jobick.ViewModels;

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

    // Each value = (Σ (w_i * doneRatio_i) / Σ w_i) for that stage (DoneRatio fraction 0..1)
    public Dictionary<string, decimal> StageCompletionByWeight { get; set; } = new();

    public int DaysRemaining { get; set; }
    public int TotalProjectDays { get; set; }
    public decimal ProjectProgressPercentage { get; set; }

    public string GetTaskStatusChartData() => $"{CompletedTasks}/{TotalTasks}";

    public string GetProjectProgressChartData()
    {
        var elapsed = TotalProjectDays - DaysRemaining;
        return $"{elapsed}/{TotalProjectDays}";
    }
}