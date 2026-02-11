using Bloomdo.Client.Domain.Enums;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Client.Domain.Attributes;

/// <summary>
/// Marks a ViewModel as requiring authorization before navigation.
/// Supports role-based, permission-based, and policy-based access control.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AuthorizeAttribute : Attribute
{
    public UserRole[] Roles { get; init; }
    public string[] Permissions { get; init; }
    public AuthorizationPolicy Policy { get; init; }

    public AuthorizeAttribute(AuthorizationPolicy policy = AuthorizationPolicy.RequireAuthentication)
    {
        Policy = policy;
        Roles = [];
        Permissions = [];
    }

    public AuthorizeAttribute(params UserRole[] roles)
    {
        Roles = roles;
        Permissions = [];
        Policy = AuthorizationPolicy.None;
    }
}
