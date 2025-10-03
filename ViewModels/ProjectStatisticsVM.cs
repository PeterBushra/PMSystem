using System;
using System.Collections.Generic;

namespace Jobick.ViewModels
{
    public class ProjectStatisticsVM
    {
        // First KPI
        public int InProgressProjects { get; set; }
        public int NotStartedProjects { get; set; }
        public int DoneProjects { get; set; }

        // Second KPI
        public Dictionary<int, int> ProjectsCountByYear { get; set; } = new();

        // Third KPI (Budgets of not fully done projects)
        // ProjectId -> Budget value
        public Dictionary<int, decimal> AllProjectsBudgetsExceptFullyDone { get; set; } = new();
        // ProjectId -> Project Name (for labels)
        public Dictionary<int, string> ProjectNames { get; set; } = new();

        // Fourth KPI
        public Dictionary<int, decimal> ProjectsBudgetsByYear { get; set; } = new();

        // Fifth KPI
        public List<ProjectInfo> OverdueProjectsWithIncompleteTasks { get; set; } = new();

        public class ProjectInfo
        {
            public int ProjectId { get; set; }
            public string Name { get; set; } = string.Empty;
            public DateTime EndDate { get; set; }
            public int IncompleteTasksCount { get; set; }
        }
    }
}