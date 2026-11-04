using EvolutionApi.API.Middleware;
using EvolutionApi.Infrastructure.Hubs;
using EvolutionApi.Infrastructure.Security;
using EvolutionApi.Infrastructure.WhatsApp.Services;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

namespace EvolutionApi.API.Startup;

/// <summary>
/// Configures the HTTP middleware and endpoint surface for the gateway.
/// This keeps Program.cs limited to application composition.
/// </summary>
public static class GatewayApplicationExtensions
{
    public static WebApplication UseGatewayApplication(this WebApplication app)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseCors("dashboard");
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        app.UseApiMultiTenant();
        app.UseRateLimiter();

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            DarkModeEnabled = true,
            Authorization = [app.Services.GetRequiredService<HangfireDashboardAuthorizationFilter>()]
        });

        MapDocumentation(app);
        app.MapWhatsAppWebSockets();
        app.MapHub<InstanceHub>("/hubs/instance");
        app.MapHub<EventHub>("/hubs/events");
        app.MapHub<DashboardHub>("/hubs/dashboard");
        app.MapControllers().RequireRateLimiting("api");

        return app;
    }

    private static void MapDocumentation(WebApplication app)
    {
        var exposeApiDocs = app.Environment.IsDevelopment() ||
                            app.Configuration.GetValue<bool>("ApiDocumentation:Enabled");
        if (!exposeApiDocs) return;

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Evolution API Documentation")
                .WithTheme(ScalarTheme.Moon)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });
    }
}
