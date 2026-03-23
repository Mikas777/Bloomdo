namespace Bloomdo.Server.Application.Settings;

public interface IStripeSettings
{
    string PublishableKey { get; }
    string SecretKey { get; }
    string? WebhookSecret { get; }
    string MonthlyPriceId { get; }
    string YearlyPriceId { get; }
}
