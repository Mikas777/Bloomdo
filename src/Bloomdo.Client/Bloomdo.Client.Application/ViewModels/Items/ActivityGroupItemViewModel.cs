using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels.Items;

public partial class ActivityGroupItemViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _icon = "📋";

    [ObservableProperty]
    private string _color = "#7E57C2";

    [ObservableProperty]
    private int _currentStreak;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editTitle = string.Empty;

    [ObservableProperty]
    private bool _isAddingItem;

    [ObservableProperty]
    private string _newItemTitle = string.Empty;

    [ObservableProperty]
    private string? _newItemDescription;

    [ObservableProperty]
    private int? _newItemDuration;

    [ObservableProperty]
    private string _newItemIcon = "✨";

    [ObservableProperty]
    private string _newItemColor = "#7E57C2";

    public ObservableCollection<ActivityTaskItemViewModel> Tasks { get; } = [];

    public int CompletedCount => Tasks.Count(t => t.IsCompleted);
    public int TotalCount => Tasks.Count;
    public string ProgressText => $"{CompletedCount}/{TotalCount}";
    public double ProgressPercent => TotalCount > 0 ? (double)CompletedCount / TotalCount * 100 : 0;
    public bool HasStreak => CurrentStreak > 0;
    public string StreakText => $"🔥 {CurrentStreak}";

    public void RefreshProgress()
    {
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(ProgressPercent));
    }

    public void ResetNewItemForm()
    {
        NewItemTitle = string.Empty;
        NewItemDescription = null;
        NewItemDuration = null;
        NewItemIcon = "✨";
        NewItemColor = "#7E57C2";
    }
}
