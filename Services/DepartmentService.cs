namespace Jobick.Services;

public class DepartmentService
{
    static List<string> Departments = new List<string>
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

    // Expose departments for UI bindings
    public static IReadOnlyList<string> GetDepartments() => Departments;
}
