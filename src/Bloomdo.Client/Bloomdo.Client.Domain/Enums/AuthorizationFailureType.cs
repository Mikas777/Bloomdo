namespace Bloomdo.Client.Domain.Enums;

public enum AuthorizationFailureType
{
    None,
    NotAuthenticated,
    InsufficientRole,
    InsufficientPermission,
    PolicyNotMet
}