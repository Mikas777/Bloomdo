namespace Bloomdo.Client.Domain.Enums;

public enum AuthorizationPolicy
{
    None,
    RequireAuthentication,
    RequirePremium,
    RequireAdmin,
    RequireModerator
}
