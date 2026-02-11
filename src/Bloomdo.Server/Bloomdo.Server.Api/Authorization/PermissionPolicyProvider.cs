using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Bloomdo.Server.Api.Authorization;

/// <summary>
/// Dynamically generates authorization policies for permission-based checks.
/// Any policy name starting with "Permission:" is resolved to a <see cref="PermissionRequirement"/>.
/// </summary>
public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private const string PolicyPrefix = "Permission:";

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[PolicyPrefix.Length..];

            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
        }

        return await base.GetPolicyAsync(policyName);
    }
}
