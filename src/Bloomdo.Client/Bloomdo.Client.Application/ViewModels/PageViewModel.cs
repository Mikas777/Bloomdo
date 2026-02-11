using Bloomdo.Client.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels;

/// <summary>
/// Base class for all application ViewModels.
/// Implements <see cref="IPage"/> to support navigation lifecycle methods.
/// </summary>
public class PageViewModel : ObservableObject, IPage
{
    /// <summary>
    /// Called when the page becomes visible.
    /// Override to initialize data or subscribe to events.
    /// </summary>
    public virtual void OnAppearing() { }

    /// <summary>
    /// Called when the page is hidden.
    /// Override to release resources or unsubscribe from events.
    /// </summary>
    public virtual void OnDisappearing() { }
}