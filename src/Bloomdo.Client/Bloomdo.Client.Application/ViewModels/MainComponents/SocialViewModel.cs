using System.Collections.ObjectModel;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Friends;
using Bloomdo.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class SocialViewModel : PageViewModel
{
    private readonly IFriendsApiService _friendsApiService;
    private readonly INavigationService _navigationService;
    private readonly IToastService _toastService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSearching;

    public ObservableCollection<ProfileSummaryDto> SearchResults { get; } = [];
    public ObservableCollection<FriendshipDto> Friends { get; } = [];
    public ObservableCollection<FriendshipDto> PendingRequests { get; } = [];

    public bool HasNoFriends => Friends.Count == 0;
    public bool HasPendingRequests => PendingRequests.Count > 0;

    public SocialViewModel(
        IFriendsApiService friendsApiService,
        INavigationService navigationService,
        IToastService toastService)
    {
        _friendsApiService = friendsApiService;
        _navigationService = navigationService;
        _toastService = toastService;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadSocialDataAsync();
    }

    [RelayCommand]
    private async Task LoadSocialDataAsync()
    {
        IsLoading = true;
        try
        {
            var friendships = await _friendsApiService.GetFriendsAsync();
            Friends.Clear();
            PendingRequests.Clear();

            foreach (var f in friendships)
            {
                if (f.Status == FriendshipStatus.Accepted)
                    Friends.Add(f);
                else if (f.Status == FriendshipStatus.Pending && f.IsIncomingRequest)
                    PendingRequests.Add(f);
            }

            // Notify computed properties
            OnPropertyChanged(nameof(HasNoFriends));
            OnPropertyChanged(nameof(HasPendingRequests));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults.Clear();
            IsSearching = false;
            return;
        }

        IsLoading = true;
        IsSearching = true;
        try
        {
            var results = await _friendsApiService.SearchUsersAsync(SearchQuery);
            SearchResults.Clear();
            foreach (var user in results)
                SearchResults.Add(user);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SendRequestAsync(ProfileSummaryDto? user)
    {
        if (user == null) return;

        var result = await _friendsApiService.SendFriendRequestAsync(user.Id);
        if (result)
        {
            _toastService.ShowSuccess($"Friend request sent to @{user.Username}");
            SearchQuery = string.Empty;
            SearchResults.Clear();
            IsSearching = false;
            await LoadSocialDataAsync();
        }
        else
        {
            _toastService.ShowError("Could not send request. Maybe it already exists?");
        }
    }

    [RelayCommand]
    private async Task AcceptRequestAsync(FriendshipDto? request)
    {
        if (request == null) return;

        var result = await _friendsApiService.RespondToRequestAsync(request.Id, true);
        if (result)
        {
            _toastService.ShowSuccess("Friend request accepted!");
            await LoadSocialDataAsync();
        }
    }

    [RelayCommand]
    private async Task DeclineRequestAsync(FriendshipDto? request)
    {
        if (request == null) return;

        var result = await _friendsApiService.RespondToRequestAsync(request.Id, false);
        if (result)
        {
            _toastService.ShowInfo("Friend request declined");
            await LoadSocialDataAsync();
        }
    }

    [RelayCommand]
    private async Task RemoveFriendAsync(FriendshipDto? friendship)
    {
        if (friendship == null) return;

        var result = await _friendsApiService.RemoveFriendAsync(friendship.Friend.Id);
        if (result)
        {
            _toastService.ShowInfo("Removed from friends");
            await LoadSocialDataAsync();
        }
    }
}
