using Bloomdo.Server.Domain.Entities;

namespace Bloomdo.Server.Application.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default);
    Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken ct = default);
    Task<Subscription?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken ct = default);
    Task<Subscription> CreateAsync(Subscription subscription, CancellationToken ct = default);
    Task UpdateAsync(Subscription subscription, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
