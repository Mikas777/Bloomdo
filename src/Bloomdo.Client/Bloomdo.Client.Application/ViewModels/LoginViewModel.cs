using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Application.ViewModels.MainComponents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class LoginViewModel : PageViewModel
{
    private readonly IAccessTokenManager _tokenManager;
    private readonly INavigationService _navigationService;
    private readonly IToastService _toastService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isLoading;

    public LoginViewModel(
        IAccessTokenManager tokenManager, 
        INavigationService navigationService,
        IToastService toastService)
    {
        _tokenManager = tokenManager;
        _navigationService = navigationService;
        _toastService = toastService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please fill in all fields";
            _toastService.ShowWarning("Validation", "Please fill in all fields");
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var success = await _tokenManager.LoginAsync(Email, Password);

            if (success)
            {
                _toastService.ShowSuccess("Welcome back!");
                _navigationService.NavigateTo<MainViewModel>();
            }
            else
            {
                ErrorMessage = "Invalid email or password";
                _toastService.ShowError("Login Failed", "Invalid email or password");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
            _toastService.ShowError("Error", $"Login failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void NavigateToRegister()
    {
        _navigationService.NavigateTo<RegisterViewModel>();
    }
}

