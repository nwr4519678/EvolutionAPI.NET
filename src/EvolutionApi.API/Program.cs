using EvolutionApi.API;
using EvolutionApi.API.Startup;

var builder = WebApplication.CreateBuilder(args);
builder.ValidateProductionConfiguration();

// ── Dependency Injection (All Architectural Layers) ───────────────────────────
builder.Services.AddGatewayServices(builder.Configuration);

var app = builder.Build();
app.UseGatewayApplication();

app.Run();
