using Bloomdo.Client.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class AccessDeniedViewModel : PageViewModel
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _message = "You do not have access to this page";

    [ObservableProperty]
    private string _icon = "🔒";

    public AccessDeniedViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public void SetMessage(string message)
    {
        this.Message = message;
    }

    [RelayCommand]
    private void GoBack()
    {
        // Navigate back to the main page
    }
}
