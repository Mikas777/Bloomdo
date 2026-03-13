using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels.Items;

public partial class DayChartBarViewModel : ObservableObject
{
    public DayOfWeek DayOfWeek { get; }
    public DateOnly Date { get; }
    public string DayLabel { get; }
    
    [ObservableProperty]
    private int _screenTimeSeconds;
    
    [ObservableProperty]
    private double _barHeightPercent;
    
    [ObservableProperty]
    private bool _isToday;
    
    [ObservableProperty]
    private bool _goalMet;
    
    [ObservableProperty]
    private int _pickups;

    public string FormattedTime
    {
        get
        {
            var ts = TimeSpan.FromSeconds(ScreenTimeSeconds);
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            return $"{ts.Minutes}m";
        }
    }

    public bool HasData => ScreenTimeSeconds > 0;

    public DayChartBarViewModel(DayOfWeek dayOfWeek, DateOnly date)
    {
        DayOfWeek = dayOfWeek;
        Date = date;
        DayLabel = dayOfWeek switch
        {
            DayOfWeek.Monday => "Mon",
            DayOfWeek.Tuesday => "Tue",
            DayOfWeek.Wednesday => "Wed",
            DayOfWeek.Thursday => "Thu",
            DayOfWeek.Friday => "Fri",
            DayOfWeek.Saturday => "Sat",
            DayOfWeek.Sunday => "Sun",
            _ => ""
        };
        IsToday = date == DateOnly.FromDateTime(DateTime.Today);
    }
    
    partial void OnScreenTimeSecondsChanged(int value)
    {
        OnPropertyChanged(nameof(FormattedTime));
        OnPropertyChanged(nameof(HasData));
    }
}
