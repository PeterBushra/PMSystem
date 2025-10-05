using Jobick.Models;
using Jobick.ViewModels;

namespace Jobick.Services.Interfaces;

public interface IProjectKpiService
{
    ProjectKPIs Calculate(Project project);
}
