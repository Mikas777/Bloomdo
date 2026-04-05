namespace Bloomdo.Client.Application.Helpers;

/// <summary>
/// Shared avatar color mapping helpers used by profile views.
/// </summary>
public static class AvatarColorHelper
{
    public static string GetBackgroundColor(int id) => id switch
    {
        0 => "#7E57C2",
        1 => "#42A5F5",
        2 => "#66BB6A",
        3 => "#FFA726",
        4 => "#EC407A",
        5 => "#26A69A",
        6 => "#EF5350",
        7 => "#FDD835",
        8 => "#283593",
        9 => "#FF7043",
        10 => "#80CBC4",
        11 => "#B39DDB",
        _ => "#7E57C2"
    };

    public static string GetSkinColor(int id) => id switch
    {
        0 => "#FDDBB4",
        1 => "#E8B98D",
        2 => "#D4915A",
        3 => "#B07040",
        4 => "#8B5E3C",
        5 => "#5C3A1E",
        6 => "#3A1F04",
        _ => "#FDDBB4"
    };

    public static string GetHairColor(int id) => id switch
    {
        0 => "#2C2C2C",
        1 => "#4E342E",
        2 => "#6B4226",
        3 => "#F5D76E",
        4 => "#C0392B",
        5 => "#E65100",
        6 => "#2980B9",
        7 => "#8E44AD",
        8 => "#EC407A",
        9 => "#43A047",
        10 => "#B0BEC5",
        11 => "#F5F5DC",
        12 => "#00897B",
        _ => "#2C2C2C"
    };

    public static string GetClothingColor(int id) => id switch
    {
        0 => "#66BB6A",
        1 => "#42A5F5",
        2 => "#EF5350",
        3 => "#AB47BC",
        4 => "#FFA726",
        5 => "#37474F",
        6 => "#ECEFF1",
        7 => "#EC407A",
        8 => "#26A69A",
        9 => "#FDD835",
        10 => "#1A237E",
        11 => "#880E4F",
        _ => "#66BB6A"
    };

    public static string GetEyeColor(int id) => id switch
    {
        0 => "#5D4037",
        1 => "#8D6E63",
        2 => "#4CAF50",
        3 => "#42A5F5",
        4 => "#78909C",
        5 => "#FFA000",
        6 => "#7E57C2",
        7 => "#26A69A",
        8 => "#81D4FA",
        9 => "#2E7D32",
        _ => "#5D4037"
    };

    public static string GetGlassesColor(int id) => id switch
    {
        0 => "#263238",
        1 => "#5D4037",
        2 => "#FFB300",
        3 => "#90A4AE",
        4 => "#1565C0",
        5 => "#C62828",
        6 => "#EC407A",
        7 => "#2E7D32",
        8 => "#7E57C2",
        _ => "#263238"
    };

    public static string GetFacialHairColor(int id) => id switch
    {
        0 => "#2C2C2C",
        1 => "#4E342E",
        2 => "#6B4226",
        3 => "#F5D76E",
        4 => "#C0392B",
        5 => "#E65100",
        6 => "#9E9E9E",
        7 => "#ECEFF1",
        8 => "#8D4004",
        _ => "#2C2C2C"
    };

    public static string GetHeadwearColor(int id) => id switch
    {
        0 => "#EF5350",
        1 => "#42A5F5",
        2 => "#66BB6A",
        3 => "#AB47BC",
        4 => "#FFA726",
        5 => "#37474F",
        6 => "#EC407A",
        7 => "#FDD835",
        8 => "#26A69A",
        9 => "#ECEFF1",
        _ => "#EF5350"
    };
}
