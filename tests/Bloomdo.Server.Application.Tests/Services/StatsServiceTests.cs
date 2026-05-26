using System.Linq.Expressions;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Application.Services;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.DTOs.Activities;
using Bloomdo.Shared.DTOs.Stats;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Application.Tests.Services;

public class StatsServiceTests
{
    private readonly Mock<IStatsRepository> _statsRepo = new();
    private readonly Mock<IRepository<ActivityGroup>> _groupRepo = new();
    private readonly Mock<IRepository<ActivityItem>> _itemRepo = new();
    private readonly Mock<IRepository<ActivityCompletion>> _completionRepo = new();
    private readonly Mock<IRepository<GroupMembership>> _membershipRepo = new();
    private readonly Mock<ISubscriptionService> _subService = new();
    private readonly Mock<IRepository<StreakFreeze>> _freezeRepo = new();
    private readonly FakeFreeLimitsSettings _free = new() { MonthlyStreakFreezes = 2 };

    private StatsService BuildSut()
    {
        _subService.Setup(s => s.IsPremiumAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _freezeRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<StreakFreeze, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _membershipRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<GroupMembership, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        return new StatsService(_statsRepo.Object, _groupRepo.Object, _itemRepo.Object,
            _completionRepo.Object, _membershipRepo.Object, _subService.Object, _freezeRepo.Object, _free);
    }

    [Fact]
    public async Task SyncUsage_NewSnapshot_Creates()
    {
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new SyncUsageRequest
        {
            Date = today,
            Pickups = 25,
            Apps = [new AppUsageEntry { PackageName = "com.tg", AppLabel = "Telegram", ForegroundSeconds = 600 }]
        };
        _statsRepo.Setup(r => r.GetUsageRecordAsync(accountId, today, "com.tg", It.IsAny<CancellationToken>())).ReturnsAsync((AppUsageRecord?)null);
        _statsRepo.Setup(r => r.GetSnapshotAsync(accountId, today, It.IsAny<CancellationToken>())).ReturnsAsync((DailySnapshot?)null);
        _groupRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        DailySnapshot? created = null;
        _statsRepo.Setup(r => r.AddSnapshotAsync(It.IsAny<DailySnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<DailySnapshot, CancellationToken>((s, _) => created = s).Returns(Task.CompletedTask);

        await BuildSut().SyncUsageAsync(accountId, request);

        _statsRepo.Verify(r => r.AddUsageRecordAsync(It.IsAny<AppUsageRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        created.ShouldNotBeNull();
        created!.TotalScreenTimeSeconds.ShouldBe(600);
        created.Pickups.ShouldBe(25);
        created.GoalMet.ShouldBeFalse(); // no groups
    }

    [Fact]
    public async Task SyncUsage_ExistingSnapshot_Updates()
    {
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var snapshot = TestData.BuildSnapshot(accountId, today, screenSeconds: 0, pickups: 0);
        var existingRecord = TestData.BuildUsage(accountId, today, "com.x", "X", 100);

        _statsRepo.Setup(r => r.GetUsageRecordAsync(accountId, today, "com.x", It.IsAny<CancellationToken>())).ReturnsAsync(existingRecord);
        _statsRepo.Setup(r => r.GetSnapshotAsync(accountId, today, It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);
        _groupRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await BuildSut().SyncUsageAsync(accountId, new SyncUsageRequest
        {
            Date = today, Pickups = 7,
            Apps = [new AppUsageEntry { PackageName = "com.x", ForegroundSeconds = 1234 }]
        });

        existingRecord.ForegroundSeconds.ShouldBe(1234);
        snapshot.TotalScreenTimeSeconds.ShouldBe(1234);
        snapshot.Pickups.ShouldBe(7);
        _statsRepo.Verify(r => r.AddUsageRecordAsync(It.IsAny<AppUsageRecord>(), It.IsAny<CancellationToken>()), Times.Never);
        _statsRepo.Verify(r => r.AddSnapshotAsync(It.IsAny<DailySnapshot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecalculateGoalMet_AllTasksCompleted_True()
    {
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var group = TestData.BuildGroup(accountId);
        var item = TestData.BuildItem(group.Id);
        var snapshot = TestData.BuildSnapshot(accountId, today, goalMet: false);

        _groupRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([group]);
        _itemRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityItem, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([item]);
        _completionRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityCompletion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.BuildCompletion(accountId, item.Id, today)]);
        _statsRepo.Setup(r => r.GetSnapshotAsync(accountId, today, It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        await BuildSut().RecalculateGoalMetAsync(accountId, today);

        snapshot.GoalMet.ShouldBeTrue();
    }

    [Fact]
    public async Task RecalculateGoalMet_NoGroups_False()
    {
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var snapshot = TestData.BuildSnapshot(accountId, today, goalMet: true);
        _groupRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _statsRepo.Setup(r => r.GetSnapshotAsync(accountId, today, It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        await BuildSut().RecalculateGoalMetAsync(accountId, today);

        snapshot.GoalMet.ShouldBeFalse();
    }

    [Fact]
    public async Task RecalculateGoalMet_CountTaskBelowTarget_False()
    {
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var group = TestData.BuildGroup(accountId);
        var item = TestData.BuildItem(group.Id, type: ActivityItemType.Count, targetCount: 10);
        var snapshot = TestData.BuildSnapshot(accountId, today, goalMet: true);

        _groupRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([group]);
        _itemRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityItem, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([item]);
        _completionRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityCompletion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.BuildCompletion(accountId, item.Id, today, countValue: 5)]);
        _statsRepo.Setup(r => r.GetSnapshotAsync(accountId, today, It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        await BuildSut().RecalculateGoalMetAsync(accountId, today);

        snapshot.GoalMet.ShouldBeFalse();
    }

    [Fact]
    public async Task RecalculateGoalMet_CountTaskAtTarget_True()
    {
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var group = TestData.BuildGroup(accountId);
        var item = TestData.BuildItem(group.Id, type: ActivityItemType.Count, targetCount: 10);
        var snapshot = TestData.BuildSnapshot(accountId, today, goalMet: false);

        _groupRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([group]);
        _itemRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityItem, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([item]);
        _completionRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityCompletion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.BuildCompletion(accountId, item.Id, today, countValue: 10)]);
        _statsRepo.Setup(r => r.GetSnapshotAsync(accountId, today, It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        await BuildSut().RecalculateGoalMetAsync(accountId, today);

        snapshot.GoalMet.ShouldBeTrue();
    }

    [Fact]
    public async Task GetWeeklyStats_NoData_ReturnsEmptyDailyData()
    {
        var accountId = Guid.NewGuid();
        var weekStart = new DateOnly(2026, 5, 18); // Monday
        _statsRepo.Setup(r => r.GetSnapshotsForMonthAsync(accountId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _statsRepo.Setup(r => r.GetUsageRecordsForRangeAsync(accountId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var stats = await BuildSut().GetWeeklyStatsAsync(accountId, weekStart);

        stats.ShouldNotBeNull();
        stats!.DailyData.Count.ShouldBe(7);
        stats.TotalScreenTimeSeconds.ShouldBe(0);
        stats.AverageScreenTimeSeconds.ShouldBe(0);
        stats.Comparison.ShouldBeNull();
        stats.TopApps.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetWeeklyStats_WithPreviousWeek_ComputesComparison()
    {
        var accountId = Guid.NewGuid();
        var weekStart = new DateOnly(2026, 5, 18);

        _statsRepo.Setup(r => r.GetSnapshotsForMonthAsync(accountId, weekStart, weekStart.AddDays(6), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailySnapshot>
            {
                TestData.BuildSnapshot(accountId, weekStart, screenSeconds: 3600, pickups: 30, goalMet: true),
                TestData.BuildSnapshot(accountId, weekStart.AddDays(1), screenSeconds: 1800, pickups: 10, goalMet: true)
            });
        _statsRepo.Setup(r => r.GetSnapshotsForMonthAsync(accountId, weekStart.AddDays(-7), weekStart.AddDays(-1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailySnapshot>
            {
                TestData.BuildSnapshot(accountId, weekStart.AddDays(-7), screenSeconds: 10000, pickups: 100, goalMet: true)
            });
        _statsRepo.Setup(r => r.GetUsageRecordsForRangeAsync(accountId, weekStart, weekStart.AddDays(6), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var stats = await BuildSut().GetWeeklyStatsAsync(accountId, weekStart);

        stats!.TotalScreenTimeSeconds.ShouldBe(5400);
        stats.Comparison.ShouldNotBeNull();
        stats.Comparison!.IsImproving.ShouldBeTrue(); // 5400 < 10000
        stats.Comparison.ScreenTimeChangeSeconds.ShouldBe(-4600);
    }

    [Fact]
    public async Task GetMonthCalendar_ComputesStreaks()
    {
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = new DateOnly(today.Year, today.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var goalDates = new List<DateOnly>
        {
            today, today.AddDays(-1), today.AddDays(-2), // streak 3
            today.AddDays(-10), today.AddDays(-11) // old streak 2
        };

        _statsRepo.Setup(r => r.GetSnapshotsForMonthAsync(accountId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goalDates.Select(d => TestData.BuildSnapshot(accountId, d, goalMet: true)).ToList());
        _statsRepo.Setup(r => r.GetGoalMetDatesAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(goalDates);

        var result = await BuildSut().GetMonthCalendarAsync(accountId, today.Year, today.Month);

        result.CurrentStreak.ShouldBe(3);
        result.LongestStreak.ShouldBe(3);
        result.Days.Count.ShouldBe(5);
    }

    [Fact]
    public async Task GetMonthCalendar_FreezeDayMergesIntoStreak()
    {
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = new DateOnly(today.Year, today.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var goalDates = new List<DateOnly> { today, today.AddDays(-2) }; // gap on -1
        var freezeOnGap = TestData.BuildFreeze(accountId, today.AddDays(-1));

        _statsRepo.Setup(r => r.GetSnapshotsForMonthAsync(accountId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goalDates.Select(d => TestData.BuildSnapshot(accountId, d, goalMet: true)).ToList());
        _statsRepo.Setup(r => r.GetGoalMetDatesAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(goalDates);

        var sut = BuildSut();
        // Override the default empty-list freeze setup from BuildSut. Moq's "last-setup-wins" rule
        // means this MUST come after BuildSut, not before.
        _freezeRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<StreakFreeze, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([freezeOnGap]);

        var result = await sut.GetMonthCalendarAsync(accountId, today.Year, today.Month);

        // Streak should bridge the gap via freeze: today, -1 (frozen), -2 = 3 days
        result.CurrentStreak.ShouldBe(3);
        result.Days.ShouldContain(d => d.Date == today.AddDays(-1) && d.IsFreezeDay);
    }
}
