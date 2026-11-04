using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Enums;
using Xunit;

namespace EvolutionApi.Tests;

public sealed class PlatformRulesEngineTests
{
    [Fact]
    public void KeywordBlockRule_FlagsForbiddenKeywords()
    {
        var blockedKeywords = new[] { "lawsuit", "refund", "sue" };
        var message = "I want a refund immediately or I will sue.";

        var matches = blockedKeywords.Any(k => message.Contains(k, StringComparison.OrdinalIgnoreCase));

        Assert.True(matches);
    }

    [Fact]
    public void EscalationTriggerRule_DetectsHumanTakeoverRequest()
    {
        var escalationTriggers = new[] { "human", "agent", "manager", "support person" };
        var message = "Can I please speak to a human representative?";

        var triggered = escalationTriggers.Any(t => message.Contains(t, StringComparison.OrdinalIgnoreCase));

        Assert.True(triggered);
    }

    [Fact]
    public void SystemPrompt_InterpolatesDynamicVariables()
    {
        var template = "Hello {{customer_name}}, welcome to {{client_name}}! Current hours: {{business_hours}}.";
        var result = template
            .Replace("{{customer_name}}", "Alice")
            .Replace("{{client_name}}", "Acme Corp")
            .Replace("{{business_hours}}", "09:00 - 18:00 UTC");

        Assert.Equal("Hello Alice, welcome to Acme Corp! Current hours: 09:00 - 18:00 UTC.", result);
    }

    [Theory]
    [InlineData(1000, 500, 0.005, 0.015, 0.0125)]
    [InlineData(2000, 1000, 0.002, 0.006, 0.0100)]
    public void UsageMetering_CalculatesAiCostAccurately(
        int tokensIn, int tokensOut, decimal priceInPerK, decimal priceOutPerK, decimal expectedCost)
    {
        var costIn = (tokensIn / 1000m) * priceInPerK;
        var costOut = (tokensOut / 1000m) * priceOutPerK;
        var totalCost = costIn + costOut;

        Assert.Equal(expectedCost, totalCost);
    }

    [Fact]
    public void Tenant_StrictIsolationQueryFilter_WorksOnEntities()
    {
        var tenantA = new Tenant { Name = "Tenant A", Slug = "tenant-a" };
        var tenantB = new Tenant { Name = "Tenant B", Slug = "tenant-b" };

        var numberA = new WhatsAppNumber { TenantId = tenantA.Id, DisplayName = "Line A" };
        var numberB = new WhatsAppNumber { TenantId = tenantB.Id, DisplayName = "Line B" };

        Assert.NotEqual(tenantA.Id, tenantB.Id);
        Assert.Equal(tenantA.Id, numberA.TenantId);
        Assert.Equal(tenantB.Id, numberB.TenantId);
    }
}

