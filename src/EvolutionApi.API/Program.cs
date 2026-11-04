using EvolutionApi.API;
using EvolutionApi.API.Startup;
using EvolutionApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.ValidateProductionConfiguration();

// ── Dependency Injection (All Architectural Layers) ───────────────────────────
builder.Services.AddGatewayServices(builder.Configuration);

var app = builder.Build();

// Create the database and apply the EF schema before Hangfire resolves its SQL
// storage through the dashboard middleware or starts any background workers.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    app.Logger.LogInformation("Applying database migrations before starting Hangfire.");
    await db.Database.MigrateAsync();
}

app.UseGatewayApplication();

app.Run();
