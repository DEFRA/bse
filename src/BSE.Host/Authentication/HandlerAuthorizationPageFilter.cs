using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BSE.Host.Authentication;

/// <summary>
/// Razor Pages ignore <c>[Authorize]</c> on handler methods. This filter enforces it for the handler about to run
/// so role rules live in named policies rather than in-handler checks. A failure returns <c>Forbid()</c>,
/// the same outcome as the in-handler checks it backs.
/// </summary>
public sealed class HandlerAuthorizationPageFilter(IAuthorizationPolicyProvider policyProvider, IAuthorizationService authorizationService)
    : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var method = context.HandlerMethod?.MethodInfo;
        var authorizeData = method?.GetCustomAttributes(inherit: true).OfType<IAuthorizeData>().ToList();

        if (authorizeData is { Count: > 0 })
        {
            var policy = await AuthorizationPolicy.CombineAsync(policyProvider, authorizeData);
            if (policy is not null)
            {
                var result = await authorizationService.AuthorizeAsync(context.HttpContext.User, context.HandlerInstance, policy);
                if (!result.Succeeded)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }

        await next();
    }
}
