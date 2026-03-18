using Bloomdo.Client.Application.ViewModels.MainComponents;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
    private readonly INavigationService _navigationService;
    private readonly IAccessTokenManager _tokenManager;
    private readonly IProfileApiService _profileApiService;

    [ObservableProperty] private int _selectedTab;
    [ObservableProperty] private string _firstName = string.Empty;
    [ObservableProperty] private string _lastName = string.Empty;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _bio = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isSaving;

    public SettingsViewModel(
        INavigationService navigationService,
        IAccessTokenManager tokenManager,
        IProfileApiService profileApiService)
    {
        _navigationService = navigationService;
        _tokenManager = tokenManager;
        _profileApiService = profileApiService;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        Initialize();
    }

    private void Initialize()
    {
        var user = _tokenManager.CurrentUser;
        if (user != null)
        {
            FirstName = user.FirstName ?? string.Empty;
            LastName = user.LastName ?? string.Empty;
            Username = user.Username ?? string.Empty;
            Bio = user.Bio ?? string.Empty;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsSaving) return;

        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            ErrorMessage = "First name is required.";
            return;
        }

        IsSaving = true;

        try
        {
            var request = new UpdateProfileRequest
            {
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                Username = Username.Trim(),
                Bio = Bio.Trim()
            };

            var result = await _profileApiService.UpdateProfileAsync(request);

            if (result != null)
            {
                _navigationService.NavigateTo<MainViewModel>(vm => vm.SelectedTabIndex = 3);
            }
            else
            {
                ErrorMessage = "Failed to update profile. Please try again.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Save settings failed: {ex.Message}");
            ErrorMessage = "An error occurred while saving.";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.NavigateTo<MainViewModel>(vm => vm.SelectedTabIndex = 3);
    }
}
