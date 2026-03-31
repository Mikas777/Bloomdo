using Bloomdo.Server.Api.Authorization;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Bloomdo.Server.Api.Controllers;

[ApiController]
[Authorize]
public class SocialController(ISocialService socialService) : ControllerBase
{
    [HttpGet(ApiRoutes.Social.Search)]
    public async Task<IActionResult> Search([FromQuery] string query, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var users = await socialService.SearchUsersAsync(accountId.Value, query, ct);
        return Ok(users);
    }

    [HttpGet(ApiRoutes.Social.Friends)]
    public async Task<IActionResult> GetFriends(CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var friends = await socialService.GetFriendsAsync(accountId.Value, ct);
        return Ok(friends);
    }

    [HttpPost(ApiRoutes.Social.Request)]
    public async Task<IActionResult> SendRequest([FromQuery] Guid addresseeId, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await socialService.SendFriendRequestAsync(accountId.Value, addresseeId, ct);
        return result ? Ok() : BadRequest("Request failed or already exists.");
    }

    [HttpPut(ApiRoutes.Social.Respond)]
    public async Task<IActionResult> RespondToRequest(Guid id, [FromQuery] bool accept, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await socialService.RespondToFriendRequestAsync(accountId.Value, id, accept, ct);
        return result ? Ok() : BadRequest("Invalid request or unauthorized.");
    }

    [HttpDelete(ApiRoutes.Social.Remove)]
    public async Task<IActionResult> RemoveFriend(Guid id, CancellationToken ct)
    {
        var accountId = GetAccountId();
        if (accountId is null) return Unauthorized();

        var result = await socialService.RemoveFriendAsync(accountId.Value, id, ct);
        return result ? NoContent() : NotFound();
    }

    private Guid? GetAccountId()
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
