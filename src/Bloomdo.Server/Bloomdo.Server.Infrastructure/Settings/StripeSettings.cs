namespace Bloomdo.Server.Infrastructure.Settings;

public class StripeSettings : Application.Settings.IStripeSettings
{
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string? WebhookSecret { get; set; }
    public string MonthlyPriceId { get; set; } = string.Empty;
    public string YearlyPriceId { get; set; } = string.Empty;
}
