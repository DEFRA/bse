using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BSE.Host.Tests;

internal static class PageTestContext
{
    public static T Blank<T>() where T : class => (T)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(T));
    public static void Attach(PageModel model, params string[] roles)
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                roles.Select(r => new Claim(ClaimTypes.Role, r)), "test"))
        };
        model.PageContext = new PageContext { HttpContext = http };
        model.PageContext.ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
            new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
    }
}
