using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace EvolutionApi.API.Controllers;

/// <summary>
/// System info controller — index and credential verification.
/// Contains zero business logic.
/// </summary>
[ApiController]
[Route("/")]
public sealed class SystemController(IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public IActionResult Index()
    {
        var serverUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(new
        {
            status = 200,
            message = "Welcome to the Evolution API (.NET Clean Architecture), it is working!",
            version = configuration["Evolution:Version"] ?? "2.3.7",
            manager = serverUrl + "/scalar/v1",
            documentation = "https://doc.evolution-api.com"
        });
    }

    [HttpPost("verify-creds")]
    public IActionResult VerifyCredentials() => Ok(new
    {
        status = 200,
        message = "Credentials are valid"
    });
}
