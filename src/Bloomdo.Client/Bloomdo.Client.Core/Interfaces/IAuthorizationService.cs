using Bloomdo.Client.Domain.Enums;
using Bloomdo.Client.Domain.Models;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Client.Core.Interfaces;

public interface IAuthorizationService
{
    bool IsAuthorized(Type viewModelType);
    bool HasRole(UserRole role);
    bool HasAnyRole(params UserRole[] roles);
    bool HasPermission(string permission);
    bool HasAnyPermission(params string[] permissions);
    bool MeetsPolicy(AuthorizationPolicy policy);
    AuthorizationResult CheckAccess(Type viewModelType);
}
