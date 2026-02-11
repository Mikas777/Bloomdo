using Microsoft.AspNetCore.Authorization;

namespace Bloomdo.Server.Api.Authorization;

/// <summary>
/// Represents the requirement that the caller possesses a specific permission claim.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
