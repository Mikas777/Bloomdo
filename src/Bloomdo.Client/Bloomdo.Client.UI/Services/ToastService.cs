using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Domain.Enums;
using ShadUI;

namespace Bloomdo.Client.UI.Services;

public class ToastService : IToastService
{
    private readonly ToastManager _toastManager;

    public ToastService(ToastManager toastManager)
    {
        _toastManager = toastManager;
    }

    public void Show(string title, string? content = null, ToastType type = ToastType.Info, double delaySeconds = 5)
    {
        var builder = _toastManager.CreateToast(title)
            .WithDelay(delaySeconds)
            .DismissOnClick();

        if (content is not null)
            builder = builder.WithContent(content);

        builder.Show(MapNotification(type));
    }

    public void ShowSuccess(string title, string? content = null)
        => Show(title, content, ToastType.Success);

    public void ShowError(string title, string? content = null)
        => Show(title, content, ToastType.Error, 8);

    public void ShowWarning(string title, string? content = null)
        => Show(title, content, ToastType.Warning, 6);

    public void ShowInfo(string title, string? content = null)
        => Show(title, content, ToastType.Info);

    public void DismissAll()
        => _toastManager.DismissAll();

    private static Notification MapNotification(ToastType type) => type switch
    {
        ToastType.Success => Notification.Success,
        ToastType.Error => Notification.Error,
        ToastType.Warning => Notification.Warning,
        ToastType.Info => Notification.Info,
        _ => Notification.Basic
    };
}
