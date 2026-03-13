using System.Text.Json;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.DTOs.Stats;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Application.Services;

public class StatsService(
    IStatsRepository statsRepository,
    IRepository<BlockRule> blockRuleRepository) : IStatsService
{
    public async Task SyncUsageAsync(Guid accountId, SyncUsageRequest request, CancellationToken ct = default)
    {
        foreach (var app in request.Apps)
        {
            var existing = await statsRepository.GetUsageRecordAsync(accountId, request.Date, app.PackageName, ct);

            if (existing is not null)
            {
                existing.ForegroundSeconds = app.ForegroundSeconds;
                existing.AppLabel = app.AppLabel ?? existing.AppLabel;
            }
            else
            {
                await statsRepository.AddUsageRecordAsync(new AppUsageRecord
                {
                    AccountId = accountId,
                    Date = request.Date,
                    PackageName = app.PackageName,
                    AppLabel = app.AppLabel,
                    ForegroundSeconds = app.ForegroundSeconds
                }, ct);
            }
        }

        var totalSeconds = request.Apps.Sum(a => a.ForegroundSeconds);
        var snapshot = await statsRepository.GetSnapshotAsync(accountId, request.Date, ct);

        var goalMet = await EvaluateBlockComplianceAsync(accountId, request, ct);

        if (snapshot is not null)
        {
            snapshot.TotalScreenTimeSeconds = totalSeconds;
            snapshot.Pickups = request.Pickups;
            snapshot.GoalMet = goalMet;
        }
        else
        {
            await statsRepository.AddSnapshotAsync(new DailySnapshot
            {
                AccountId = accountId,
                Date = request.Date,
                TotalScreenTimeSeconds = totalSeconds,
                Pickups = request.Pickups,
                GoalMet = goalMet
            }, ct);
        }

        await statsRepository.SaveChangesAsync(ct);
    }

    public async Task<DailyStatsResponse?> GetDailyStatsAsync(Guid accountId, DateOnly date, CancellationToken ct = default)
    {
        var snapshot = await statsRepository.GetSnapshotAsync(accountId, date, ct);
        if (snapshot is null)
            return null;

        var records = await statsRepository.GetUsageRecordsForDateAsync(accountId, date, ct);

        return new DailyStatsResponse
        {
            Date = date,
            TotalScreenTimeSeconds = snapshot.TotalScreenTimeSeconds,
            Pickups = snapshot.Pickups,
            GoalMet = snapshot.GoalMet,
            Apps = records
                .OrderByDescending(r => r.ForegroundSeconds)
                .Select(r => new AppUsageEntry
                {
                    PackageName = r.PackageName,
                    AppLabel = r.AppLabel,
                    ForegroundSeconds = r.ForegroundSeconds
                })
                .ToList()
        };
    }

    public async Task<MonthCalendarResponse> GetMonthCalendarAsync(Guid accountId, int year, int month, CancellationToken ct = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var snapshots = await statsRepository.GetSnapshotsForMonthAsync(accountId, startDate, endDate, ct);

        var days = snapshots.Select(s => new CalendarDayDto
        {
            Date = s.Date,
            GoalMet = s.GoalMet,
            TotalScreenTimeSeconds = s.TotalScreenTimeSeconds
        }).ToList();

        var (current, longest) = await CalculateStreaksAsync(accountId, ct);

        return new MonthCalendarResponse
        {
            Year = year,
            Month = month,
            CurrentStreak = current,
            LongestStreak = longest,
            Days = days
        };
    }

    public async Task<WeeklyStatsResponse?> GetWeeklyStatsAsync(Guid accountId, DateOnly weekStartDate, CancellationToken ct = default)
    {
        var weekEndDate = weekStartDate.AddDays(6);

        var snapshots = await statsRepository.GetSnapshotsForMonthAsync(accountId, weekStartDate, weekEndDate, ct);
        var snapshotDict = snapshots.ToDictionary(s => s.Date);

        var dailyData = new List<DailyScreenTimeDto>();

        for (var i = 0; i < 7; i++)
        {
            var date = weekStartDate.AddDays(i);
            var hasData = snapshotDict.TryGetValue(date, out var snapshot);

            dailyData.Add(new DailyScreenTimeDto
            {
                Date = date,
                DayOfWeek = date.DayOfWeek,
                ScreenTimeSeconds = hasData ? snapshot!.TotalScreenTimeSeconds : 0,
                Pickups = hasData ? snapshot!.Pickups : 0,
                GoalMet = hasData && snapshot!.GoalMet
            });
        }

        var totalSeconds = dailyData.Sum(d => d.ScreenTimeSeconds);
        var daysWithData = dailyData.Count(d => d.ScreenTimeSeconds > 0);
        var avgSeconds = daysWithData > 0 ? totalSeconds / daysWithData : 0;

        var totalPickups = dailyData.Sum(d => d.Pickups);
        var avgPickups = daysWithData > 0 ? totalPickups / daysWithData : 0;

        // Get comparison with previous week
        var prevWeekStart = weekStartDate.AddDays(-7);
        var prevWeekEnd = prevWeekStart.AddDays(6);
        var prevSnapshots = await statsRepository.GetSnapshotsForMonthAsync(accountId, prevWeekStart, prevWeekEnd, ct);

        WeekComparisonDto? comparison = null;
        if (prevSnapshots.Count > 0)
        {
            var prevTotalSeconds = prevSnapshots.Sum(s => s.TotalScreenTimeSeconds);
            var prevTotalPickups = prevSnapshots.Sum(s => s.Pickups);

            var screenTimeChange = totalSeconds - prevTotalSeconds;
            var screenTimeChangePercent = prevTotalSeconds > 0 
                ? (double)screenTimeChange / prevTotalSeconds * 100 
                : 0;

            var pickupsChange = totalPickups - prevTotalPickups;
            var pickupsChangePercent = prevTotalPickups > 0 
                ? (double)pickupsChange / prevTotalPickups * 100 
                : 0;

            comparison = new WeekComparisonDto
            {
                ScreenTimeChangePercent = screenTimeChangePercent,
                ScreenTimeChangeSeconds = screenTimeChange,
                PickupsChangePercent = pickupsChangePercent,
                PickupsChange = pickupsChange,
                IsImproving = screenTimeChange < 0
            };
        }

        // Get top apps for the week
        var usageRecords = await statsRepository.GetUsageRecordsForRangeAsync(accountId, weekStartDate, weekEndDate, ct);
        var topApps = usageRecords
            .GroupBy(r => r.PackageName)
            .Select(g => new AppUsageEntry
            {
                PackageName = g.Key,
                AppLabel = g.FirstOrDefault(r => !string.IsNullOrEmpty(r.AppLabel))?.AppLabel,
                ForegroundSeconds = g.Sum(r => r.ForegroundSeconds)
            })
            .OrderByDescending(a => a.ForegroundSeconds)
            .Take(10)
            .ToList();

        return new WeeklyStatsResponse
        {
            WeekStartDate = weekStartDate,
            WeekEndDate = weekEndDate,
            DailyData = dailyData,
            TotalScreenTimeSeconds = totalSeconds,
            AverageScreenTimeSeconds = avgSeconds,
            TotalPickups = totalPickups,
            AveragePickups = avgPickups,
            Comparison = comparison,
            TopApps = topApps
        };
    }

    private async Task<(int Current, int Longest)> CalculateStreaksAsync(Guid accountId, CancellationToken ct)
    {
        var goalDays = await statsRepository.GetGoalMetDatesAsync(accountId, ct);

        if (goalDays.Count == 0)
            return (0, 0);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Current streak: count consecutive days ending at today or yesterday
        var currentStreak = 0;
        var checkDate = today;

        // If today is not in the list, start from yesterday
        if (!goalDays.Contains(today) && goalDays.Count > 0 && goalDays[0] == today.AddDays(-1))
            checkDate = today.AddDays(-1);

        foreach (var day in goalDays)
        {
            if (day == checkDate)
            {
                currentStreak++;
                checkDate = checkDate.AddDays(-1);
            }
            else if (day < checkDate)
            {
                break;
            }
        }

        // Longest streak: scan all goal days (sorted descending)
        var longestStreak = 0;
        var streak = 1;

        for (var i = 1; i < goalDays.Count; i++)
        {
            if (goalDays[i - 1].AddDays(-1) == goalDays[i])
            {
                streak++;
            }
            else
            {
                longestStreak = Math.Max(longestStreak, streak);
                streak = 1;
            }
        }
        longestStreak = Math.Max(longestStreak, streak);

        return (currentStreak, longestStreak);
    }

    /// <summary>
    /// Evaluates whether the user complied with their active block rules for the given day.
    /// GoalMet = true when the user has at least one active block rule AND
    /// all Limit-type rules are respected (blocked apps stayed under the daily limit).
    /// Schedule/Focus/Bloomdo rules are enforced client-side;
    /// the server only verifies Limit compliance from actual usage data.
    /// </summary>
    private async Task<bool> EvaluateBlockComplianceAsync(Guid accountId, SyncUsageRequest request, CancellationToken ct)
    {
        var rules = await blockRuleRepository.FindAsync(r => r.AccountId == accountId && r.IsActive, ct);
        var activeRules = rules.ToList();

        if (activeRules.Count == 0)
            return false;

        var usageLookup = request.Apps.ToDictionary(a => a.PackageName, a => a.ForegroundSeconds);

        foreach (var rule in activeRules)
        {
            if (rule.Type != BlockType.Limit || !rule.DailyLimitMinutes.HasValue)
                continue;

            var blockedPackages = JsonSerializer.Deserialize<List<string>>(rule.BlockedPackagesJson) ?? [];
            var limitSeconds = rule.DailyLimitMinutes.Value * 60;

            foreach (var pkg in blockedPackages)
            {
                if (usageLookup.TryGetValue(pkg, out var usedSeconds) && usedSeconds > limitSeconds)
                    return false;
            }
        }

        return true;
    }
}
