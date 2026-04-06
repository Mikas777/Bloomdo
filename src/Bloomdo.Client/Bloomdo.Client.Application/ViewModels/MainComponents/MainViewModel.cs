using System.Collections.ObjectModel;
using Bloomdo.Client.Application.ViewModels.Items;
using Bloomdo.Client.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class MainViewModel : PageViewModel
{
    private readonly ISocialApiService _socialApiService;
    private readonly ISignalRClientService _signalR;

    [ObservableProperty]
    private PageViewModel _currentPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHomeSelected))]
    [NotifyPropertyChangedFor(nameof(IsSocialSelected))]
    [NotifyPropertyChangedFor(nameof(IsBlocksSelected))]
    [NotifyPropertyChangedFor(nameof(IsStatsSelected))]
    [NotifyPropertyChangedFor(nameof(IsAiChatSelected))]
    [NotifyPropertyChangedFor(nameof(IsSubscriptionSelected))]
    [NotifyPropertyChangedFor(nameof(IsProfileSelected))]
    private int _selectedTabIndex = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnreadNotifications))]
    private int _unreadNotificationCount;

    public bool IsHomeSelected => SelectedTabIndex == 0;
    public bool IsSocialSelected => SelectedTabIndex == 1;
    public bool IsBlocksSelected => SelectedTabIndex == 2;
    public bool IsStatsSelected => SelectedTabIndex == 3;
    public bool IsAiChatSelected => SelectedTabIndex == 4;
    public bool IsSubscriptionSelected => SelectedTabIndex == 5;
    public bool IsProfileSelected => SelectedTabIndex == 6;

    public bool HasUnreadNotifications => UnreadNotificationCount > 0;

    public ObservableCollection<TabItemViewModel> Tabs { get; }

    public MainViewModel(
        HomeViewModel homeViewModel,
        SocialViewModel socialViewModel,
        BlocksViewModel blocksViewModel,
        StatsViewModel statsViewModel,
        AiChatViewModel aiChatViewModel,
        SubscriptionViewModel subscriptionViewModel,
        ProfileViewModel profileViewModel,
        ISocialApiService socialApiService,
        ISignalRClientService signalR)
    {
        _socialApiService = socialApiService;
        _signalR = signalR;

        Tabs = new ObservableCollection<TabItemViewModel>
        {
            new("Home", homeViewModel),
            new("Social", socialViewModel),
            new("Blocks", blocksViewModel),
            new("Stats", statsViewModel),
            new("AI", aiChatViewModel),
            new("Plus", subscriptionViewModel),
            new("Profile", profileViewModel)
        };

        // Sync badge count from ProfileViewModel (updates after returning from notifications)
        profileViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ProfileViewModel.UnreadNotificationCount))
                UnreadNotificationCount = profileViewModel.UnreadNotificationCount;
        };

        _currentPage = homeViewModel;
        _currentPage.OnAppearing();
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        if (value >= 0 && value < Tabs.Count)
        {
            var oldPage = CurrentPage;
            oldPage?.OnDisappearing();

            CurrentPage = Tabs[value].Content;
            CurrentPage?.OnAppearing();

            // Refresh count when switching to profile tab
            if (value == 6)
                _ = LoadUnreadCountAsync();
        }
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        SubscribeSignalR();
        _ = LoadUnreadCountAsync();
        CurrentPage?.OnAppearing();
    }

    public override void OnDisappearing()
    {
        base.OnDisappearing();
        UnsubscribeSignalR();
    }

    private async Task LoadUnreadCountAsync()
    {
        try
        {
            var notifications = await _socialApiService.GetNotificationsAsync();
            UnreadNotificationCount = notifications.Count(n => !n.IsRead);
        }
        catch
        {
            // Ignore errors — badge is non-critical
        }
    }

    private void SubscribeSignalR()
    {
        _signalR.GroupInviteReceived += OnNotificationReceived;
        _signalR.NewFollowerReceived += OnFollowerReceived;
    }

    private void UnsubscribeSignalR()
    {
        _signalR.GroupInviteReceived -= OnNotificationReceived;
        _signalR.NewFollowerReceived -= OnFollowerReceived;
    }

    private void OnNotificationReceived(Bloomdo.Shared.DTOs.Social.SharedGroupDto _, Bloomdo.Shared.DTOs.Friends.ProfileSummaryDto __)
    {
        UnreadNotificationCount++;
    }

    private void OnFollowerReceived(Bloomdo.Shared.DTOs.Friends.ProfileSummaryDto _)
    {
        UnreadNotificationCount++;
    }

    [RelayCommand]
    private void SelectTab(string? indexStr)
    {
        if (int.TryParse(indexStr, out var index))
            SelectedTabIndex = index;
    }
}
