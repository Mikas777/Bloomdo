using Microsoft.AspNetCore.Authorization;

namespace Bloomdo.Server.Api.Authorization;

/// <summary>
/// Shorthand attribute for requiring a specific permission on a controller or action.
/// Maps to the dynamic "Permission:{name}" policy resolved by <see cref="PermissionPolicyProvider"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
        : base($"Permission:{permission}")
    {
    }
}
