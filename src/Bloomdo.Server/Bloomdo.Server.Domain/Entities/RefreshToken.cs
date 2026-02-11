namespace Bloomdo.Server.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public string? RevokedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}
