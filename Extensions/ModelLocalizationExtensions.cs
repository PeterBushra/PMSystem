using Jobick.Models;

namespace Jobick.Extensions;

/// <summary>
/// Common helpers to normalize and mirror bilingual fields.
/// </summary>
public static class ModelLocalizationExtensions
{
    /// <summary>
    /// Mirrors Arabic fields on Project when not explicitly provided.
    /// </summary>
    public static void MirrorArabicFromEnglish(this Project project)
    {
        if (project == null) return;
        project.NameAr = string.IsNullOrWhiteSpace(project.NameAr) ? project.Name : project.NameAr;
        project.DescriptionAr = string.IsNullOrWhiteSpace(project.DescriptionAr) ? project.Description : project.DescriptionAr;
    }

    /// <summary>
    /// Mirrors Arabic fields on Task when not explicitly provided.
    /// </summary>
    public static void MirrorArabicFromEnglish(this Jobick.Models.Task task)
    {
        if (task == null) return;
        task.StageNameAr = string.IsNullOrWhiteSpace(task.StageNameAr) ? task.StageName ?? task.StageNameAr : task.StageNameAr;
        task.TaskAr = string.IsNullOrWhiteSpace(task.TaskAr) ? task.Task1 ?? task.TaskAr : task.TaskAr;
    }
}
