using Bloomdo.Shared.DTOs.Subscription;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Client.Core.Interfaces;

public interface ISubscriptionApiService
{
    Task<SubscriptionStatusResponse?> GetStatusAsync(CancellationToken ct = default);
    Task<CreateCheckoutSessionResponse?> CreateCheckoutSessionAsync(SubscriptionPlan plan, CancellationToken ct = default);
    Task<bool> CancelSubscriptionAsync(CancellationToken ct = default);
}
