namespace Bloomdo.Shared.DTOs.Subscription;

public class CreateCheckoutSessionResponse
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}
