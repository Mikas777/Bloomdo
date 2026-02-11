namespace Bloomdo.Server.Domain.Exceptions;

public class ForbiddenAccessException : DomainException
{
    public ForbiddenAccessException(string? detail = null)
        : base(detail ?? "You do not have permission to perform this action")
    {
    }
}
