using System.Globalization;
using System.Text.Json;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Bloomdo.Client.Application.Helpers;
using Bloomdo.Shared.DTOs.Profile;

namespace Bloomdo.Client.UI.Converters;

/// <summary>
/// Converts an AvatarJson string to a <see cref="SolidColorBrush"/> based on the
/// <c>ConverterParameter</c> (Background, Skin, Hair, Clothing, Eye).
/// Returns a default purple brush when the JSON is null/empty or cannot be parsed.
/// </summary>
public class AvatarJsonToBrushConverter : IValueConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string json || string.IsNullOrEmpty(json))
            return new SolidColorBrush(Color.Parse("#7E57C2"));

        try
        {
            var avatar = JsonSerializer.Deserialize<AvatarConfig>(json, JsonOptions);
            if (avatar == null)
                return new SolidColorBrush(Color.Parse("#7E57C2"));

            var hex = parameter?.ToString() switch
            {
                "Background" => AvatarColorHelper.GetBackgroundColor(avatar.BackgroundColor),
                "Skin" => AvatarColorHelper.GetSkinColor(avatar.SkinTone),
                "Hair" => AvatarColorHelper.GetHairColor(avatar.HairColor),
                "Clothing" => AvatarColorHelper.GetClothingColor(avatar.ClothingColor),
                "Eye" => AvatarColorHelper.GetEyeColor(avatar.EyeColor),
                _ => AvatarColorHelper.GetBackgroundColor(avatar.BackgroundColor)
            };

            return Color.TryParse(hex, out var color)
                ? new SolidColorBrush(color)
                : new SolidColorBrush(Color.Parse("#7E57C2"));
        }
        catch
        {
            return new SolidColorBrush(Color.Parse("#7E57C2"));
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
