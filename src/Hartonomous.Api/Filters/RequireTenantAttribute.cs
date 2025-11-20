using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Hartonomous.Api.Filters;

/// <summary>
/// Action filter that validates tenant ID is present in user claims.
/// Returns 401 Unauthorized with Problem Details if tenant ID is missing.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireTenantAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var controller = context.Controller as ControllerBase;
        if (controller == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        var tenantClaim = controller.User.FindFirst("tenant_id");

        if (tenantClaim == null)
        {
            context.Result = new UnauthorizedObjectResult(
                new ProblemDetails
                {
                    Status = 401,
                    Title = "Tenant Required",
                    Detail = "Tenant ID not found in user claims. Multi-tenant isolation requires a valid tenant_id claim.",
                    Instance = context.HttpContext.Request.Path
                });
        }
    }
}
