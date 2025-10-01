namespace Jobick.ViewModels;

public class ProjectDetailsVM
{
    // Project Information
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

    // Tasks
    public List<Models.Task> Tasks { get; set; } = new List<Models.Task>();

    // KPIs
    public ProjectKPIs KPIs { get; set; } = new ProjectKPIs();
}

public class ProjectKPIs
{
    // Task Status KPIs
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int NotStartedTasks { get; set; }
    public int OverdueTasks { get; set; }

    // Completion Percentages
    public decimal CompletionPercentage { get; set; }
    public decimal AverageTaskCompletion { get; set; }

    // Department Distribution
    public Dictionary<string, int> TasksByDepartment { get; set; } = new Dictionary<string, int>();

    // Stage Distribution
    public Dictionary<string, int> TasksByStage { get; set; } = new Dictionary<string, int>();

    // Time-based KPIs
    public int DaysRemaining { get; set; }
    public int TotalProjectDays { get; set; }
    public decimal ProjectProgressPercentage { get; set; }

    // Helper Methods for Chart Data
    public string GetTaskStatusChartData()
    {
        return $"{CompletedTasks}/{TotalTasks}";
    }

    public string GetProjectProgressChartData()
    {
        var daysElapsed = TotalProjectDays - DaysRemaining;
        return $"{daysElapsed}/{TotalProjectDays}";
    }
}