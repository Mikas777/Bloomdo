using Bloomdo.Client.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class ProfileViewModel : PageViewModel
{
    private readonly IAccessTokenManager _tokenManager;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _name = "Alex Johnson";

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _username = "@alex_j";

    [ObservableProperty]
    private string _initials = "AJ";

    [ObservableProperty]
    private int _streakDays = 12;

    [ObservableProperty]
    private int _tasksCompleted = 145;

    [ObservableProperty]
    private int _focusHours = 32;

    [ObservableProperty]
    private string _level = "Pro Member";

    public ProfileViewModel(IAccessTokenManager tokenManager, INavigationService navigationService)
    {
        _tokenManager = tokenManager;
        _navigationService = navigationService;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        LoadUserProfile();
    }

    private void LoadUserProfile()
    {
        var user = _tokenManager.CurrentUser;
        if (user != null)
        {
            Name = $"{user.FirstName} {user.LastName}".Trim();
            Email = user.Email;
            
            if (!string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName))
            {
                Initials = $"{user.FirstName[0]}{user.LastName[0]}".ToUpper();
            }
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _tokenManager.LogoutAsync();
        _navigationService.NavigateTo<LoginViewModel>();
    }
}
