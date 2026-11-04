namespace EvolutionApi.Domain.Enums;

public enum BotRuleType
{
    BusinessHours,
    KeywordBlock,
    KeywordAllow,
    PhoneBlacklist,
    PhoneWhitelist,
    RateLimit,
    QuotaCheck,
    LanguageDetection,
    EscalationTrigger,
    OutputGuardrail,
    WelcomeMessage,
    ConversationTimeout
}
