namespace Bloomdo.Shared.Constants;

/// <summary>
/// Defines all granular permission constants used across the application.
/// Permissions are stored in DB mapped to roles via the RolePermission table.
/// </summary>
public static class Permissions
{
    // Profile
    public const string ProfileView = "profile:view";
    public const string ProfileEdit = "profile:edit";

    // Goals
    public const string GoalsCreate = "goals:create";
    public const string GoalsEdit = "goals:edit";
    public const string GoalsDelete = "goals:delete";

    // Blocks
    public const string BlocksManage = "blocks:manage";

    // Activities
    public const string ActivitiesManage = "activities:manage";

    // Stats
    public const string StatsView = "stats:view";
    public const string StatsExport = "stats:export";

    // Premium features
    public const string PremiumAccess = "premium:access";

    // User management (moderator/admin)
    public const string UsersView = "users:view";
    public const string UsersManage = "users:manage";

    // Admin-only
    public const string RolesManage = "roles:manage";
    public const string SettingsManage = "settings:manage";
    public const string AnalyticsView = "analytics:view";

    /// <summary>
    /// All known permissions for validation purposes.
    /// </summary>
    public static readonly IReadOnlyList<string> All =
    [
        ProfileView, ProfileEdit,
        GoalsCreate, GoalsEdit, GoalsDelete,
        BlocksManage, ActivitiesManage,
        StatsView, StatsExport,
        PremiumAccess,
        UsersView, UsersManage,
        RolesManage, SettingsManage, AnalyticsView
    ];
}
