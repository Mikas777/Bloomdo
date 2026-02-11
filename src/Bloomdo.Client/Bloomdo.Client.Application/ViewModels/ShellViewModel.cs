using System.Diagnostics;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Application.ViewModels.MainComponents;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly IAccessTokenManager _tokenManager;
    private readonly Func<INavigationService> _navigationServiceFactory;
    private INavigationService? _navigationService;

    [ObservableProperty]
    private IPage _currentViewModel = null!;

    public ShellViewModel(IAccessTokenManager tokenManager, Func<INavigationService> navigationServiceFactory)
    {
        _tokenManager = tokenManager;
        _navigationServiceFactory = navigationServiceFactory;
    }

    public async Task InitializeAsync()
    {
        Debug.WriteLine("ShellViewModel.InitializeAsync started");

        // Lazy initialization of NavigationService to avoid circular dependency
        _navigationService ??= _navigationServiceFactory();

        await _tokenManager.InitializeAsync();

        if (_tokenManager.IsAuthenticated)
        {
            Debug.WriteLine("User is authenticated, navigating to MainViewModel");
            _navigationService.NavigateTo<MainViewModel>();
        }
        else
        {
            Debug.WriteLine("User is not authenticated, navigating to LoginViewModel");
            _navigationService.NavigateTo<LoginViewModel>();
        }
    }

    public void SetViewModel(IPage viewModel)
    {
        Debug.WriteLine($"ShellViewModel.SetViewModel called with {viewModel?.GetType().Name ?? "null"}");
        CurrentViewModel?.OnDisappearing();
        CurrentViewModel = viewModel;
        CurrentViewModel.OnAppearing();
        Debug.WriteLine($"CurrentViewModel is now {CurrentViewModel?.GetType().Name ?? "null"}");
    }
}

