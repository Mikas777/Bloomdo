namespace Bloomdo.Server.Domain.Exceptions;

public class InvalidRefreshTokenException : DomainException
{
    public InvalidRefreshTokenException() 
        : base("Invalid or expired refresh token")
    {
    }
}
