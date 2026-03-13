namespace Bloomdo.Shared.DTOs.Stats;

public sealed class WeeklyStatsResponse
{
    public DateOnly WeekStartDate { get; init; }
    public DateOnly WeekEndDate { get; init; }
    
    /// <summary>
    /// Daily screen time in seconds for each day (7 items, Mon-Sun)
    /// </summary>
    public List<DailyScreenTimeDto> DailyData { get; init; } = [];
    
    /// <summary>
    /// Total screen time this week in seconds
    /// </summary>
    public int TotalScreenTimeSeconds { get; init; }
    
    /// <summary>
    /// Average daily screen time in seconds
    /// </summary>
    public int AverageScreenTimeSeconds { get; init; }
    
    /// <summary>
    /// Total pickups this week
    /// </summary>
    public int TotalPickups { get; init; }
    
    /// <summary>
    /// Average daily pickups
    /// </summary>
    public int AveragePickups { get; init; }
    
    /// <summary>
    /// Comparison with previous week
    /// </summary>
    public WeekComparisonDto? Comparison { get; init; }
    
    /// <summary>
    /// Top apps usage for the week
    /// </summary>
    public List<AppUsageEntry> TopApps { get; init; } = [];
}

public sealed class DailyScreenTimeDto
{
    public DateOnly Date { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public int ScreenTimeSeconds { get; init; }
    public int Pickups { get; init; }
    public bool GoalMet { get; init; }
}

public sealed class WeekComparisonDto
{
    /// <summary>
    /// Percentage change in screen time compared to previous week. Negative = improvement
    /// </summary>
    public double ScreenTimeChangePercent { get; init; }
    
    /// <summary>
    /// Absolute change in screen time seconds (current - previous)
    /// </summary>
    public int ScreenTimeChangeSeconds { get; init; }
    
    /// <summary>
    /// Percentage change in pickups compared to previous week. Negative = improvement
    /// </summary>
    public double PickupsChangePercent { get; init; }
    
    /// <summary>
    /// Absolute change in pickups (current - previous)
    /// </summary>
    public int PickupsChange { get; init; }
    
    /// <summary>
    /// True if user is improving (spending less time), false otherwise
    /// </summary>
    public bool IsImproving { get; init; }
}
