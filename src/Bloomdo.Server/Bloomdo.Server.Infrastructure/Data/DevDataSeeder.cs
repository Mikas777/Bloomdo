using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bloomdo.Server.Infrastructure.Data;

/// <summary>
/// Seeds a test account with realistic statistics data for development/testing.
/// Account: test@bloomdo.dev / Test123!
/// </summary>
public static class DevDataSeeder
{
    private static readonly Guid SeedAccountId = new("d0000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(AppDbContext context, ILogger? logger = null)
    {
        if (await context.Accounts.IgnoreQueryFilters().AnyAsync(a => a.Id == SeedAccountId))
        {
            logger?.LogInformation("Seed account already exists, skipping");
            return;
        }

        logger?.LogInformation("Seeding test account and statistics...");

        var now = DateTime.UtcNow;

        // 1. Create test account
        var account = new Account
        {
            Id = SeedAccountId,
            Email = "test@bloomdo.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            FirstName = "Test",
            LastName = "User",
            IsEmailConfirmed = true,
            LastLoginAt = now,
            CreatedAt = now.AddDays(-45)
        };
        context.Accounts.Add(account);

        // 2. Assign User role
        context.AccountRoles.Add(new AccountRole
        {
            Id = Guid.NewGuid(),
            AccountId = SeedAccountId,
            RoleId = (int)UserRole.User,
            CreatedAt = now
        });

        // 3. Create a Limit block rule (≤ 60 min per app per day)
        context.BlockRules.Add(new BlockRule
        {
            Id = Guid.NewGuid(),
            AccountId = SeedAccountId,
            Title = "Social media limit",
            Type = BlockType.Limit,
            IsActive = true,
            DailyLimitMinutes = 60,
            BlockedPackagesJson = "[\"com.instagram.android\",\"com.twitter.android\",\"com.zhiliaoapp.musically\"]",
            CreatedAt = now
        });

        // 4. Seed 35 days of stats (past 5 weeks)
        var random = new Random(42); // Deterministic seed for reproducibility
        var today = DateOnly.FromDateTime(now);

        var apps = new (string Package, string Label)[]
        {
            ("com.instagram.android", "Instagram"),
            ("com.twitter.android", "Twitter"),
            ("com.whatsapp", "WhatsApp"),
            ("com.google.android.youtube", "YouTube"),
            ("com.zhiliaoapp.musically", "TikTok"),
            ("com.spotify.music", "Spotify"),
            ("com.google.android.gm", "Gmail"),
            ("org.telegram.messenger", "Telegram"),
            ("com.google.android.apps.maps", "Maps"),
            ("com.reddit.frontpage", "Reddit")
        };

        // Generate decreasing trend over weeks (user is improving)
        // Week 5 (oldest): high usage ~6h
        // Week 1 (current): lower usage ~3.5h
        var weeklyBaselines = new[] { 6.0, 5.2, 4.5, 3.8, 3.5 }; // hours per day baseline

        for (var dayOffset = 34; dayOffset >= 0; dayOffset--)
        {
            var date = today.AddDays(-dayOffset);
            var weekIndex = dayOffset / 7; // 0 = current week, 4 = oldest
            var reversedWeekIndex = 4 - weekIndex;
            if (reversedWeekIndex >= weeklyBaselines.Length)
                reversedWeekIndex = weeklyBaselines.Length - 1;

            var baselineHours = weeklyBaselines[reversedWeekIndex];

            // Add some daily variation (±30%)
            var variation = 0.7 + random.NextDouble() * 0.6;
            var totalHours = baselineHours * variation;
            var totalSeconds = (int)(totalHours * 3600);

            // Determine if goal was met (screen time < 4 hours)
            var goalMet = totalHours < 4.0;

            // Create snapshot
            var pickups = (int)(totalHours * 8 + random.Next(-5, 15));

            context.DailySnapshots.Add(new DailySnapshot
            {
                Id = Guid.NewGuid(),
                AccountId = SeedAccountId,
                Date = date,
                TotalScreenTimeSeconds = totalSeconds,
                Pickups = Math.Max(1, pickups),
                GoalMet = goalMet,
                CreatedAt = date.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc)
            });

            // Create app usage records for this day
            var remainingSeconds = totalSeconds;
            var appsForDay = apps.OrderBy(_ => random.Next()).Take(random.Next(4, 8)).ToArray();

            for (var a = 0; a < appsForDay.Length; a++)
            {
                int appSeconds;
                if (a == appsForDay.Length - 1)
                {
                    appSeconds = remainingSeconds;
                }
                else
                {
                    // First apps get more time, decreasing distribution
                    var share = (appsForDay.Length - a) / (double)appsForDay.Length;
                    appSeconds = (int)(remainingSeconds * share * (0.3 + random.NextDouble() * 0.4));
                    appSeconds = Math.Min(appSeconds, remainingSeconds);
                }

                if (appSeconds <= 0) continue;
                remainingSeconds -= appSeconds;

                context.AppUsageRecords.Add(new AppUsageRecord
                {
                    Id = Guid.NewGuid(),
                    AccountId = SeedAccountId,
                    Date = date,
                    PackageName = appsForDay[a].Package,
                    AppLabel = appsForDay[a].Label,
                    ForegroundSeconds = appSeconds,
                    CreatedAt = date.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc)
                });
            }
        }

        await context.SaveChangesAsync();
        logger?.LogInformation("Seed complete: test@bloomdo.dev / Test123! (35 days of stats)");
    }
}
