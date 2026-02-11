namespace Bloomdo.Server.Domain.Entities;

/// <summary>
/// Maps a <see cref="Role"/> to a granular permission string.
/// Stored in DB so permissions per role can be adjusted without redeploying.
/// </summary>
public class RolePermission : BaseEntity
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public string Permission { get; set; } = string.Empty;
}
