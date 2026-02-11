namespace Bloomdo.Client.Core.Interfaces;

/// <summary>
/// Base interface for all ViewModels that support navigation.
/// Lifecycle methods are invoked during page transitions.
/// </summary>
public interface IPage
{
    /// <summary>
    /// Called when the page becomes visible.
    /// </summary>
    void OnAppearing();

    /// <summary>
    /// Called when the page is hidden.
    /// </summary>
    void OnDisappearing();
}
