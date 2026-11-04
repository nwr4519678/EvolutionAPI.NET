using EvolutionApi.Application.Common.Monitoring;
using Microsoft.AspNetCore.Mvc;

namespace EvolutionApi.API.Controllers;

[ApiController]
[Route("system/usage")]
public sealed class UsageController(IDashboardUsageProvider usageProvider) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardUsageSnapshot>> Get(CancellationToken cancellationToken) =>
        Ok(await usageProvider.GetSnapshotAsync(cancellationToken));
}
