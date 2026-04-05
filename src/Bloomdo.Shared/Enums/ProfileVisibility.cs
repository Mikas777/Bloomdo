namespace Bloomdo.Shared.Enums;

/// <summary>
/// Controls who can see a user's full profile and statistics.
/// </summary>
public enum ProfileVisibility
{
    /// <summary>Anyone can see full profile and stats.</summary>
    Public = 0,

    /// <summary>Only mutual followers (friends) can see full stats. Others see basic info.</summary>
    FriendsOnly = 1,

    /// <summary>Follow requests require approval. Only accepted followers see the profile.</summary>
    Private = 2
}
