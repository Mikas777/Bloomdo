using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Domain.Entities;

public class Friendship : BaseEntity
{
    public Guid RequesterId { get; set; }
    public Guid AddresseeId { get; set; }
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

    public Account Requester { get; set; } = null!;
    public Account Addressee { get; set; } = null!;
}
