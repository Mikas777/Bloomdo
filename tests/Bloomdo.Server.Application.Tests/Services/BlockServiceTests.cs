using System.Linq.Expressions;
using Bloomdo.Server.Application.Exceptions;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Application.Services;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.DTOs.Blocks;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Application.Tests.Services;

public class BlockServiceTests
{
    private readonly Mock<IRepository<BlockRule>> _blockRepo = new();
    private readonly Mock<IRepository<ActivityGroup>> _groupRepo = new();
    private readonly Mock<IRepository<ActivityItem>> _itemRepo = new();
    private readonly Mock<IRepository<ActivityCompletion>> _completionRepo = new();
    private readonly Mock<ISubscriptionService> _subService = new();
    private readonly FakeFreeLimitsSettings _free = new() { MaxBlockRules = 2 };

    private BlockService BuildSut() => new(
        _blockRepo.Object, _groupRepo.Object, _itemRepo.Object,
        _completionRepo.Object, _subService.Object, _free);

    [Fact]
    public async Task CreateBlockRule_FreeUser_AtLimit_Throws()
    {
        var accountId = Guid.NewGuid();
        _subService.Setup(s => s.IsPremiumAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _blockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BlockRule, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BlockRule>
            {
                TestData.BuildBlockRule(accountId),
                TestData.BuildBlockRule(accountId)
            });

        var act = () => BuildSut().CreateBlockRuleAsync(accountId, new CreateBlockRuleRequest { Title = "x", Type = BlockType.Schedule });

        await act.ShouldThrowAsync<BlockLimitExceededException>();
    }

    [Fact]
    public async Task CreateBlockRule_FreeUser_BelowLimit_Persists()
    {
        var accountId = Guid.NewGuid();
        _subService.Setup(s => s.IsPremiumAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _blockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BlockRule, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        BlockRule? added = null;
        _blockRepo.Setup(r => r.AddAsync(It.IsAny<BlockRule>(), It.IsAny<CancellationToken>()))
            .Callback<BlockRule, CancellationToken>((b, _) => added = b)
            .ReturnsAsync((BlockRule b, CancellationToken _) => b);

        var resp = await BuildSut().CreateBlockRuleAsync(accountId, new CreateBlockRuleRequest
        {
            Title = "Sleep", Type = BlockType.Schedule, BlockedPackages = ["com.tiktok"]
        });

        added.ShouldNotBeNull();
        added!.Title.ShouldBe("Sleep");
        added.BlockedPackagesJson.ShouldContain("com.tiktok");
        resp.Title.ShouldBe("Sleep");
    }

    [Fact]
    public async Task CreateBlockRule_PremiumUser_BypassesLimit()
    {
        var accountId = Guid.NewGuid();
        _subService.Setup(s => s.IsPremiumAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        // Note: FindAsync should NOT be called for premium — they bypass the limit check
        _blockRepo.Setup(r => r.AddAsync(It.IsAny<BlockRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlockRule b, CancellationToken _) => b);

        await BuildSut().CreateBlockRuleAsync(accountId, new CreateBlockRuleRequest { Title = "x", Type = BlockType.Schedule });

        _blockRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<BlockRule, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
        _blockRepo.Verify(r => r.AddAsync(It.IsAny<BlockRule>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBlockRule_FocusType_SetsFocusStartedAtUtc()
    {
        var accountId = Guid.NewGuid();
        _subService.Setup(s => s.IsPremiumAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        BlockRule? added = null;
        _blockRepo.Setup(r => r.AddAsync(It.IsAny<BlockRule>(), It.IsAny<CancellationToken>()))
            .Callback<BlockRule, CancellationToken>((b, _) => added = b)
            .ReturnsAsync((BlockRule b, CancellationToken _) => b);

        await BuildSut().CreateBlockRuleAsync(accountId, new CreateBlockRuleRequest
        {
            Title = "Deep work", Type = BlockType.Focus, FocusDurationMinutes = 60
        });

        added!.FocusStartedAtUtc.ShouldNotBeNull();
        added.FocusStartedAtUtc!.Value.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public async Task CreateBlockRule_NonFocusType_DoesNotSetFocusStartedAtUtc()
    {
        var accountId = Guid.NewGuid();
        _subService.Setup(s => s.IsPremiumAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        BlockRule? added = null;
        _blockRepo.Setup(r => r.AddAsync(It.IsAny<BlockRule>(), It.IsAny<CancellationToken>()))
            .Callback<BlockRule, CancellationToken>((b, _) => added = b)
            .ReturnsAsync((BlockRule b, CancellationToken _) => b);

        await BuildSut().CreateBlockRuleAsync(accountId, new CreateBlockRuleRequest { Title = "S", Type = BlockType.Schedule });

        added!.FocusStartedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateBlockRule_NotFound_ReturnsNull()
    {
        _blockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<BlockRule, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlockRule?)null);

        var result = await BuildSut().UpdateBlockRuleAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateBlockRuleRequest { Title = "new" });

        result.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateBlockRule_AppliesProvidedFieldsOnly()
    {
        var accountId = Guid.NewGuid();
        var rule = TestData.BuildBlockRule(accountId);
        rule.Title = "Original";
        rule.IsActive = true;
        _blockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<BlockRule, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        await BuildSut().UpdateBlockRuleAsync(accountId, rule.Id, new UpdateBlockRuleRequest { Title = "Renamed" });

        rule.Title.ShouldBe("Renamed");
        rule.IsActive.ShouldBeTrue(); // unchanged
    }

    [Fact]
    public async Task DeleteBlockRule_NotFound_ReturnsFalse()
    {
        _blockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<BlockRule, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlockRule?)null);

        (await BuildSut().DeleteBlockRuleAsync(Guid.NewGuid(), Guid.NewGuid())).ShouldBeFalse();
    }

    [Fact]
    public async Task GetBlockRules_BloomdoTypeWithCompletedGroup_IsBloomdoSatisfiedTrue()
    {
        var accountId = Guid.NewGuid();
        var group = TestData.BuildGroup(accountId);
        var item1 = TestData.BuildItem(group.Id, "morning task");
        var item2 = TestData.BuildItem(group.Id, "stretch");
        var rule = TestData.BuildBlockRule(accountId, BlockType.Bloomdo, requiredActivityGroupId: group.Id);

        _blockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BlockRule, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([rule]);
        _groupRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        _itemRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item1, item2]);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _completionRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityCompletion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ActivityCompletion>
            {
                TestData.BuildCompletion(accountId, item1.Id, today),
                TestData.BuildCompletion(accountId, item2.Id, today)
            });

        var rules = await BuildSut().GetBlockRulesAsync(accountId);

        rules.Single().IsBloomdoSatisfied.ShouldBeTrue();
        rules.Single().RequiredActivityGroupTitle.ShouldBe(group.Title);
    }
}
