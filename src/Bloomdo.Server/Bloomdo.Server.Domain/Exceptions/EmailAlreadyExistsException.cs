namespace Bloomdo.Server.Domain.Exceptions;

public class EmailAlreadyExistsException : DomainException
{
    public EmailAlreadyExistsException(string email) 
        : base($"Account with email '{email}' already exists")
    {
        Email = email;
    }

    public string Email { get; }
}
