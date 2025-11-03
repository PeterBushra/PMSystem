using Jobick.Models;

namespace Jobick.Services.Statistics;

internal static class StatisticsHelpers
{
    public static int GetQuarter(DateTime date) => (date.Month - 1) / 3 + 1;

    public static (DateOnly Start, DateOnly End) GetQuarterRange(int year, int quarter)
    {
        var startMonth = (quarter - 1) * 3 + 1;
        var endMonth = quarter * 3;
        var start = new DateOnly(year, startMonth, 1);
        var endDay = DateTime.DaysInMonth(year, endMonth);
        var end = new DateOnly(year, endMonth, endDay);
        return (start, end);
    }

    public static decimal SafeWeight(decimal? weight) => weight.HasValue && weight.Value > 0m ? weight.Value : 0m;

    public static decimal Clamp01(decimal v) => v < 0m ? 0m : (v > 1m ? 1m : v);

    public static decimal NormalizeProgress(decimal v)
    {
        if (v < 0m) return 0m;
        // if > 1, assume it's a percentage (0..100)
        if (v > 1m) return Clamp01(v / 100m);
        return Clamp01(v);
    }

    public static decimal GetTaskProgressFromLogsAt(Jobick.Models.Task task, DateOnly cutoff)
    {
        decimal? val = task.TaskLogs?
            .Where(l => l.Date <= cutoff)
            .OrderByDescending(l => l.Date)
            .Select(l => (decimal?)l.Progress)
            .FirstOrDefault();
        return val.HasValue ? NormalizeProgress(val.Value) : 0m;
    }

    public static decimal SumTaskLogsInRange(Jobick.Models.Task task, DateOnly start, DateOnly end)
    {
        if (task.TaskLogs == null) return 0m;
        var sum = task.TaskLogs
            .Where(l => l.Date >= start && l.Date <= end)
            .Sum(l => NormalizeProgress(l.Progress));
        return Clamp01(sum);
    }

    public static List<Jobick.Models.Task> GetTasksForProject(Project project, Dictionary<int, List<Jobick.Models.Task>> tasksLookup)
    {
        if (project.Tasks != null && project.Tasks.Count > 0)
            return project.Tasks.ToList();

        return tasksLookup.TryGetValue(project.Id, out var tasks) ? tasks : new List<Jobick.Models.Task>();
    }
}
