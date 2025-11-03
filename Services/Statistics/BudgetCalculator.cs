using Jobick.Models;

namespace Jobick.Services.Statistics;

internal static class BudgetCalculator
{
    public static Dictionary<int, int> ComputeProjectsCountByYear(IEnumerable<Project> projects)
        => projects.GroupBy(p => p.EndDate.Year).ToDictionary(g => g.Key, g => g.Count());

    public static (Dictionary<int, decimal> Budgets, Dictionary<int, string> Names) ComputeBudgetsAndNames(
        IEnumerable<Project> projects,
        Dictionary<int, List<Jobick.Models.Task>> tasksLookup)
    {
        var budgetsExceptFullyDone = new Dictionary<int, decimal>();
        var projectNames = new Dictionary<int, string>();

        foreach (var p in projects)
        {
            var tasks = StatisticsHelpers.GetTasksForProject(p, tasksLookup);
            bool fullyDone = tasks.Count > 0 && tasks.All(t => (t.DoneRatio ?? 0m) >= 1.0m);
            if (fullyDone) continue;

            decimal budget = p.TotalCost ?? tasks.Sum(t => t.Cost ?? 0m);
            if (budget < 0) budget = 0;
            budgetsExceptFullyDone[p.Id] = budget;
            projectNames[p.Id] = string.IsNullOrWhiteSpace(p.NameAr) ? p.Name : p.NameAr;
        }

        return (budgetsExceptFullyDone, projectNames);
    }

    public static Dictionary<int, decimal> ComputeBudgetsByYear(IEnumerable<Project> projects)
        => projects.GroupBy(p => p.EndDate.Year).ToDictionary(g => g.Key, g => g.Sum(p => p.TotalCost ?? 0m));
}
