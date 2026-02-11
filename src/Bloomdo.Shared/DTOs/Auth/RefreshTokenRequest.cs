using System.ComponentModel.DataAnnotations;

namespace Bloomdo.Shared.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
