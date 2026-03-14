using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels.Items;

public partial class ActivityTaskItemViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private int? _durationMinutes;

    [ObservableProperty]
    private string _icon = "✨";

    [ObservableProperty]
    private string _color = "#7E57C2";

    [ObservableProperty]
    private int _currentStreak;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private DateTime? _completedAtUtc;

    [ObservableProperty]
    private bool _isToggling;

    public string DurationLabel =>
        DurationMinutes.HasValue ? $"{DurationMinutes} min" : string.Empty;

    public bool HasDuration => DurationMinutes.HasValue;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool HasStreak => CurrentStreak > 0;
    public string StreakText => $"🔥{CurrentStreak}";

    public string Subtitle
    {
        get
        {
            var parts = new List<string> { "Every day" };
            if (DurationMinutes.HasValue)
                parts.Add($"{DurationMinutes} minutes");
            if (!string.IsNullOrWhiteSpace(Description))
                parts.Add(Description);
            return string.Join(", ", parts);
        }
    }
}
