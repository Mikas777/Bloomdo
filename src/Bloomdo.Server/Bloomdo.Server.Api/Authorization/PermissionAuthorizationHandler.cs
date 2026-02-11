using Bloomdo.Shared.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Bloomdo.Server.Api.Authorization;

/// <summary>
/// Evaluates whether the current user has the required permission claim in their JWT.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissions = context.User
            .FindAll(AppClaimTypes.Permission)
            .Select(c => c.Value);

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
