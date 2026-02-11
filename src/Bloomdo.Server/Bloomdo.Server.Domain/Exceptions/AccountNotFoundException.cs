namespace Bloomdo.Server.Domain.Exceptions;

public class AccountNotFoundException : DomainException
{
    public AccountNotFoundException(Guid accountId) 
        : base($"Account with ID '{accountId}' not found")
    {
        AccountId = accountId;
    }

    public Guid AccountId { get; }
}
