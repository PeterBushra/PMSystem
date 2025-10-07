using Microsoft.AspNetCore.Mvc.Rendering;

namespace Jobick.Extensions;

public static class SelectListExtensions
{
    /// <summary>
    /// Converts a sequence of strings to a list of SelectListItem for dropdowns.
    /// </summary>
    public static List<SelectListItem> ToSelectList(this IEnumerable<string> source)
    {
        return source.Select(d => new SelectListItem { Text = d, Value = d }).ToList();
    }
}
