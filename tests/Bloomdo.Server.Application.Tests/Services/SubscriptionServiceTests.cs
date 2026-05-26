using System.Linq.Expressions;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.Enums;
using SubscriptionService = Bloomdo.Server.Application.Services.SubscriptionService;

namespace Bloomdo.Server.Application.Tests.Services;

public class SubscriptionServiceTests
{
    private readonly Mock<ISubscriptionRepository> _subRepo = new();
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IChatRepository> _chatRepo = new();
    private readonly Mock<IRepository<BlockRule>> _blockRepo = new();
    private readonly Mock<IRepository<StreakFreeze>> _freezeRepo = new();
    private readonly FakeStripeSettings _stripe = new();
    private readonly FakeFreeLimitsSettings _free = new();

    private SubscriptionService BuildSut() => new(
        _subRepo.Object, _accountRepo.Object, _stripe, _free,
        _chatRepo.Object, _blockRepo.Object, _freezeRepo.Object);

    [Fact]
    public async Task GetStatusAsync_NoSubscription_ReturnsNoneWithFreeLimits()
    {
        var accountId = Guid.NewGuid();
        _subRepo.Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync((Subscription?)null);
        _chatRepo.Setup(r => r.CountTodayUserMessagesAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _blockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BlockRule, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var status = await BuildSut().GetStatusAsync(accountId);

        status.IsPremium.ShouldBeFalse();
        status.Status.ShouldBe(SubscriptionStatus.None);
        status.Limits.MaxDailyChatMessages.ShouldBe(_free.MaxDailyChatMessages);
        status.Limits.RemainingChatMessagesToday.ShouldBe(_free.MaxDailyChatMessages - 2);
        status.Limits.MaxBlockRules.ShouldBe(_free.MaxBlockRules);
    }

    [Fact]
    public async Task GetStatusAsync_ExpiredActiveSub_MarksExpired_ReturnsFalsePremium()
    {
        var accountId = Guid.NewGuid();
        var sub = TestData.BuildSubscription(accountId, SubscriptionStatus.Active, periodEnd: DateTime.UtcNow.AddDays(-1));
        _subRepo.Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(sub);
        _chatRepo.Setup(r => r.CountTodayUserMessagesAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _blockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BlockRule, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var status = await BuildSut().GetStatusAsync(accountId);

        sub.Status.ShouldBe(SubscriptionStatus.Expired);
        status.IsPremium.ShouldBeFalse();
        _subRepo.Verify(r => r.UpdateAsync(sub, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStatusAsync_ActivePremium_ReturnsUnlimitedLimits()
    {
        var accountId = Guid.NewGuid();
        var sub = TestData.BuildSubscription(accountId, SubscriptionStatus.Active);
        _subRepo.Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(sub);
        _freezeRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<StreakFreeze, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var status = await BuildSut().GetStatusAsync(accountId);

        status.IsPremium.ShouldBeTrue();
        status.Limits.MaxDailyChatMessages.ShouldBe(int.MaxValue);
        status.Limits.MaxBlockRules.ShouldBe(int.MaxValue);
        status.Limits.MonthlyStreakFreezes.ShouldBe(_free.MonthlyStreakFreezes);
        status.Limits.RemainingStreakFreezes.ShouldBe(_free.MonthlyStreakFreezes);
    }

    [Fact]
    public async Task IsPremiumAsync_ActiveAndNotExpired_True()
    {
        var accountId = Guid.NewGuid();
        var sub = TestData.BuildSubscription(accountId, SubscriptionStatus.Active);
        _subRepo.Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(sub);

        (await BuildSut().IsPremiumAsync(accountId)).ShouldBeTrue();
    }

    [Fact]
    public async Task IsPremiumAsync_ActiveButExpired_AutoExpires_False()
    {
        var accountId = Guid.NewGuid();
        var sub = TestData.BuildSubscription(accountId, SubscriptionStatus.Active, periodEnd: DateTime.UtcNow.AddHours(-1));
        _subRepo.Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(sub);

        var isPremium = await BuildSut().IsPremiumAsync(accountId);

        isPremium.ShouldBeFalse();
        sub.Status.ShouldBe(SubscriptionStatus.Expired);
    }

    [Fact]
    public async Task IsPremiumAsync_NoSubscription_False()
    {
        _subRepo.Setup(r => r.GetByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Subscription?)null);

        (await BuildSut().IsPremiumAsync(Guid.NewGuid())).ShouldBeFalse();
    }

    [Fact]
    public async Task GetStatusAsync_FreeUser_ReportsCurrentBlockCount()
    {
        var accountId = Guid.NewGuid();
        _subRepo.Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync((Subscription?)null);
        _chatRepo.Setup(r => r.CountTodayUserMessagesAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(99);
        _blockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BlockRule, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BlockRule> { TestData.BuildBlockRule(accountId), TestData.BuildBlockRule(accountId) });

        var status = await BuildSut().GetStatusAsync(accountId);

        status.Limits.CurrentBlockRuleCount.ShouldBe(2);
        status.Limits.RemainingChatMessagesToday.ShouldBe(0); // capped at zero
    }

    [Fact]
    public async Task HandleWebhook_CheckoutCompleted_NoExisting_CreatesSubscription()
    {
        var accountId = Guid.NewGuid();
        var json = BuildCheckoutSessionCompletedJson(accountId, "Monthly", customerId: "cus_X", subscriptionId: "sub_X");

        _subRepo.Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync((Subscription?)null);
        Subscription? created = null;
        _subRepo.Setup(r => r.CreateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .Callback<Subscription, CancellationToken>((s, _) => created = s)
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        await BuildSut().HandleWebhookAsync(json, stripeSignature: "");

        created.ShouldNotBeNull();
        created!.AccountId.ShouldBe(accountId);
        created.Plan.ShouldBe(SubscriptionPlan.Monthly);
        created.Status.ShouldBe(SubscriptionStatus.Active);
        created.StripeCustomerId.ShouldBe("cus_X");
        created.StripeSubscriptionId.ShouldBe("sub_X");
        (created.CurrentPeriodEnd - created.CurrentPeriodStart).TotalDays.ShouldBeGreaterThan(27);
    }

    [Fact]
    public async Task HandleWebhook_CheckoutCompleted_ExistingSub_Updates()
    {
        var accountId = Guid.NewGuid();
        var existing = TestData.BuildSubscription(accountId, SubscriptionStatus.Expired, periodEnd: DateTime.UtcNow.AddDays(-10));
        existing.StripeCustomerId = "cus_OLD";
        var json = BuildCheckoutSessionCompletedJson(accountId, "Yearly", customerId: "cus_NEW", subscriptionId: "sub_NEW");

        _subRepo.Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await BuildSut().HandleWebhookAsync(json, stripeSignature: "");

        existing.Status.ShouldBe(SubscriptionStatus.Active);
        existing.Plan.ShouldBe(SubscriptionPlan.Yearly);
        existing.StripeCustomerId.ShouldBe("cus_NEW");
        existing.StripeSubscriptionId.ShouldBe("sub_NEW");
        existing.CancelAtPeriodEnd.ShouldBeFalse();
        existing.CancelledAt.ShouldBeNull();
        _subRepo.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _subRepo.Verify(r => r.CreateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_CheckoutCompleted_InvalidAccountId_NoOp()
    {
        var json = BuildCheckoutSessionCompletedJson(rawAccountId: "not-a-guid", plan: "Monthly");

        await BuildSut().HandleWebhookAsync(json, stripeSignature: "");

        _subRepo.Verify(r => r.CreateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
        _subRepo.Verify(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_NoStripeId_Throws()
    {
        var accountId = Guid.NewGuid();
        var sub = TestData.BuildSubscription(accountId, stripeSubId: null);
        _subRepo.Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(sub);

        var act = () => BuildSut().CancelSubscriptionAsync(accountId);

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Builds a raw checkout.session.completed event JSON that Stripe's EventUtility.ParseEvent can deserialize.
    /// Avoids mocking Stripe internals — round-trips through real SDK types.
    /// </summary>
    /// <summary>
    /// Stripe's EventConverter is strict — it needs the full event envelope. We build raw JSON
    /// that matches Stripe's webhook payload shape exactly.
    /// </summary>
    private static string BuildCheckoutSessionCompletedJson(
        Guid? accountId = null,
        string plan = "Monthly",
        string? rawAccountId = null,
        string customerId = "cus_test",
        string subscriptionId = "sub_test")
    {
        var accountIdStr = rawAccountId ?? accountId?.ToString() ?? Guid.NewGuid().ToString();
        var sessionId = "cs_test_" + Guid.NewGuid().ToString("N");
        var eventId = "evt_" + Guid.NewGuid().ToString("N");
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "2026-02-25.clover",
          "created": {{created}},
          "livemode": false,
          "pending_webhooks": 0,
          "request": { "id": null, "idempotency_key": null },
          "type": "checkout.session.completed",
          "data": {
            "object": {
              "id": "{{sessionId}}",
              "object": "checkout.session",
              "customer": "{{customerId}}",
              "subscription": "{{subscriptionId}}",
              "mode": "subscription",
              "payment_status": "paid",
              "status": "complete",
              "metadata": {
                "accountId": "{{accountIdStr}}",
                "plan": "{{plan}}"
              }
            }
          }
        }
        """;
    }
}
