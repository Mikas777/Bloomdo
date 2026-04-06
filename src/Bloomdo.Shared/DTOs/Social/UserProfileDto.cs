using Bloomdo.Shared.DTOs.Friends;
using Bloomdo.Shared.DTOs.Profile;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Shared.DTOs.Social;

/// <summary>
/// Full public-facing profile response returned when viewing another user's profile.
/// Data visibility is controlled by the target user's ProfileVisibility setting.
/// </summary>
public class UserProfileDto
{
    // ── Always visible ───────────────────────────────────────────
    public ProfileSummaryDto User { get; set; } = null!;
    public string? Bio { get; set; }
    public ProfileVisibility Visibility { get; set; }
    public DateTime JoinedAt { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }

    // ── Relationship status (viewer → target) ────────────────────
    public bool IsFollowing { get; set; }
    public bool IsFollower { get; set; }
    public bool IsMutual { get; set; }
    public bool IsPendingFollow { get; set; }

    /// <summary>
    /// Whether the viewer has permission to see the full stats/activity data.
    /// </summary>
    public bool CanViewStats { get; set; }

    // ── Stats (null when CanViewStats = false) ───────────────────
    public int? StreakDays { get; set; }
    public int? TasksCompleted { get; set; }
    public int? FocusHours { get; set; }
    public int? AchievementsUnlocked { get; set; }

    /// <summary>
    /// Level label derived from streak.
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// Whether the target user has an active premium subscription.
    /// </summary>
    public bool IsPremium { get; set; }
}
