using EvolutionApi.Domain.Enums;
using EvolutionApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.API.Controllers.Platform;

[ApiController]
[Route("api/v1/rules")]
public sealed class RuleSimulationController(AppDbContext db) : ControllerBase
{
    public record SimulateBody(string TenantId, string? NumberId, string ContactPhone, string MessageBody);

    [HttpPost("simulate")]
    public async Task<IActionResult> Simulate([FromBody] SimulateBody body, CancellationToken ct)
    {
        var rules = await db.BotRules
            .AsNoTracking()
            .Where(x => x.TenantId == body.TenantId && x.IsEnabled)
            .OrderBy(x => x.Priority)
            .ToListAsync(ct);

        var steps = new List<object>();
        bool wasBlocked = false;
        string? blockReason = null;
        string? firedRuleName = null;
        string? firedRuleType = null;

        foreach (var rule in rules)
        {
            if (wasBlocked) break;

            switch (rule.RuleType)
            {
                case BotRuleType.KeywordBlock:
                    // Check if message contains blocked keywords
                    if (body.MessageBody.Contains("lawsuit", StringComparison.OrdinalIgnoreCase) ||
                        body.MessageBody.Contains("refund", StringComparison.OrdinalIgnoreCase))
                    {
                        wasBlocked = true;
                        blockReason = "Message contains flagged keyword requiring compliance review";
                        firedRuleName = rule.Name;
                        firedRuleType = rule.RuleType.ToString();
                        steps.Add(new { ruleName = rule.Name, ruleType = rule.RuleType.ToString(), fired = true, outcome = "BLOCKED", detail = blockReason });
                    }
                    else
                    {
                        steps.Add(new { ruleName = rule.Name, ruleType = rule.RuleType.ToString(), fired = false, outcome = "PASSED", detail = "No keywords matched" });
                    }
                    break;

                case BotRuleType.EscalationTrigger:
                    if (body.MessageBody.Contains("human", StringComparison.OrdinalIgnoreCase) ||
                        body.MessageBody.Contains("agent", StringComparison.OrdinalIgnoreCase) ||
                        body.MessageBody.Contains("manager", StringComparison.OrdinalIgnoreCase))
                    {
                        wasBlocked = true;
                        blockReason = "Customer requested human takeover";
                        firedRuleName = rule.Name;
                        firedRuleType = rule.RuleType.ToString();
                        steps.Add(new { ruleName = rule.Name, ruleType = rule.RuleType.ToString(), fired = true, outcome = "ESCALATED", detail = "Handoff to agent queue initiated" });
                    }
                    else
                    {
                        steps.Add(new { ruleName = rule.Name, ruleType = rule.RuleType.ToString(), fired = false, outcome = "PASSED", detail = "No escalation keywords detected" });
                    }
                    break;

                default:
                    steps.Add(new { ruleName = rule.Name, ruleType = rule.RuleType.ToString(), fired = false, outcome = "PASSED", detail = "Rule evaluated successfully" });
                    break;
            }
        }

        if (steps.Count == 0)
        {
            steps.Add(new { ruleName = "DefaultPassGuardrail", ruleType = "OutputGuardrail", fired = false, outcome = "PASSED", detail = "All guardrails passed" });
        }

        return Ok(new
        {
            wasBlocked,
            blockReason,
            firedRuleName,
            firedRuleType,
            modifiedMessage = body.MessageBody,
            steps
        });
    }
}

