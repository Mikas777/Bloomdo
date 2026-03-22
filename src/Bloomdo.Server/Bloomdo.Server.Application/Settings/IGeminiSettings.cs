namespace Bloomdo.Server.Application.Settings;

public interface IGeminiSettings
{
    IReadOnlyList<string> ApiKeys { get; }
}
