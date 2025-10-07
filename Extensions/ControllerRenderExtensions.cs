using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.IO;
using System.Threading.Tasks;

namespace Jobick;

public static class ControllerRenderExtensions
{
    public static string RenderPartialViewToString(this Controller controller, string viewName, object model)
    {
        controller.ViewData.Model = model;
        using var writer = new StringWriter();

        var serviceProvider = controller.HttpContext.RequestServices;
        var viewEngine = (ICompositeViewEngine)serviceProvider.GetService(typeof(ICompositeViewEngine))!;
        var tempDataProvider = (ITempDataProvider)serviceProvider.GetService(typeof(ITempDataProvider))!;

        var viewResult = viewEngine.FindView(controller.ControllerContext, viewName, false);
        if (viewResult.View == null)
        {
            throw new FileNotFoundException("تعذر العثور على العرض الجزئي: " + viewName);
        }

        var viewContext = new ViewContext(
            controller.ControllerContext,
            viewResult.View,
            controller.ViewData,
            controller.TempData,
            writer,
            new HtmlHelperOptions()
        );

        viewResult.View.RenderAsync(viewContext).GetAwaiter().GetResult();
        return writer.ToString();
    }
}
