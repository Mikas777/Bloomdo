using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Application.Tests.Common;

/// <summary>
/// Factory helpers for building entities with safe defaults. Tests override only what they care about.
/// </summary>
public static class TestData
{
    public static Account BuildAccount(
        Guid? id = null,
        string email = "user@example.com",
        string password = "Password123!",
        string username = "user",
        UserRole role = UserRole.User)
    {
        return new Account
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Username = username,
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            IsEmailConfirmed = true,
            AccountRoles = [new AccountRole { RoleId = (int)role }]
        };
    }

    public static RefreshToken BuildRefreshToken(
        Guid accountId,
        string token = "rt_test",
        bool revoked = false,
        DateTime? expiresAt = null,
        string? replacedBy = null,
        DateTime? revokedAt = null)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Token = token,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(30),
            IsRevoked = revoked,
            ReplacedByToken = replacedBy,
            RevokedAt = revokedAt,
            CreatedByIp = "127.0.0.1"
        };
    }

    public static Subscription BuildSubscription(
        Guid accountId,
        SubscriptionStatus status = SubscriptionStatus.Active,
        SubscriptionPlan plan = SubscriptionPlan.Monthly,
        DateTime? periodEnd = null,
        string? stripeSubId = "sub_test",
        string? stripeCustomerId = "cus_test")
    {
        return new Subscription
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Status = status,
            Plan = plan,
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-15),
            CurrentPeriodEnd = periodEnd ?? DateTime.UtcNow.AddDays(15),
            StripeSubscriptionId = stripeSubId,
            StripeCustomerId = stripeCustomerId
        };
    }

    public static BlockRule BuildBlockRule(
        Guid accountId,
        BlockType type = BlockType.Schedule,
        string title = "Block",
        int? focusDurationMinutes = null,
        Guid? requiredActivityGroupId = null)
    {
        return new BlockRule
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Title = title,
            Type = type,
            IsActive = true,
            BlockedPackagesJson = "[]",
            FocusDurationMinutes = focusDurationMinutes,
            FocusStartedAtUtc = type == BlockType.Focus ? DateTime.UtcNow : null,
            RequiredActivityGroupId = requiredActivityGroupId
        };
    }

    public static ActivityGroup BuildGroup(Guid accountId, string title = "Morning", int sortOrder = 1)
    {
        return new ActivityGroup
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Title = title,
            Icon = "📋",
            Color = "#7E57C2",
            SortOrder = sortOrder,
            IsActive = true
        };
    }

    public static ActivityItem BuildItem(
        Guid groupId,
        string title = "Task",
        Bloomdo.Shared.DTOs.Activities.ActivityItemType type = Bloomdo.Shared.DTOs.Activities.ActivityItemType.Checkbox,
        int? targetCount = null)
    {
        return new ActivityItem
        {
            Id = Guid.NewGuid(),
            ActivityGroupId = groupId,
            Title = title,
            TaskType = (int)type,
            TargetCount = targetCount,
            IsActive = true,
            Icon = "✨",
            Color = "#7E57C2"
        };
    }

    public static ActivityCompletion BuildCompletion(
        Guid accountId,
        Guid itemId,
        DateOnly date,
        int? countValue = null)
    {
        return new ActivityCompletion
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            ActivityItemId = itemId,
            Date = date,
            CompletedAtUtc = DateTime.UtcNow,
            CountValue = countValue
        };
    }

    public static DailySnapshot BuildSnapshot(
        Guid accountId,
        DateOnly date,
        int screenSeconds = 3600,
        int pickups = 50,
        bool goalMet = true)
    {
        return new DailySnapshot
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Date = date,
            TotalScreenTimeSeconds = screenSeconds,
            Pickups = pickups,
            GoalMet = goalMet
        };
    }

    public static AppUsageRecord BuildUsage(
        Guid accountId,
        DateOnly date,
        string packageName = "com.example",
        string? label = "Example",
        int seconds = 600)
    {
        return new AppUsageRecord
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Date = date,
            PackageName = packageName,
            AppLabel = label,
            ForegroundSeconds = seconds
        };
    }

    public static GroupMembership BuildMembership(
        Guid groupId,
        Guid accountId,
        GroupMemberStatus status = GroupMemberStatus.Accepted)
    {
        return new GroupMembership
        {
            Id = Guid.NewGuid(),
            ActivityGroupId = groupId,
            AccountId = accountId,
            Status = status,
            Role = GroupMemberRole.Member
        };
    }

    public static StreakFreeze BuildFreeze(Guid accountId, DateOnly date)
    {
        return new StreakFreeze
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Date = date
        };
    }

    public static Achievement BuildAchievement(string code, string title = "Title", int sortOrder = 0)
    {
        return new Achievement
        {
            Id = Guid.NewGuid(),
            Code = code,
            Title = title,
            Description = $"{code} description",
            Icon = "🏅",
            SortOrder = sortOrder
        };
    }

    public static ChatConversation BuildConversation(Guid accountId, string title = "Chat", DateTime? createdAt = null)
    {
        return new ChatConversation
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Title = title,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            Messages = []
        };
    }

    public static ChatMessage BuildMessage(Guid conversationId, string role, string content, DateTime? createdAt = null, bool deleted = false)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = role,
            Content = content,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            IsDeleted = deleted
        };
    }
}
