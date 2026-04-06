using System.Globalization;
using Avalonia.Data.Converters;
using Bloomdo.Shared.DTOs.Social;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Client.UI.Converters;

public class NotificationTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not NotificationDto notif) return "New notification";

        return notif.Type switch
        {
            NotificationType.NewFollower => $"@{notif.Actor?.Username} started following you",
            NotificationType.FollowRequest => $"@{notif.Actor?.Username} wants to follow you",
            NotificationType.GroupInvite => $"@{notif.Actor?.Username} invited you to a group",
            NotificationType.GroupTaskCompleted => $"@{notif.Actor?.Username} completed a task",
            NotificationType.GroupNewTask => "A new task was added to your group",
            NotificationType.GroupDeleted => "A group you were in was deleted",
            _ => "New notification"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class NotificationTypeEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NotificationType type && parameter is string paramStr
            && Enum.TryParse<NotificationType>(paramStr, out var expected))
        {
            return type == expected;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns true if the notification is of the given type (parameter) AND ActionResult == None (still actionable).
/// </summary>
public class NotificationIsActionableConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not NotificationDto dto || parameter is not string paramStr) return false;
        if (!Enum.TryParse<NotificationType>(paramStr, out var expectedType)) return false;
        return dto.Type == expectedType && dto.ActionResult == NotificationActionResult.None;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns true if the notification is of the given type (parameter) AND ActionResult != None (already acted).
/// </summary>
public class NotificationHasResultConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not NotificationDto dto || parameter is not string paramStr) return false;
        if (!Enum.TryParse<NotificationType>(paramStr, out var expectedType)) return false;
        return dto.Type == expectedType && dto.ActionResult != NotificationActionResult.None;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns the result label text, e.g. "✓ Joined" / "✗ Declined".
/// parameter: "GroupInvite" or "FollowRequest"
/// </summary>
public class NotificationResultLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not NotificationDto dto) return string.Empty;

        return (dto.Type, dto.ActionResult) switch
        {
            (NotificationType.GroupInvite, NotificationActionResult.Accepted) => "✓ Joined",
            (NotificationType.GroupInvite, NotificationActionResult.Declined) => "✗ Declined",
            (NotificationType.FollowRequest, NotificationActionResult.Accepted) => "✓ Accepted",
            (NotificationType.FollowRequest, NotificationActionResult.Declined) => "✗ Declined",
            _ => string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns the background color for the result badge.
/// </summary>
public class NotificationResultColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NotificationDto { ActionResult: NotificationActionResult.Accepted })
            return "#22C55E";
        return "#9CA3AF";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
