using System.IO;
using System.Linq;

namespace Jobick.Services;

public static class DepartmentService
{
    private static readonly object _sync = new();
    private static IReadOnlyList<string> _departments = Array.Empty<string>();
    private static bool _loaded;

    // Fallback values (used when CSV file is missing/empty or any error occurs)
    private static readonly string[] _fallback = new[]
    {
        "قسم الموارد البشرية",
        "قسم المالية",
        "قسم تقنية المعلومات",
        "قسم المشتريات",
        "قسم التسويق",
        "قسم المبيعات",
        "قسم الصيانة",
        "قسم الإنتاج",
        "قسم التخطيط",
        "قسم الجودة"
    };

    // CSV is copied to output (see csproj). We read it once, lazily.
    private static void EnsureLoaded()
    {
        if (_loaded) return;
        lock (_sync)
        {
            if (_loaded) return;
            try
            {
                var baseDir = AppContext.BaseDirectory; // bin/<config>/<tfm>
                var csvPath = Path.Combine(baseDir, "Data", "departments.csv");
                if (File.Exists(csvPath))
                {
                    var lines = File.ReadAllLines(csvPath);
                    var values = lines
                        .Select(l => (l ?? string.Empty).Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct()
                        .ToList();

                    _departments = values.Count > 0 ? values : _fallback;
                }
                else
                {
                    _departments = _fallback;
                }
            }
            catch
            {
                _departments = _fallback;
            }
            finally
            {
                _loaded = true;
            }
        }
    }

    // Expose departments for UI bindings
    public static IReadOnlyList<string> GetDepartments()
    {
        EnsureLoaded();
        return _departments;
    }

    // Optional: allow manual refresh if the CSV changes while the app is running
    public static void Refresh()
    {
        lock (_sync)
        {
            _loaded = false;
            _departments = Array.Empty<string>();
        }
    }
}
