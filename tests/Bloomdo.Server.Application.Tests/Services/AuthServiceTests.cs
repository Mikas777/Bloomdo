using System.Linq.Expressions;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Application.Services;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Server.Domain.Exceptions;
using Bloomdo.Shared.DTOs.Auth;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IRepository<RefreshToken>> _refreshRepo = new();
    private readonly Mock<IRolePermissionRepository> _rolePermRepo = new();
    private readonly Mock<IRepository<ActivityCompletion>> _completionRepo = new();
    private readonly Mock<IRepository<BlockRule>> _blockRepo = new();
    private readonly Mock<IRepository<AccountAchievement>> _achievementRepo = new();
    private readonly Mock<IRepository<Friendship>> _friendshipRepo = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly FakeAuthSettings _settings = new();

    private AuthService BuildSut()
    {
        _jwt.SetupGet(j => j.AccessTokenExpirationMinutes).Returns(15);
        _jwt.Setup(j => j.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<UserRole>>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns("access_token_value");
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns(() => "rt_" + Guid.NewGuid().ToString("N"));
        _rolePermRepo.Setup(r => r.GetPermissionsForRolesAsync(It.IsAny<IEnumerable<UserRole>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "perm.read" });
        _friendshipRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Friendship, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        return new AuthService(
            _accountRepo.Object, _refreshRepo.Object, _rolePermRepo.Object,
            _completionRepo.Object, _blockRepo.Object, _achievementRepo.Object,
            _friendshipRepo.Object, _jwt.Object, _settings);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_Throws()
    {
        _accountRepo.Setup(r => r.EmailExistsAsync("dup@x.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = BuildSut();

        var act = () => sut.RegisterAsync(new RegisterRequest { Email = "dup@x.com", Password = "Pwd1234", Username = "u" }, "1.1.1.1");

        await act.ShouldThrowAsync<EmailAlreadyExistsException>();
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_Throws()
    {
        _accountRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _accountRepo.Setup(r => r.UsernameExistsAsync("taken", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = BuildSut();

        var act = () => sut.RegisterAsync(new RegisterRequest { Email = "a@x.com", Password = "Pwd1234", Username = "Taken" }, "1.1.1.1");

        await act.ShouldThrowAsync<UsernameAlreadyExistsException>();
    }

    [Fact]
    public async Task RegisterAsync_NewUser_HashesPassword_PersistsAccount_ReturnsTokens()
    {
        _accountRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _accountRepo.Setup(r => r.UsernameExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Account? saved = null;
        _accountRepo.Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((a, _) => saved = a)
            .ReturnsAsync((Account a, CancellationToken _) => a);
        var sut = BuildSut();

        var resp = await sut.RegisterAsync(new RegisterRequest
        {
            Email = "new@x.com", Password = "Secret123", Username = "newuser",
            FirstName = "Vlad", LastName = "K"
        }, "1.1.1.1");

        resp.AccessToken.ShouldBe("access_token_value");
        resp.RefreshToken.ShouldStartWith("rt_");
        saved.ShouldNotBeNull();
        saved!.Email.ShouldBe("new@x.com");
        BCrypt.Net.BCrypt.Verify("Secret123", saved.PasswordHash).ShouldBeTrue();
        saved.PasswordHash.ShouldNotBe("Secret123");
        _refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_NormalizesUsernameToLowercase()
    {
        _accountRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _accountRepo.Setup(r => r.UsernameExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Account? saved = null;
        _accountRepo.Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((a, _) => saved = a)
            .ReturnsAsync((Account a, CancellationToken _) => a);
        var sut = BuildSut();

        await sut.RegisterAsync(new RegisterRequest { Email = "a@x.com", Password = "Secret123", Username = "  MixedCase  " }, "1.1.1.1");

        saved!.Username.ShouldBe("mixedcase");
    }

    [Fact]
    public async Task LoginAsync_AccountNotFound_ReturnsNull()
    {
        _accountRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);
        var sut = BuildSut();

        var resp = await sut.LoginAsync(new LoginRequest { Email = "no@x.com", Password = "x" }, "1.1.1.1");

        resp.ShouldBeNull();
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        var account = TestData.BuildAccount(email: "a@x.com", password: "RealPwd1");
        _accountRepo.Setup(r => r.GetByEmailAsync("a@x.com", It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var sut = BuildSut();

        var resp = await sut.LoginAsync(new LoginRequest { Email = "a@x.com", Password = "WrongPwd" }, "1.1.1.1");

        resp.ShouldBeNull();
    }

    [Fact]
    public async Task LoginAsync_Valid_UpdatesLastLogin_ReturnsTokens()
    {
        var account = TestData.BuildAccount(email: "a@x.com", password: "RealPwd1");
        account.LastLoginAt = null;
        _accountRepo.Setup(r => r.GetByEmailAsync("a@x.com", It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var sut = BuildSut();

        var resp = await sut.LoginAsync(new LoginRequest { Email = "a@x.com", Password = "RealPwd1" }, "1.1.1.1");

        resp.ShouldNotBeNull();
        resp!.AccessToken.ShouldBe("access_token_value");
        account.LastLoginAt.ShouldNotBeNull();
        _accountRepo.Verify(r => r.UpdateAsync(account, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_UnknownToken_ReturnsNull()
    {
        _refreshRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);
        var sut = BuildSut();

        var resp = await sut.RefreshTokenAsync("missing", "1.1.1.1");

        resp.ShouldBeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_Active_RotatesAndReturnsNewPair()
    {
        var account = TestData.BuildAccount();
        var old = TestData.BuildRefreshToken(account.Id, token: "old");
        _refreshRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(old);
        _accountRepo.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var sut = BuildSut();

        var resp = await sut.RefreshTokenAsync("old", "1.1.1.1");

        resp.ShouldNotBeNull();
        resp!.RefreshToken.ShouldNotBe("old");
        old.IsRevoked.ShouldBeTrue();
        old.ReplacedByToken.ShouldBe(resp.RefreshToken);
        _refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_RecentlyRevokedWithReplacement_ReturnsReplacement()
    {
        var account = TestData.BuildAccount();
        var revoked = TestData.BuildRefreshToken(account.Id, token: "old", revoked: true,
            replacedBy: "new", revokedAt: DateTime.UtcNow.AddSeconds(-5));
        var replacement = TestData.BuildRefreshToken(account.Id, token: "new");

        _refreshRepo.SetupSequence(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(revoked)
            .ReturnsAsync(replacement);
        _accountRepo.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var sut = BuildSut();

        var resp = await sut.RefreshTokenAsync("old", "1.1.1.1");

        resp.ShouldNotBeNull();
        resp!.RefreshToken.ShouldBe("new");
        // No new token issued during grace period
        _refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeTokenAsync_OtherUsersToken_ThrowsForbidden()
    {
        var attacker = Guid.NewGuid();
        var victim = Guid.NewGuid();
        var token = TestData.BuildRefreshToken(victim, token: "vt");
        _refreshRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        var sut = BuildSut();

        var act = () => sut.RevokeTokenAsync(attacker, "vt", "1.1.1.1");

        await act.ShouldThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task GetProfileStatsAsync_ComputesStreak_TasksCompleted_FocusHours()
    {
        var account = TestData.BuildAccount();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _accountRepo.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _completionRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityCompletion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ActivityCompletion>
            {
                TestData.BuildCompletion(account.Id, Guid.NewGuid(), today),
                TestData.BuildCompletion(account.Id, Guid.NewGuid(), today.AddDays(-1)),
                TestData.BuildCompletion(account.Id, Guid.NewGuid(), today.AddDays(-2)),
                // Gap on day -3, so streak ends here
                TestData.BuildCompletion(account.Id, Guid.NewGuid(), today.AddDays(-5)),
            });
        _blockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BlockRule, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BlockRule>
            {
                TestData.BuildBlockRule(account.Id, BlockType.Focus, focusDurationMinutes: 60),
                TestData.BuildBlockRule(account.Id, BlockType.Focus, focusDurationMinutes: 30),
                TestData.BuildBlockRule(account.Id, BlockType.Schedule)
            });
        _achievementRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AccountAchievement, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountAchievement> { new() { AccountId = account.Id, AchievementId = Guid.NewGuid(), UnlockedDate = today } });

        var sut = BuildSut();

        var stats = await sut.GetProfileStatsAsync(account.Id);

        stats.TasksCompleted.ShouldBe(4);
        stats.StreakDays.ShouldBe(3);
        stats.TotalBlocksCreated.ShouldBe(3);
        stats.AchievementsUnlocked.ShouldBe(1);
        stats.FocusHours.ShouldBe(1); // (60 + 30) / 60 = 1 (integer division)
    }
}
