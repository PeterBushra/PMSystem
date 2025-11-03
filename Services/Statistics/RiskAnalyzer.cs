using Jobick.Models;
using Jobick.ViewModels;
using static Jobick.Services.Statistics.StatisticsHelpers;

namespace Jobick.Services.Statistics;

internal static class RiskAnalyzer
{
    public static List<ProjectStatisticsVM.ProjectInfo> BuildOverdueOrAtRiskProjects(
        IEnumerable<Project> projects,
        Dictionary<int, List<Jobick.Models.Task>> tasksLookup,
        DateTime today)
    {
        var overdueOrAtRisk = projects
            .Select(p => new { Project = p, Tasks = GetTasksForProject(p, tasksLookup) })
            .Select(x =>
            {
                var incompleteTasks = x.Tasks.Where(t => (t.DoneRatio ?? 0m) < 1.0m).ToList();
                int incompleteCount = incompleteTasks.Count;

                int remainingRequiredDays = incompleteTasks.Sum(t => t.ManyDaysToComplete);
                int projectDurationDays = Math.Max(0, (x.Project.EndDate.Date - x.Project.StartSate.Date).Days);

                bool isOverdue = today > x.Project.EndDate.Date && incompleteCount > 0;
                bool isAtRisk = !isOverdue && incompleteCount > 0 && remainingRequiredDays > projectDurationDays;

                return new { x.Project, incompleteCount, isOverdue, isAtRisk };
            })
            .Where(x => x.isOverdue || x.isAtRisk)
            .Select(x => new ProjectStatisticsVM.ProjectInfo
            {
                ProjectId = x.Project.Id,
                Name = string.IsNullOrWhiteSpace(x.Project.NameAr) ? x.Project.Name : x.Project.NameAr,
                EndDate = x.Project.EndDate,
                IncompleteTasksCount = x.incompleteCount,
                DelayReasons = x.Project.DelayReasons
            })
            .OrderByDescending(pi => pi.EndDate < today)
            .ThenBy(pi => pi.EndDate)
            .ToList();

        return overdueOrAtRisk;
    }
}
