using Avalonia.Controls;
using Avalonia.Input.Platform;
using Bloomdo.Client.Application.ViewModels.MainComponents;

namespace Bloomdo.Client.UI.MainComponents;

public partial class AiChatView : UserControl
{
    public AiChatView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is AiChatViewModel vm)
        {
            vm.CopyToClipboardFunc = async text =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard is { } clipboard)
                    await clipboard.SetTextAsync(text);
            };
        }
    }
}
