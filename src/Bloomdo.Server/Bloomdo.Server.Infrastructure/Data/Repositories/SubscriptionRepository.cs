using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bloomdo.Server.Infrastructure.Data.Repositories;

public class SubscriptionRepository(AppDbContext context) : ISubscriptionRepository
{
    public async Task<Subscription?> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default)
    {
        return await context.Subscriptions
            .FirstOrDefaultAsync(s => s.AccountId == accountId, ct);
    }

    public async Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken ct = default)
    {
        return await context.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, ct);
    }

    public async Task<Subscription?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken ct = default)
    {
        return await context.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeCustomerId == stripeCustomerId, ct);
    }

    public async Task<Subscription> CreateAsync(Subscription subscription, CancellationToken ct = default)
    {
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync(ct);
        return subscription;
    }

    public async Task UpdateAsync(Subscription subscription, CancellationToken ct = default)
    {
        context.Subscriptions.Update(subscription);
        await context.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
