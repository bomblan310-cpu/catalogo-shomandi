using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Cryptography;
using System.Text;

namespace MobileCatalog.WebApi.Security;

public sealed class AdminApiKeyFilter(IConfiguration configuration) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            await next();
            return;
        }
        var expectedKey = configuration["Admin:ApiKey"];
        var suppliedKey = context.HttpContext.Request.Headers["X-Admin-Key"].ToString();
        var matches = !string.IsNullOrWhiteSpace(expectedKey) &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedKey), Encoding.UTF8.GetBytes(suppliedKey));
        if (!matches)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Se requiere una clave administrativa válida." });
            return;
        }

        await next();
    }
}
