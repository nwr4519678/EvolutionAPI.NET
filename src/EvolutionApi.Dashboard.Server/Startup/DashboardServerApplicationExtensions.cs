using EvolutionApi.Dashboard.Server.Endpoints;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

namespace EvolutionApi.Dashboard.Server.Startup;

public static class DashboardServerApplicationExtensions
{
    public static WebApplication UseDashboardServer(this WebApplication app)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        ManagerAuthEndpoints.Map(app);
        ApiProxyEndpoints.Map(app);
        app.MapFallbackToFile("index.html");
        return app;
    }
}
