using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace EvolutionApi.Dashboard.Server.Endpoints;

public static class ManagerAuthEndpoints
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/manager/login", (Delegate)new Func<ManagerLoginRequest, HttpContext, IConfiguration, Task<IResult>>(Login))
            .AllowAnonymous()
            .RequireRateLimiting("manager-login");

        endpoints.MapPost("/manager/logout", Logout).RequireAuthorization();
        endpoints.MapGet("/manager/session", GetSession).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> Login(ManagerLoginRequest request, HttpContext context, IConfiguration configuration)
    {
        if (!IsSameOrigin(context)) return Results.BadRequest(new { message = "Cross-origin login requests are not allowed." });

        var expected = configuration["Dashboard:ManagerPassword"];
        if (string.IsNullOrWhiteSpace(expected))
            return Results.Problem("Manager login is not configured on the server.", statusCode: StatusCodes.Status503ServiceUnavailable);
        if (!FixedTimeEquals(request.Password, expected)) return Results.Unauthorized();

        var claims = new[] { new Claim(ClaimTypes.Name, "manager"), new Claim(ClaimTypes.Role, "Manager") };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });
        return Results.Ok(new { authenticated = true });
    }

    private static async Task<IResult> Logout(HttpContext context)
    {
        if (!IsSameOrigin(context)) return Results.BadRequest();
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok(new { authenticated = false });
    }

    private static IResult GetSession(HttpContext context) =>
        Results.Ok(new { authenticated = context.User.Identity?.IsAuthenticated == true });

    private static bool IsSameOrigin(HttpContext context)
    {
        var origin = context.Request.Headers["Origin"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(origin)) return true;
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var parsed)) return false;

        var requestPort = context.Request.Host.Port ?? (context.Request.IsHttps ? 443 : 80);
        return string.Equals(parsed.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase) &&
               parsed.Port == requestPort &&
               string.Equals(parsed.Scheme, context.Request.Scheme, StringComparison.OrdinalIgnoreCase);
    }

    private static bool FixedTimeEquals(string supplied, string expected)
    {
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied ?? string.Empty);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return suppliedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    }

    private sealed record ManagerLoginRequest(string Password);
}
