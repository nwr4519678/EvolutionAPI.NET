using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;

namespace EvolutionApi.Dashboard.Server.Startup;

public static class DashboardServerServiceExtensions
{
    public static WebApplicationBuilder AddDashboardServer(this WebApplicationBuilder builder)
    {
        ValidateConfiguration(builder);

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "Evolution.Manager";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        builder.Services.AddAuthorization();
        builder.Services.AddHttpClient("evolution-api", client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5051/", UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("manager-login", limiter =>
        {
            limiter.PermitLimit = 5;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
            limiter.AutoReplenishment = true;
        }));

        return builder;
    }

    private static void ValidateConfiguration(WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment()) return;

        var required = new[] { "Dashboard:ManagerPassword", "Evolution:ApiKey", "ApiBaseUrl" };
        var missing = required.Where(key => string.IsNullOrWhiteSpace(builder.Configuration[key])).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"Dashboard server is missing configuration: {string.Join(", ", missing)}.");
    }
}
