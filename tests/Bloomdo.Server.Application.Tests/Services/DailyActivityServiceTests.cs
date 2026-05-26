using System.Linq.Expressions;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Application.Services;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.DTOs.Activities;

namespace Bloomdo.Server.Application.Tests.Services;

public class DailyActivityServiceTests
{
    private readonly Mock<IRepository<ActivityGroup>> _groupRepo = new();
    private readonly Mock<IRepository<ActivityItem>> _itemRepo = new();
    private readonly Mock<IRepository<ActivityCompletion>> _completionRepo = new();
    private readonly Mock<IRepository<GroupMembership>> _membershipRepo = new();
    private readonly Mock<IVisionService> _visionService = new();
    private readonly Mock<IStatsService> _statsService = new();

    private DailyActivityService BuildSut() => new(
        _groupRepo.Object, _itemRepo.Object, _completionRepo.Object,
        _membershipRepo.Object, _visionService.Object, _statsService.Object);

    [Fact]
    public async Task CreateGroup_AssignsNextSortOrder()
    {
        var accountId = Guid.NewGuid();
        _groupRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ActivityGroup>
            {
                TestData.BuildGroup(accountId, "A", sortOrder: 1),
                TestData.BuildGroup(accountId, "B", sortOrder: 5)
            });
        ActivityGroup? added = null;
        _groupRepo.Setup(r => r.AddAsync(It.IsAny<ActivityGroup>(), It.IsAny<CancellationToken>()))
            .Callback<ActivityGroup, CancellationToken>((g, _) => added = g)
            .ReturnsAsync((ActivityGroup g, CancellationToken _) => g);

        await BuildSut().CreateGroupAsync(accountId, new CreateActivityGroupRequest { Title = "Evening" });

        added!.SortOrder.ShouldBe(6);
    }

    [Fact]
    public async Task CreateItem_GroupNotOwned_Throws()
    {
        var accountId = Guid.NewGuid();
        _groupRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActivityGroup?)null);

        var act = () => BuildSut().CreateItemAsync(accountId, new CreateActivityItemRequest { ActivityGroupId = Guid.NewGuid(), Title = "X" });

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateItem_NonOwner_ReturnsNull()
    {
        var accountId = Guid.NewGuid();
        var item = TestData.BuildItem(Guid.NewGuid());
        _itemRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ActivityItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _groupRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActivityGroup?)null);

        var result = await BuildSut().UpdateItemAsync(accountId, item.Id, new UpdateActivityItemRequest { Title = "x" });

        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteGroup_AlsoDeletesItems()
    {
        var accountId = Guid.NewGuid();
        var group = TestData.BuildGroup(accountId);
        var item1 = TestData.BuildItem(group.Id);
        var item2 = TestData.BuildItem(group.Id);
        _groupRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        _itemRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ActivityItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item1, item2]);

        await BuildSut().DeleteGroupAsync(accountId, group.Id);

        _itemRepo.Verify(r => r.DeleteAsync(item1, It.IsAny<CancellationToken>()), Times.Once);
        _itemRepo.Verify(r => r.DeleteAsync(item2, It.IsAny<CancellationToken>()), Times.Once);
        _groupRepo.Verify(r => r.DeleteAsync(group, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleCompletion_NonOwnerNonMember_ReturnsFalse()
    {
        var accountId = Guid.NewGuid();
        var item = TestData.BuildItem(Guid.NewGuid());
        _itemRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ActivityItem, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _groupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _membershipRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<GroupMembership, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var ok = await BuildSut().ToggleCompletionAsync(accountId, new ToggleCompletionRequest { ActivityItemId = item.Id, Date = DateOnly.FromDateTime(DateTime.UtcNow) });

        ok.ShouldBeFalse();
        _statsService.Verify(s => s.RecalculateGoalMetAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleCompletion_NewCompletion_Adds_RecalculatesGoal()
    {
        var accountId = Guid.NewGuid();
        var item = TestData.BuildItem(Guid.NewGuid());
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _itemRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ActivityItem, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _groupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _completionRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ActivityCompletion, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((ActivityCompletion?)null);

        var ok = await BuildSut().ToggleCompletionAsync(accountId, new ToggleCompletionRequest { ActivityItemId = item.Id, Date = today });

        ok.ShouldBeTrue();
        _completionRepo.Verify(r => r.AddAsync(It.IsAny<ActivityCompletion>(), It.IsAny<CancellationToken>()), Times.Once);
        _statsService.Verify(s => s.RecalculateGoalMetAsync(accountId, today, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleCompletion_ExistingNoCountValue_Deletes()
    {
        var accountId = Guid.NewGuid();
        var item = TestData.BuildItem(Guid.NewGuid());
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existing = TestData.BuildCompletion(accountId, item.Id, today);

        _itemRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ActivityItem, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _groupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _completionRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ActivityCompletion, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await BuildSut().ToggleCompletionAsync(accountId, new ToggleCompletionRequest { ActivityItemId = item.Id, Date = today });

        _completionRepo.Verify(r => r.DeleteAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _completionRepo.Verify(r => r.UpdateAsync(It.IsAny<ActivityCompletion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleCompletion_ExistingWithCountValue_Updates()
    {
        var accountId = Guid.NewGuid();
        var item = TestData.BuildItem(Guid.NewGuid(), type: ActivityItemType.Count, targetCount: 10);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existing = TestData.BuildCompletion(accountId, item.Id, today, countValue: 3);

        _itemRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ActivityItem, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _groupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _completionRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ActivityCompletion, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await BuildSut().ToggleCompletionAsync(accountId, new ToggleCompletionRequest { ActivityItemId = item.Id, Date = today, CountValue = 7 });

        existing.CountValue.ShouldBe(7);
        _completionRepo.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _completionRepo.Verify(r => r.DeleteAsync(It.IsAny<ActivityCompletion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VerifyPhoto_Verified_AutoCompletesItem()
    {
        var accountId = Guid.NewGuid();
        var item = TestData.BuildItem(Guid.NewGuid());
        item.VerificationTemplateId = (int)VerificationTemplate.Workout;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _itemRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ActivityItem, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _groupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _visionService.Setup(v => v.VerifyAsync(It.IsAny<byte[]>(), VerificationTemplate.Workout, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VisionResult(VerificationStatus.Verified, "ok", 0.95f));
        _completionRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ActivityCompletion, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((ActivityCompletion?)null);

        var imageBase64 = Convert.ToBase64String([1, 2, 3]);
        var resp = await BuildSut().VerifyPhotoAsync(accountId, new VerifyPhotoRequest { ActivityItemId = item.Id, Date = today, ImageBase64 = imageBase64 });

        resp.Status.ShouldBe(VerificationStatus.Verified);
        _completionRepo.Verify(r => r.AddAsync(It.IsAny<ActivityCompletion>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyPhoto_Rejected_DoesNotComplete()
    {
        var accountId = Guid.NewGuid();
        var item = TestData.BuildItem(Guid.NewGuid());
        item.VerificationTemplateId = (int)VerificationTemplate.Meditation;

        _itemRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ActivityItem, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _groupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ActivityGroup, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _visionService.Setup(v => v.VerifyAsync(It.IsAny<byte[]>(), It.IsAny<VerificationTemplate>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VisionResult(VerificationStatus.Rejected, "no", 0.2f));

        var imageBase64 = Convert.ToBase64String([1, 2, 3]);
        var resp = await BuildSut().VerifyPhotoAsync(accountId, new VerifyPhotoRequest { ActivityItemId = item.Id, Date = DateOnly.FromDateTime(DateTime.UtcNow), ImageBase64 = imageBase64 });

        resp.Status.ShouldBe(VerificationStatus.Rejected);
        _completionRepo.Verify(r => r.AddAsync(It.IsAny<ActivityCompletion>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
