using System.Text.Json;
using Bloomdo.Client.Application.Helpers;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Profile;
using Bloomdo.Shared.DTOs.Social;
using Bloomdo.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class UserProfileViewModel : PageViewModel
{
    private readonly ISocialApiService _socialApiService;
    private readonly INavigationService _navigationService;
    private readonly IToastService _toastService;

    private Guid _userId;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _bio = string.Empty;
    [ObservableProperty] private string _initials = "?";
    [ObservableProperty] private string _joinedDateText = string.Empty;
    [ObservableProperty] private string _level = "Beginner";
    [ObservableProperty] private bool _isLoading;

    // Avatar properties
    [ObservableProperty] private bool _hasAvatar;
    [ObservableProperty] private string _avatarBackgroundHex = "#7E57C2";
    [ObservableProperty] private string _avatarSkinHex = "#FDDBB4";
    [ObservableProperty] private string _avatarHairHex = "#2C2C2C";
    [ObservableProperty] private string _avatarClothingHex = "#66BB6A";
    [ObservableProperty] private string _avatarEyeHex = "#5D4037";
    [ObservableProperty] private string _avatarGlassesHex = "#263238";
    [ObservableProperty] private string _avatarFacialHairHex = "#2C2C2C";
    [ObservableProperty] private string _avatarHeadwearHex = "#EF5350";
    [ObservableProperty] private int _avatarBodyType;
    [ObservableProperty] private int _avatarHairStyle;
    [ObservableProperty] private int _avatarEyeStyle;
    [ObservableProperty] private int _avatarClothingStyle;
    [ObservableProperty] private int _avatarGlassesStyle;
    [ObservableProperty] private int _avatarFacialHair;
    [ObservableProperty] private int _avatarHeadwearStyle;
    [ObservableProperty] private int _avatarMouthStyle;
    [ObservableProperty] private int _avatarFaceExtra;

    // Stats
    [ObservableProperty] private int _followersCount;
    [ObservableProperty] private int _followingCount;
    [ObservableProperty] private int _streakDays;
    [ObservableProperty] private int _tasksCompleted;
    [ObservableProperty] private int _focusHours;
    [ObservableProperty] private int _achievementsUnlocked;

    // Relationship & privacy state
    [ObservableProperty] private bool _isFollowing;
    [ObservableProperty] private bool _isFollower;
    [ObservableProperty] private bool _isMutual;
    [ObservableProperty] private bool _isPendingFollow;
    [ObservableProperty] private bool _canViewStats;
    [ObservableProperty] private ProfileVisibility _visibility;
    [ObservableProperty] private bool _isPremium;

    public string FollowButtonText => IsPendingFollow ? "Pending" : IsFollowing ? "Following" : "Follow";
    public bool ShowFollowButton => !IsFollowing && !IsPendingFollow;
    public bool ShowUnfollowButton => IsFollowing;
    public bool ShowPendingButton => IsPendingFollow && !IsFollowing;

    public UserProfileViewModel(
        ISocialApiService socialApiService,
        INavigationService navigationService,
        IToastService toastService)
    {
        _socialApiService = socialApiService;
        _navigationService = navigationService;
        _toastService = toastService;
    }

    public void Initialize(Guid userId)
    {
        _userId = userId;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadProfileAsync();
    }

    [RelayCommand]
    private async Task LoadProfileAsync()
    {
        IsLoading = true;
        try
        {
            var profile = await _socialApiService.GetUserProfileAsync(_userId);
            if (profile == null)
            {
                _toastService.ShowError("Could not load profile.");
                return;
            }

            // Basic info (always visible)
            var first = profile.User.FirstName ?? "";
            var last = profile.User.LastName ?? "";
            Name = $"{first} {last}".Trim();
            Username = !string.IsNullOrEmpty(profile.User.Username) ? $"@{profile.User.Username}" : string.Empty;
            Bio = profile.Bio ?? string.Empty;
            JoinedDateText = $"Joined {profile.JoinedAt:MMMM yyyy}";
            Visibility = profile.Visibility;

            if (!string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(last))
                Initials = $"{first[0]}{last[0]}".ToUpper();
            else if (!string.IsNullOrEmpty(first))
                Initials = $"{first[0]}".ToUpper();

            FollowersCount = profile.FollowersCount;
            FollowingCount = profile.FollowingCount;

            // Relationship
            IsFollowing = profile.IsFollowing;
            IsFollower = profile.IsFollower;
            IsMutual = profile.IsMutual;
            IsPendingFollow = profile.IsPendingFollow;
            CanViewStats = profile.CanViewStats;
            IsPremium = profile.IsPremium;

            OnPropertyChanged(nameof(FollowButtonText));
            OnPropertyChanged(nameof(ShowFollowButton));
            OnPropertyChanged(nameof(ShowUnfollowButton));
            OnPropertyChanged(nameof(ShowPendingButton));

            // Stats
            if (profile.CanViewStats)
            {
                StreakDays = profile.StreakDays ?? 0;
                TasksCompleted = profile.TasksCompleted ?? 0;
                FocusHours = profile.FocusHours ?? 0;
                AchievementsUnlocked = profile.AchievementsUnlocked ?? 0;
                Level = profile.Level ?? "Beginner";
            }

            // Avatar
            ApplyAvatar(profile.User.AvatarJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Load user profile failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyAvatar(string? avatarJson)
    {
        if (!string.IsNullOrEmpty(avatarJson))
        {
            try
            {
                var avatar = JsonSerializer.Deserialize<AvatarConfig>(avatarJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (avatar != null)
                {
                    HasAvatar = true;
                    AvatarBackgroundHex = AvatarColorHelper.GetBackgroundColor(avatar.BackgroundColor);
                    AvatarSkinHex = AvatarColorHelper.GetSkinColor(avatar.SkinTone);
                    AvatarHairHex = AvatarColorHelper.GetHairColor(avatar.HairColor);
                    AvatarClothingHex = AvatarColorHelper.GetClothingColor(avatar.ClothingColor);
                    AvatarEyeHex = AvatarColorHelper.GetEyeColor(avatar.EyeColor);
                    AvatarBodyType = avatar.BodyType;
                    AvatarHairStyle = avatar.HairStyle;
                    AvatarEyeStyle = avatar.EyeStyle;
                    AvatarClothingStyle = avatar.ClothingStyle;
                    AvatarGlassesStyle = avatar.GlassesStyle;
                    AvatarGlassesHex = AvatarColorHelper.GetGlassesColor(avatar.GlassesColor);
                    AvatarFacialHair = avatar.FacialHair;
                    AvatarFacialHairHex = AvatarColorHelper.GetFacialHairColor(avatar.FacialHairColor);
                    AvatarHeadwearStyle = avatar.HeadwearStyle;
                    AvatarHeadwearHex = AvatarColorHelper.GetHeadwearColor(avatar.HeadwearColor);
                    AvatarMouthStyle = avatar.MouthStyle;
                    AvatarFaceExtra = avatar.FaceExtra;
                    return;
                }
            }
            catch { /* fallback to defaults */ }
        }

        HasAvatar = false;
        AvatarBackgroundHex = "#7E57C2";
    }

    [RelayCommand]
    private async Task FollowAsync()
    {
        var result = await _socialApiService.FollowUserAsync(_userId);
        if (result)
        {
            var msg = Visibility == ProfileVisibility.Private ? "Follow request sent!" : "Now following!";
            _toastService.ShowSuccess(msg);
            await LoadProfileAsync();
        }
        else
        {
            _toastService.ShowError("Could not follow user.");
        }
    }

    [RelayCommand]
    private async Task UnfollowAsync()
    {
        var result = await _socialApiService.UnfollowUserAsync(_userId);
        if (result)
        {
            _toastService.ShowInfo($"Unfollowed {Username}");
            await LoadProfileAsync();
        }
    }

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateBack();
}
