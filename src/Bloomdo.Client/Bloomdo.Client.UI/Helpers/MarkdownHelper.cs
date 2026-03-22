using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Bloomdo.Client.UI.Helpers;

public static class MarkdownHelper
{
    public static readonly AttachedProperty<string?> MarkdownTextProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, string?>("MarkdownText", typeof(MarkdownHelper));

    static MarkdownHelper()
    {
        MarkdownTextProperty.Changed.AddClassHandler<TextBlock>(OnMarkdownTextChanged);
    }

    public static string? GetMarkdownText(TextBlock element) => element.GetValue(MarkdownTextProperty);
    public static void SetMarkdownText(TextBlock element, string? value) => element.SetValue(MarkdownTextProperty, value);

    private static void OnMarkdownTextChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        textBlock.Inlines?.Clear();

        if (e.NewValue is not string text || string.IsNullOrEmpty(text))
            return;

        textBlock.Inlines ??= [];

        var i = 0;
        while (i < text.Length)
        {
            var boldStart = text.IndexOf("**", i, StringComparison.Ordinal);
            if (boldStart == -1)
            {
                textBlock.Inlines.Add(new Run(text[i..]));
                break;
            }

            if (boldStart > i)
                textBlock.Inlines.Add(new Run(text[i..boldStart]));

            var boldEnd = text.IndexOf("**", boldStart + 2, StringComparison.Ordinal);
            if (boldEnd == -1)
            {
                textBlock.Inlines.Add(new Run(text[boldStart..]));
                break;
            }

            textBlock.Inlines.Add(new Run(text[(boldStart + 2)..boldEnd]) { FontWeight = FontWeight.Bold });
            i = boldEnd + 2;
        }
    }
}
