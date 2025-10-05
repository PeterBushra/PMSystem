using Jobick.Models;
using Jobick.ViewModels;

namespace Jobick.Services.Interfaces;

public interface IStatisticsService
{
    ProjectStatisticsVM CalculateDashboard(IEnumerable<Project> projects, IEnumerable<Jobick.Models.Task> allTasks, DateTime? now = null);
}
