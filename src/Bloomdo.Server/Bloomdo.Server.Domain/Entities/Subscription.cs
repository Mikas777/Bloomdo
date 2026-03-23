using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Domain.Entities;

public class Subscription : BaseEntity
{
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }

    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }

    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }

    public bool CancelAtPeriodEnd { get; set; }
    public DateTime? CancelledAt { get; set; }
}
