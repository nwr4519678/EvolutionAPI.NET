using EvolutionApi.Dashboard.Server.Startup;

var builder = WebApplication.CreateBuilder(args);
builder.AddDashboardServer();
var app = builder.Build();
app.UseDashboardServer();
app.Run();
