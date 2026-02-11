namespace Bloomdo.Server.Application.Settings;

public interface IAuthSettings
{
    int RefreshTokenExpirationDays { get; }
}
