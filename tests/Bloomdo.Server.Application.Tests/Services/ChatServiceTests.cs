using System.Linq.Expressions;
using Bloomdo.Server.Application.Exceptions;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Application.Services;
using Bloomdo.Server.Domain.Entities;

namespace Bloomdo.Server.Application.Tests.Services;

public class ChatServiceTests
{
    private readonly Mock<IChatRepository> _chatRepo = new();
    private readonly Mock<IStatsRepository> _statsRepo = new();
    private readonly Mock<IDailyActivityService> _activityService = new();
    private readonly Mock<IRepository<BlockRule>> _blockRepo = new();
    private readonly Mock<ISubscriptionService> _subService = new();
    private readonly FakeGeminiSettings _gemini = new(); // no keys => Gemini fallback path
    private readonly FakeFreeLimitsSettings _free = new() { MaxDailyChatMessages = 3 };

    private ChatService BuildSut()
    {
        _statsRepo.Setup(r => r.GetSnapshotsForMonthAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _statsRepo.Setup(r => r.GetUsageRecordsForRangeAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _statsRepo.Setup(r => r.GetGoalMetDatesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _activityService.Setup(s => s.GetGroupsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _blockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BlockRule, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _chatRepo.Setup(r => r.AddMessageAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatMessage m, CancellationToken _) => m);

        return new ChatService(_chatRepo.Object, _statsRepo.Object, _activityService.Object,
            _blockRepo.Object, _gemini, _subService.Object, _free);
    }

    [Fact]
    public async Task SendMessage_FreeUser_AtLimit_Throws()
    {
        var accountId = Guid.NewGuid();
        _subService.Setup(s => s.IsPremiumAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _chatRepo.Setup(r => r.CountTodayUserMessagesAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var act = () => BuildSut().SendMessageAsync(accountId, null, "hi");

        await act.ShouldThrowAsync<ChatLimitExceededException>();
    }

    [Fact]
    public async Task SendMessage_NewConversation_CreatesWithTruncatedTitle()
    {
        var accountId = Guid.NewGuid();
        _subService.Setup(s => s.IsPremiumAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        ChatConversation? created = null;
        _chatRepo.Setup(r => r.CreateConversationAsync(It.IsAny<ChatConversation>(), It.IsAny<CancellationToken>()))
            .Callback<ChatConversation, CancellationToken>((c, _) => { c.Id = Guid.NewGuid(); c.Messages = []; created = c; })
            .ReturnsAsync((ChatConversation c, CancellationToken _) => c);

        var longMessage = new string('a', 80);
        await BuildSut().SendMessageAsync(accountId, null, longMessage);

        created.ShouldNotBeNull();
        created!.Title.Length.ShouldBe(53); // 50 + "..."
        created.Title.ShouldEndWith("...");
    }

    [Fact]
    public async Task SendMessage_ShortMessage_TitleNotTruncated()
    {
        var accountId = Guid.NewGuid();
        _subService.Setup(s => s.IsPremiumAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        ChatConversation? created = null;
        _chatRepo.Setup(r => r.CreateConversationAsync(It.IsAny<ChatConversation>(), It.IsAny<CancellationToken>()))
            .Callback<ChatConversation, CancellationToken>((c, _) => { c.Id = Guid.NewGuid(); c.Messages = []; created = c; })
            .ReturnsAsync((ChatConversation c, CancellationToken _) => c);

        await BuildSut().SendMessageAsync(accountId, null, "Hello");

        created!.Title.ShouldBe("Hello");
    }

    [Fact]
    public async Task SendMessage_ExistingConversation_AppendsMessages()
    {
        var accountId = Guid.NewGuid();
        _subService.Setup(s => s.IsPremiumAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var conv = TestData.BuildConversation(accountId);
        conv.Messages = [];
        _chatRepo.Setup(r => r.GetConversationWithMessagesAsync(conv.Id, accountId, It.IsAny<CancellationToken>())).ReturnsAsync(conv);

        await BuildSut().SendMessageAsync(accountId, conv.Id, "follow-up");

        _chatRepo.Verify(r => r.CreateConversationAsync(It.IsAny<ChatConversation>(), It.IsAny<CancellationToken>()), Times.Never);
        _chatRepo.Verify(r => r.AddMessageAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _chatRepo.Verify(r => r.UpdateConversationAsync(conv, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessage_ExistingConversation_NotFound_Throws()
    {
        var accountId = Guid.NewGuid();
        _subService.Setup(s => s.IsPremiumAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _chatRepo.Setup(r => r.GetConversationWithMessagesAsync(It.IsAny<Guid>(), accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatConversation?)null);

        var act = () => BuildSut().SendMessageAsync(accountId, Guid.NewGuid(), "x");

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetConversations_OrdersByUpdatedAtDesc_ExcludesDeletedMessagesFromPreview()
    {
        var accountId = Guid.NewGuid();
        var newer = TestData.BuildConversation(accountId, "Newer", DateTime.UtcNow.AddDays(-1));
        newer.UpdatedAt = DateTime.UtcNow;
        var older = TestData.BuildConversation(accountId, "Older", DateTime.UtcNow.AddDays(-3));
        older.UpdatedAt = DateTime.UtcNow.AddDays(-2);

        newer.Messages =
        [
            TestData.BuildMessage(newer.Id, "user", "deleted msg", DateTime.UtcNow.AddMinutes(-1), deleted: true),
            TestData.BuildMessage(newer.Id, "assistant", "live preview", DateTime.UtcNow.AddMinutes(-5)),
        ];
        older.Messages = [TestData.BuildMessage(older.Id, "user", "old", DateTime.UtcNow.AddDays(-2))];

        _chatRepo.Setup(r => r.GetConversationsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([older, newer]);

        var result = await BuildSut().GetConversationsAsync(accountId);

        result[0].Title.ShouldBe("Newer");
        result[0].LastMessage.ShouldNotBeNull();
        result[0].LastMessage!.Content.ShouldBe("live preview"); // deleted excluded
        result[1].Title.ShouldBe("Older");
    }

    [Fact]
    public async Task GetConversation_ExcludesDeletedMessages_OrdersByCreatedAt()
    {
        var accountId = Guid.NewGuid();
        var conv = TestData.BuildConversation(accountId);
        conv.Messages =
        [
            TestData.BuildMessage(conv.Id, "user", "third", DateTime.UtcNow.AddMinutes(-1)),
            TestData.BuildMessage(conv.Id, "user", "deleted", DateTime.UtcNow.AddMinutes(-2), deleted: true),
            TestData.BuildMessage(conv.Id, "user", "first", DateTime.UtcNow.AddMinutes(-5)),
            TestData.BuildMessage(conv.Id, "assistant", "second", DateTime.UtcNow.AddMinutes(-3))
        ];
        _chatRepo.Setup(r => r.GetConversationWithMessagesAsync(conv.Id, accountId, It.IsAny<CancellationToken>())).ReturnsAsync(conv);

        var result = await BuildSut().GetConversationAsync(conv.Id, accountId);

        result.ShouldNotBeNull();
        result!.Messages.Select(m => m.Content).ShouldBe(["first", "second", "third"]);
    }

    [Fact]
    public async Task DeleteConversation_DelegatesToRepository()
    {
        var accountId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        _chatRepo.Setup(r => r.DeleteConversationAsync(conversationId, accountId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await BuildSut().DeleteConversationAsync(conversationId, accountId);

        result.ShouldBeTrue();
        _chatRepo.Verify(r => r.DeleteConversationAsync(conversationId, accountId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
