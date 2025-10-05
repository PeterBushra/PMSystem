using Jobick.Models;
using Jobick.Services.Interfaces;
using Jobick.ViewModels;

namespace Jobick.Services;

/// <summary>
/// Provides KPI calculations for a project. Purely computational; no I/O.
/// </summary>
public class ProjectKpiService : IProjectKpiService
{
    public ProjectKPIs Calculate(Project project)
    {
        var kpis = new ProjectKPIs();
        var tasks = project.Tasks.ToList();
        var today = DateTime.Today;

        // Task status counts
        kpis.TotalTasks = tasks.Count;
        kpis.CompletedTasks = tasks.Count(t => t.DoneRatio >= 1.0m);
        kpis.InProgressTasks = tasks.Count(t => t.DoneRatio is > 0m and < 1.0m);
        kpis.NotStartedTasks = tasks.Count(t => t.DoneRatio == 0m || t.DoneRatio == null);
        kpis.OverdueTasks = tasks.Count(t => t.ExpectedEndDate < today && (t.DoneRatio == null || t.DoneRatio < 1.0m));

        if (kpis.TotalTasks > 0)
        {
            kpis.CompletionPercentage = Math.Round((decimal)kpis.CompletedTasks / kpis.TotalTasks * 100m, 2);
            kpis.AverageTaskCompletion = Math.Round(tasks.Average(t => (t.DoneRatio ?? 0m) * 100m), 2);
        }

        // Department distribution
        kpis.TasksByDepartment = tasks
            .Where(t => !string.IsNullOrWhiteSpace(t.ImplementorDepartment))
            .GroupBy(t => t.ImplementorDepartment!.Trim())
            .ToDictionary(g => g.Key, g => g.Count());

        // Stage counts (raw)
        kpis.StageTaskCounts = tasks
            .Where(t => !string.IsNullOrWhiteSpace(t.StageName))
            .GroupBy(t => t.StageName!.Trim())
            .ToDictionary(g => g.Key, g => g.Count());

        // Per-stage weighted completion
        kpis.StageCompletionByWeight = new Dictionary<string, decimal>();
        var stageGroups = tasks
            .Where(t => !string.IsNullOrWhiteSpace(t.StageName))
            .GroupBy(t => t.StageName!.Trim());

        foreach (var g in stageGroups)
        {
            decimal sumStageWeights = g.Sum(t => t.Weight ?? 0m);
            decimal weightedDone = g.Sum(t => (t.Weight ?? 0m) * (t.DoneRatio ?? 0m));
            decimal relative = (sumStageWeights > 0m) ? (weightedDone / sumStageWeights) : 0m;
            if (relative < 0m) relative = 0m;
            if (relative > 1m) relative = 1m;
            kpis.StageCompletionByWeight[g.Key] = relative; // fraction 0..1
        }

        // Timeline KPIs
        kpis.TotalProjectDays = (project.EndDate - project.StartSate).Days;
        kpis.DaysRemaining = (project.EndDate - today).Days;
        if (kpis.TotalProjectDays > 0)
        {
            var elapsed = kpis.TotalProjectDays - kpis.DaysRemaining;
            kpis.ProjectProgressPercentage = Math.Round((decimal)elapsed / kpis.TotalProjectDays * 100m, 2);
        }

        return kpis;
    }
}
