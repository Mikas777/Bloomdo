using Bloomdo.Server.Application.Settings;

namespace Bloomdo.Server.Application.Tests.Common;

public sealed class FakeAuthSettings(int refreshTokenExpirationDays = 30) : IAuthSettings
{
    public int RefreshTokenExpirationDays { get; } = refreshTokenExpirationDays;
}

public sealed class FakeFreeLimitsSettings : IFreeLimitsSettings
{
    public int MaxDailyChatMessages { get; init; } = 5;
    public int MaxBlockRules { get; init; } = 3;
    public bool CanCustomizeEmoji { get; init; } = false;
    public bool CanCustomizeColors { get; init; } = false;
    public int MonthlyStreakFreezes { get; init; } = 2;
    public bool CanViewWeeklyStats { get; init; } = false;
}

public sealed class FakeStripeSettings : IStripeSettings
{
    public string PublishableKey { get; init; } = "pk_test_fake";
    public string SecretKey { get; init; } = "sk_test_fake";
    public string? WebhookSecret { get; init; } = null;
    public string MonthlyPriceId { get; init; } = "price_monthly_fake";
    public string YearlyPriceId { get; init; } = "price_yearly_fake";
}

public sealed class FakeGeminiSettings(params string[] keys) : IGeminiSettings
{
    public IReadOnlyList<string> ApiKeys { get; } = keys;
}
