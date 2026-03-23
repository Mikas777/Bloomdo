using Bloomdo.Shared.Enums;

namespace Bloomdo.Shared.DTOs.Subscription;

public class CreateCheckoutSessionRequest
{
    public SubscriptionPlan Plan { get; set; }
}
