using Bloomdo.Client.Domain.Enums;

namespace Bloomdo.Client.Core.Interfaces;

public interface IToastService
{
    void Show(string title, string? content = null, ToastType type = ToastType.Info, double delaySeconds = 5);
    void ShowSuccess(string title, string? content = null);
    void ShowError(string title, string? content = null);
    void ShowWarning(string title, string? content = null);
    void ShowInfo(string title, string? content = null);
    void DismissAll();
}
