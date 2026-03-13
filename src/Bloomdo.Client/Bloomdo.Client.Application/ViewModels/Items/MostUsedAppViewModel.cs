using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels.Items;

public partial class MostUsedAppViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Icon))]
    private string _name;

    [ObservableProperty]
    private string _duration;

    [ObservableProperty]
    private double _usagePercent;

    [ObservableProperty]
    private int _totalSeconds;

    public string Icon => Name.Length > 0 ? Name[..1] : "?";

    public MostUsedAppViewModel(string name, string duration)
    {
        _name = name;
        _duration = duration;
    }

    public MostUsedAppViewModel(string name, string duration, int totalSeconds, double usagePercent)
    {
        _name = name;
        _duration = duration;
        _totalSeconds = totalSeconds;
        _usagePercent = usagePercent;
    }
}