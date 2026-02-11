using Bloomdo.Client.Domain.Enums;

namespace Bloomdo.Client.Domain.Models;

public class AuthorizationResult
{
    public bool IsAuthorized { get; set; }
    public string? FailureReason { get; set; }
    public AuthorizationFailureType FailureType { get; set; }

    public static AuthorizationResult Success() => new() { IsAuthorized = true };

    public static AuthorizationResult Failure(string reason, AuthorizationFailureType failureType) =>
        new() { IsAuthorized = false, FailureReason = reason, FailureType = failureType };
}