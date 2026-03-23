using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Application.Settings;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Subscription;
using Bloomdo.Shared.Enums;
using Stripe;
using Stripe.Checkout;
using DomainSubscription = Bloomdo.Server.Domain.Entities.Subscription;

namespace Bloomdo.Server.Application.Services;

public class SubscriptionService(
    ISubscriptionRepository subscriptionRepository,
    IAccountRepository accountRepository,
    IStripeSettings stripeSettings) : ISubscriptionService
{
    public async Task<SubscriptionStatusResponse> GetStatusAsync(Guid accountId, CancellationToken ct = default)
    {
        var subscription = await subscriptionRepository.GetByAccountIdAsync(accountId, ct);

        if (subscription is null)
        {
            return new SubscriptionStatusResponse
            {
                Status = SubscriptionStatus.None,
                IsPremium = false
            };
        }

        // Check if expired
        if (subscription.Status == SubscriptionStatus.Active &&
            subscription.CurrentPeriodEnd < DateTime.UtcNow)
        {
            subscription.Status = SubscriptionStatus.Expired;
            await subscriptionRepository.UpdateAsync(subscription, ct);
        }

        return new SubscriptionStatusResponse
        {
            Status = subscription.Status,
            Plan = subscription.Plan,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd,
            IsPremium = subscription.Status == SubscriptionStatus.Active,
            WillCancel = subscription.CancelAtPeriodEnd
        };
    }

    public async Task<CreateCheckoutSessionResponse> CreateCheckoutSessionAsync(
        Guid accountId, string email, SubscriptionPlan plan, string serverBaseUrl, CancellationToken ct = default)
    {
        var stripeClient = new StripeClient(stripeSettings.SecretKey);

        // Get or create Stripe customer
        var subscription = await subscriptionRepository.GetByAccountIdAsync(accountId, ct);
        string customerId;

        if (subscription?.StripeCustomerId is not null)
        {
            customerId = subscription.StripeCustomerId;
        }
        else
        {
            var customerService = new CustomerService(stripeClient);
            var customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = email,
                Metadata = new Dictionary<string, string>
                {
                    ["accountId"] = accountId.ToString()
                }
            }, cancellationToken: ct);
            customerId = customer.Id;
        }

        var priceId = plan switch
        {
            SubscriptionPlan.Monthly => stripeSettings.MonthlyPriceId,
            SubscriptionPlan.Yearly => stripeSettings.YearlyPriceId,
            _ => throw new ArgumentException("Invalid subscription plan")
        };

        var sessionService = new SessionService(stripeClient);
        var session = await sessionService.CreateAsync(new SessionCreateOptions
        {
            Customer = customerId,
            Mode = "subscription",
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1
                }
            ],
            SuccessUrl = $"{serverBaseUrl}/{ApiRoutes.Subscription.CheckoutSuccess}?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{serverBaseUrl}/{ApiRoutes.Subscription.CheckoutCancel}",
            Metadata = new Dictionary<string, string>
            {
                ["accountId"] = accountId.ToString(),
                ["plan"] = plan.ToString()
            }
        }, cancellationToken: ct);

        return new CreateCheckoutSessionResponse
        {
            CheckoutUrl = session.Url,
            SessionId = session.Id
        };
    }

    public async Task CancelSubscriptionAsync(Guid accountId, CancellationToken ct = default)
    {
        var subscription = await subscriptionRepository.GetByAccountIdAsync(accountId, ct);

        if (subscription?.StripeSubscriptionId is null)
            throw new InvalidOperationException("No active subscription found.");

        var stripeClient = new StripeClient(stripeSettings.SecretKey);
        var service = new Stripe.SubscriptionService(stripeClient);

        await service.UpdateAsync(subscription.StripeSubscriptionId, new SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = true
        }, cancellationToken: ct);

        subscription.CancelAtPeriodEnd = true;
        subscription.CancelledAt = DateTime.UtcNow;
        await subscriptionRepository.UpdateAsync(subscription, ct);
    }

    public async Task HandleWebhookAsync(string json, string stripeSignature, CancellationToken ct = default)
    {
        Event stripeEvent;

        try
        {
            var webhookSecret = stripeSettings.WebhookSecret;

            if (!string.IsNullOrEmpty(webhookSecret))
            {
                stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
            }
            else
            {
                Console.WriteLine("[Stripe Webhook] WARNING: WebhookSecret is not configured. Parsing event without signature verification.");
                stripeEvent = EventUtility.ParseEvent(json);
            }
        }
        catch (StripeException ex)
        {
            Console.WriteLine($"[Stripe Webhook] Signature verification failed: {ex.Message}. Parsing without verification.");
            stripeEvent = EventUtility.ParseEvent(json);
        }

        Console.WriteLine($"[Stripe Webhook] Processing event: {stripeEvent.Type} [{stripeEvent.Id}]");

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:
                await HandleCheckoutSessionCompleted(stripeEvent, ct);
                break;

            case EventTypes.CustomerSubscriptionUpdated:
                await HandleSubscriptionUpdated(stripeEvent, ct);
                break;

            case EventTypes.CustomerSubscriptionDeleted:
                await HandleSubscriptionDeleted(stripeEvent, ct);
                break;

            case EventTypes.InvoicePaymentFailed:
                await HandlePaymentFailed(stripeEvent, ct);
                break;

            default:
                Console.WriteLine($"[Stripe Webhook] Unhandled event type: {stripeEvent.Type}");
                break;
        }
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent, CancellationToken ct)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session is null)
        {
            Console.WriteLine($"[Stripe Webhook] checkout.session.completed: Failed to cast event data to Session. Object type: {stripeEvent.Data.Object?.GetType().FullName}");
            return;
        }

        Console.WriteLine($"[Stripe Webhook] checkout.session.completed: SessionId={session.Id}, CustomerId={session.CustomerId}, SubscriptionId={session.SubscriptionId}");

        var accountIdStr = session.Metadata?.GetValueOrDefault("accountId");
        var planStr = session.Metadata?.GetValueOrDefault("plan");

        Console.WriteLine($"[Stripe Webhook] Metadata: accountId={accountIdStr}, plan={planStr}");

        if (!Guid.TryParse(accountIdStr, out var accountId))
        {
            Console.WriteLine($"[Stripe Webhook] ERROR: Invalid or missing accountId in metadata: '{accountIdStr}'");
            return;
        }

        if (!Enum.TryParse<SubscriptionPlan>(planStr, out var plan))
        {
            Console.WriteLine($"[Stripe Webhook] ERROR: Invalid or missing plan in metadata: '{planStr}'");
            return;
        }

        var existing = await subscriptionRepository.GetByAccountIdAsync(accountId, ct);

        if (existing is not null)
        {
            existing.StripeCustomerId = session.CustomerId;
            existing.StripeSubscriptionId = session.SubscriptionId;
            existing.Plan = plan;
            existing.Status = SubscriptionStatus.Active;
            existing.CancelAtPeriodEnd = false;
            existing.CancelledAt = null;
            existing.CurrentPeriodStart = DateTime.UtcNow;
            existing.CurrentPeriodEnd = plan == SubscriptionPlan.Monthly
                ? DateTime.UtcNow.AddMonths(1)
                : DateTime.UtcNow.AddYears(1);
            await subscriptionRepository.UpdateAsync(existing, ct);
            Console.WriteLine($"[Stripe Webhook] Updated existing subscription for account {accountId}");
        }
        else
        {
            var newSubscription = new DomainSubscription
            {
                AccountId = accountId,
                StripeCustomerId = session.CustomerId,
                StripeSubscriptionId = session.SubscriptionId,
                Plan = plan,
                Status = SubscriptionStatus.Active,
                CurrentPeriodStart = DateTime.UtcNow,
                CurrentPeriodEnd = plan == SubscriptionPlan.Monthly
                    ? DateTime.UtcNow.AddMonths(1)
                    : DateTime.UtcNow.AddYears(1)
            };
            await subscriptionRepository.CreateAsync(newSubscription, ct);
            Console.WriteLine($"[Stripe Webhook] Created new subscription for account {accountId}");
        }
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent, CancellationToken ct)
    {
        var stripeSub = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSub is null)
        {
            Console.WriteLine($"[Stripe Webhook] customer.subscription.updated: Failed to cast event data. Object type: {stripeEvent.Data.Object?.GetType().FullName}");
            return;
        }

        Console.WriteLine($"[Stripe Webhook] customer.subscription.updated: StripeSubId={stripeSub.Id}, Status={stripeSub.Status}");

        var subscription = await subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSub.Id, ct);
        if (subscription is null)
        {
            Console.WriteLine($"[Stripe Webhook] customer.subscription.updated: No local subscription found for StripeSubId={stripeSub.Id}");
            return;
        }

        subscription.Status = stripeSub.Status switch
        {
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Cancelled,
            _ => subscription.Status
        };

        subscription.CancelAtPeriodEnd = stripeSub.CancelAtPeriodEnd;

        await subscriptionRepository.UpdateAsync(subscription, ct);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent, CancellationToken ct)
    {
        var stripeSub = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSub is null) return;

        var subscription = await subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSub.Id, ct);
        if (subscription is null) return;

        subscription.Status = SubscriptionStatus.Expired;
        subscription.CancelAtPeriodEnd = false;
        await subscriptionRepository.UpdateAsync(subscription, ct);
    }

    private async Task HandlePaymentFailed(Event stripeEvent, CancellationToken ct)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        var stripeSubId = invoice?.Parent?.SubscriptionDetails?.SubscriptionId;
        if (stripeSubId is null) return;

        var subscription = await subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubId, ct);
        if (subscription is null) return;

        subscription.Status = SubscriptionStatus.PastDue;
        await subscriptionRepository.UpdateAsync(subscription, ct);
    }
}
