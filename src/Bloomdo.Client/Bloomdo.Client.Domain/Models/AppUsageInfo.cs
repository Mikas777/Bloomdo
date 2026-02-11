namespace Bloomdo.Client.Domain.Models;

/// <summary>
/// Domain model representing application usage information
/// </summary>
public sealed class AppUsageInfo
{
    public string PackageName { get; init; } = string.Empty;
    public string? AppLabel { get; init; }
    public TimeSpan ForegroundTime { get; init; }
}
