namespace Bloomdo.Server.Infrastructure.Settings;

public class GeminiSettings : Application.Settings.IGeminiSettings
{
    public IReadOnlyList<string> ApiKeys { get; set; } = [];
}
