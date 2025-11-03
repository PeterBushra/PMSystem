using Jobick.Models;
using Jobick.Services.Interfaces;
using Jobick.ViewModels;
using Jobick.Services.Statistics;

namespace Jobick.Services;

/// <summary>
/// Aggregates dashboard-level KPIs across projects and tasks.
/// </summary>
public class StatisticsService : IStatisticsService
{
    public ProjectStatisticsVM CalculateDashboard(IEnumerable<Project> projects, IEnumerable<Jobick.Models.Task> allTasks, DateTime? now = null)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(allTasks);

        now ??= DateTime.Today;
        var today = now.Value.Date;

        var tasksLookup = allTasks.GroupBy(t => t.ProjectId).ToDictionary(g => g.Key, g => g.ToList());

        var (inProgress, notStarted, done) = ProjectStatusCalculator.Compute(projects, tasksLookup);
        var projectsCountByYear = BudgetCalculator.ComputeProjectsCountByYear(projects);
        var (budgetsExceptFullyDone, projectNames) = BudgetCalculator.ComputeBudgetsAndNames(projects, tasksLookup);
        var budgetsByYear = BudgetCalculator.ComputeBudgetsByYear(projects);
        var overdueOrAtRisk = RiskAnalyzer.BuildOverdueOrAtRiskProjects(projects, tasksLookup, today);
        var progress = ProgressCalculator.Compute(projects, allTasks);

        return new ProjectStatisticsVM
        {
            InProgressProjects = inProgress,
            NotStartedProjects = notStarted,
            DoneProjects = done,
            ProjectsCountByYear = projectsCountByYear,
            AllProjectsBudgetsExceptFullyDone = budgetsExceptFullyDone,
            ProjectNames = projectNames,
            ProjectsBudgetsByYear = budgetsByYear,
            OverdueProjectsWithIncompleteTasks = overdueOrAtRisk,
            TargetedProgressByYear = progress.TargetedByYear,
            ActualProgressByYear = progress.ActualByYear,
            TargetedProgressByQuarter = progress.TargetedByQuarter,
            ActualProgressByQuarter = progress.ActualByQuarter,
            ProjectProgressDetails = progress.ProjectDetails
        };
    }
}
