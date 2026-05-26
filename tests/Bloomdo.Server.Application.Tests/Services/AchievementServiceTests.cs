using System.Linq.Expressions;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Application.Services;
using Bloomdo.Server.Domain.Entities;

namespace Bloomdo.Server.Application.Tests.Services;

public class AchievementServiceTests
{
    private readonly Mock<IRepository<Achievement>> _achievementRepo = new();
    private readonly Mock<IRepository<AccountAchievement>> _userAchievementRepo = new();
    private readonly Mock<IStatsRepository> _statsRepo = new();

    private AchievementService BuildSut() => new(_achievementRepo.Object, _userAchievementRepo.Object, _statsRepo.Object);

    private static List<Achievement> SeedAchievements() =>
    [
        TestData.BuildAchievement("streak_3", "Getting Started", 1),
        TestData.BuildAchievement("streak_7", "Week Warrior", 2),
        TestData.BuildAchievement("streak_14", "Two Weeks Strong", 3),
        TestData.BuildAchievement("streak_30", "Monthly Master", 4),
        TestData.BuildAchievement("streak_100", "Century Club", 5),
    ];

    [Fact]
    public async Task Evaluate_NoGoalDays_NoOp()
    {
        var accountId = Guid.NewGuid();
        _statsRepo.Setup(r => r.GetGoalMetDatesAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await BuildSut().EvaluateAchievementsAsync(accountId);

        _userAchievementRepo.Verify(r => r.AddAsync(It.IsAny<AccountAchievement>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Evaluate_3DayStreak_Unlocks_streak_3_Only()
    {
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var achievements = SeedAchievements();

        // Provide goal-met dates in descending order (matches real repo behavior)
        _statsRepo.Setup(r => r.GetGoalMetDatesAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DateOnly> { today, today.AddDays(-1), today.AddDays(-2) });
        _achievementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(achievements);
        _userAchievementRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AccountAchievement, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var added = new List<AccountAchievement>();
        _userAchievementRepo.Setup(r => r.AddAsync(It.IsAny<AccountAchievement>(), It.IsAny<CancellationToken>()))
            .Callback<AccountAchievement, CancellationToken>((a, _) => added.Add(a))
            .ReturnsAsync((AccountAchievement a, CancellationToken _) => a);

        await BuildSut().EvaluateAchievementsAsync(accountId);

        added.Count.ShouldBe(1);
        added[0].AchievementId.ShouldBe(achievements.First(a => a.Code == "streak_3").Id);
    }

    [Fact]
    public async Task Evaluate_7DayStreak_Unlocks_streak_3_and_streak_7()
    {
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var achievements = SeedAchievements();
        var goalDates = Enumerable.Range(0, 7).Select(i => today.AddDays(-i)).ToList();

        _statsRepo.Setup(r => r.GetGoalMetDatesAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(goalDates);
        _achievementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(achievements);
        _userAchievementRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AccountAchievement, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var added = new List<AccountAchievement>();
        _userAchievementRepo.Setup(r => r.AddAsync(It.IsAny<AccountAchievement>(), It.IsAny<CancellationToken>()))
            .Callback<AccountAchievement, CancellationToken>((a, _) => added.Add(a))
            .ReturnsAsync((AccountAchievement a, CancellationToken _) => a);

        await BuildSut().EvaluateAchievementsAsync(accountId);

        added.Count.ShouldBe(2);
        added.Select(a => a.AchievementId).ShouldContain(achievements.First(a => a.Code == "streak_3").Id);
        added.Select(a => a.AchievementId).ShouldContain(achievements.First(a => a.Code == "streak_7").Id);
    }

    [Fact]
    public async Task Evaluate_DoesNotUnlockAlreadyUnlocked()
    {
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var achievements = SeedAchievements();
        var streak3 = achievements.First(a => a.Code == "streak_3");

        _statsRepo.Setup(r => r.GetGoalMetDatesAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DateOnly> { today, today.AddDays(-1), today.AddDays(-2) });
        _achievementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(achievements);
        _userAchievementRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AccountAchievement, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AccountAchievement { AccountId = accountId, AchievementId = streak3.Id, UnlockedDate = today.AddDays(-1) }]);

        await BuildSut().EvaluateAchievementsAsync(accountId);

        _userAchievementRepo.Verify(r => r.AddAsync(It.IsAny<AccountAchievement>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAchievements_MarksUnlockedFlagPerAchievement()
    {
        var accountId = Guid.NewGuid();
        var achievements = SeedAchievements();
        var streak3 = achievements.First(a => a.Code == "streak_3");
        _achievementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(achievements);
        _userAchievementRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AccountAchievement, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AccountAchievement { AccountId = accountId, AchievementId = streak3.Id, UnlockedDate = new DateOnly(2026, 1, 1) }]);

        var result = await BuildSut().GetAchievementsAsync(accountId);

        result.Count.ShouldBe(5);
        result.Single(a => a.Code == "streak_3").IsUnlocked.ShouldBeTrue();
        result.Where(a => a.Code != "streak_3").ShouldAllBe(a => !a.IsUnlocked);
    }
}
