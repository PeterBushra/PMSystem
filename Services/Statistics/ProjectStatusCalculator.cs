using Jobick.Models;
using static Jobick.Services.Statistics.StatisticsHelpers;

namespace Jobick.Services.Statistics;

internal static class ProjectStatusCalculator
{
    public static (int InProgress, int NotStarted, int Done) Compute(
        IEnumerable<Project> projects,
        Dictionary<int, List<Jobick.Models.Task>> tasksLookup)
    {
        int inProgress = 0, notStarted = 0, done = 0;

        foreach (var p in projects)
        {
            var tasks = GetTasksForProject(p, tasksLookup);
            if (tasks.Count == 0)
                continue;

            bool allDone = tasks.All(t => (t.DoneRatio ?? 0m) >= 1.0m);
            bool noneStarted = tasks.All(t => (t.DoneRatio ?? 0m) == 0m);

            if (allDone) done++;
            else if (noneStarted) notStarted++;
            else inProgress++;
        }

        return (inProgress, notStarted, done);
    }
}
