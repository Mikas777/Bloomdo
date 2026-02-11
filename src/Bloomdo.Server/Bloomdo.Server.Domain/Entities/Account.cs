namespace Bloomdo.Server.Domain.Entities;

public class Account : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public bool IsEmailConfirmed { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
