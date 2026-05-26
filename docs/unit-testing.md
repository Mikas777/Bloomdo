# Unit Testing — Bloomdo.Server.Application

This chapter describes the unit-testing approach used to verify the
business logic of the Bloomdo server-side application layer. It explains
the technology stack, project structure, test patterns, scope of
coverage, verification methodology, and known limitations.

---

## 1. Purpose and Goals

The Bloomdo application is a cross-platform productivity assistant that
helps users manage screen-time addiction. It combines a .NET 10 backend
(ASP.NET Core), an Avalonia UI client, third-party integrations (Stripe
for payments, Google Gemini for AI chat, BCrypt for password hashing),
and a complex domain (subscriptions, streaks, achievements, social
groups, photo verification).

Because the application has many business rules that are difficult to
verify manually — for example, "a free user can create at most 3 block
rules", or "the streak counter should bridge a one-day gap when the user
has a streak freeze available" — automated unit tests are essential for
the following reasons:

1. **Regression prevention.** Each change to the codebase risks breaking
   an existing rule. Unit tests catch these regressions immediately,
   before the code reaches production.
2. **Fast feedback during development.** Tests run in approximately one
   second, allowing the developer to iterate quickly.
3. **Living documentation.** A well-named test acts as an executable
   specification of how a feature behaves.
4. **Refactor safety.** Internal restructuring of the code becomes safe
   when a test suite verifies the externally observable behaviour.

The chosen scope is the **Application layer** (the project
`Bloomdo.Server.Application`), which contains the services that
implement core business logic. Lower layers (Domain entities, EF Core
repositories) and upper layers (API controllers, UI ViewModels) are
intentionally excluded — they belong to different testing strategies
(domain unit tests, integration tests, and UI tests respectively).

---

## 2. Technology Stack

The test project uses a small set of widely-adopted .NET testing
libraries.

| Library | Version | Purpose |
|---------|---------|---------|
| **xUnit** | 2.9.2 | Test runner and assertion framework. Industry standard for modern .NET projects. |
| **Shouldly** | 4.2.1 | Fluent, readable assertion syntax (`value.ShouldBe(x)` instead of `Assert.Equal(x, value)`). |
| **Moq** | 4.20.72 | Mocking framework for substituting repository and service dependencies with controlled fakes. |
| **Microsoft.NET.Test.Sdk** | 17.12.0 | The test platform that lets `dotnet test` discover and execute the suite. |
| **coverlet.collector** | 6.0.2 | Code-coverage data collector that integrates with `dotnet test --collect:"XPlat Code Coverage"`. |
| **xunit.runner.visualstudio** | 2.8.2 | Test adapter for Visual Studio and JetBrains Rider integration. |

### Why these libraries?

- **xUnit over NUnit / MSTest.** xUnit is currently the most widely used
  framework in modern .NET, has the simplest attribute model
  (`[Fact]`, `[Theory]`), and gives each test its own class instance —
  which makes parallel test execution safer and prevents shared-state
  bugs between tests.
- **Shouldly over plain `Assert.*` or FluentAssertions.** Shouldly
  produces highly readable failure messages by inspecting the source
  expression. For example, the failure message `result.CurrentStreak
  should be 3 but was 1` is generated automatically from the
  assertion `result.CurrentStreak.ShouldBe(3)`. FluentAssertions has
  changed its commercial licence terms recently, while Shouldly
  remains MIT-licensed.
- **Moq over NSubstitute / FakeItEasy.** Moq is the classic and most
  battle-tested mocking library in the .NET ecosystem. Its strict
  setup model encourages explicit, intentional test fixtures.
- **No EF Core In-Memory provider.** The services under test do not
  depend on `DbContext` directly. They depend on repository
  abstractions (`IRepository<T>`, `IAccountRepository`,
  `IChatRepository`, etc.). Mocking these interfaces with Moq is
  therefore the correct and most isolated approach. An in-memory EF
  Core database would only be appropriate for testing the
  Infrastructure layer's repository implementations.

---

## 3. Project Structure

The test project lives at `tests/Bloomdo.Server.Application.Tests/`
and is registered in the solution file under a dedicated folder
`/04_Tests/`. The directory layout is the following:

```
tests/
└── Bloomdo.Server.Application.Tests/
    ├── Bloomdo.Server.Application.Tests.csproj
    ├── GlobalUsings.cs
    ├── Common/
    │   ├── TestSettings.cs
    │   └── TestData.cs
    └── Services/
        ├── AuthServiceTests.cs
        ├── SubscriptionServiceTests.cs
        ├── BlockServiceTests.cs
        ├── ChatServiceTests.cs
        ├── StatsServiceTests.cs
        ├── AchievementServiceTests.cs
        └── DailyActivityServiceTests.cs
```

The `Common/` folder contains shared infrastructure:

- **TestSettings.cs** — concrete in-memory implementations of the
  settings interfaces (`IAuthSettings`, `IFreeLimitsSettings`,
  `IStripeSettings`, `IGeminiSettings`). They allow each test to
  override only the configuration values that matter for that test.
- **TestData.cs** — a static factory class that builds entities
  (`Account`, `BlockRule`, `Subscription`, `ActivityGroup`, etc.)
  with safe default values. Each test method overrides only the
  fields relevant to the scenario under verification, which keeps the
  test bodies short and focused.

`GlobalUsings.cs` exposes the most common namespaces (xUnit, Moq,
Shouldly, common test helpers) to every test file, removing
repetitive `using` directives.

The `.csproj` file references three production projects:

1. `Bloomdo.Server.Application` — the project under test.
2. `Bloomdo.Server.Domain` — needed for entity types.
3. `Bloomdo.Shared` — needed for DTOs and shared enums.

It also references the Stripe.net NuGet package directly because some
tests construct synthetic webhook payloads that must be parseable by
the Stripe SDK.

---

## 4. Test Patterns and Conventions

### 4.1 Arrange–Act–Assert (AAA)

Every test follows the AAA structure:

1. **Arrange** — set up mocks, build entities, configure return
   values.
2. **Act** — invoke a single method on the service under test.
3. **Assert** — verify the result and the side effects (e.g. which
   mocks were called, with which arguments, how many times).

### 4.2 Naming convention

Tests are named `MethodUnderTest_Scenario_ExpectedResult`. Examples:

- `RegisterAsync_DuplicateEmail_Throws`
- `LoginAsync_WrongPassword_ReturnsNull`
- `CreateBlockRule_FreeUser_AtLimit_Throws`
- `GetMonthCalendar_FreezeDayMergesIntoStreak`

This convention makes the test report self-documenting — when a test
fails, its name alone communicates what was being verified.

### 4.3 Mocking strategy

Every external collaborator of a service is replaced with a `Mock<T>`.
For example, `AuthService` depends on nine interfaces; each of those
is substituted with a Moq instance. The test then:

1. Configures only the methods that the scenario actually invokes.
2. Verifies that the service interacted with the mock exactly as
   expected (`Mock.Verify(...)`).

This isolation guarantees that a test failure points to a problem in
the service itself, not in an unrelated repository or external API.

### 4.4 Test-data factories

To avoid duplication, all entities are constructed through factory
methods in `TestData`. For example:

```csharp
var account = TestData.BuildAccount(email: "user@example.com", password: "Secret123");
var sub = TestData.BuildSubscription(account.Id, SubscriptionStatus.Active);
```

Defaults are sensible (`IsEmailConfirmed = true`,
`CurrentPeriodEnd = today + 15 days`, etc.), so tests override only
what they actually care about. This keeps the body of each test
small and the intent clear.

### 4.5 One assertion theme per test

Tests are kept narrow. Each test verifies one specific behaviour or
business rule. When a feature has multiple rules (for example, "create
a block rule" has free-user-limit enforcement, premium bypass, and
focus-timer side effects), each rule has its own dedicated test.

---

## 5. Coverage Overview

The suite contains **65 tests** distributed across seven service
classes. The total runtime is approximately one second on a modern
developer machine. Test counts and the most important scenarios per
service are listed below.

### 5.1 AuthService (12 tests)

Authentication, registration, and refresh-token handling.

- Duplicate-email and duplicate-username rejection during registration.
- BCrypt password hashing is actually performed (verified through
  `BCrypt.Verify`).
- Username normalisation (lower-cased, trimmed).
- Wrong-password and missing-account login paths return `null`
  instead of leaking information.
- Successful login updates the `LastLoginAt` timestamp.
- Refresh-token rotation: each refresh issues a new token and revokes
  the old one.
- **Grace-period replay**: a recently-revoked token (within 30
  seconds) that has been replaced still returns the replacement
  token's response — this prevents legitimate retries from being
  rejected when network errors cause a duplicate refresh request.
- **IDOR protection** on `RevokeTokenAsync`: an attempt to revoke
  another user's token throws `ForbiddenAccessException`.
- Profile-statistics computation: streaks, total tasks completed,
  block-rule count, achievement count, and focus hours are all
  calculated correctly from raw data.

### 5.2 SubscriptionService (12 tests)

Premium / free tier logic and Stripe webhook handling.

- A user with no subscription record is correctly reported as free,
  with the configured free-tier limits.
- A subscription whose `CurrentPeriodEnd` is in the past is
  automatically transitioned to the `Expired` status on read.
- A premium user is reported with unlimited chat messages, unlimited
  block rules, and the configured monthly streak-freeze allowance.
- Remaining chat messages are correctly clamped to zero when the
  user has already exceeded the daily limit.
- `IsPremiumAsync` is consistent with `GetStatusAsync` — both
  perform the same expiry check and persist the same state change.
- Stripe webhook handler for `checkout.session.completed`:
  - Creates a new subscription record when none exists.
  - Updates an existing subscription record (covering the case of a
    user who previously subscribed and is renewing).
  - Ignores invalid metadata (for example, a malformed `accountId`).
- `CancelSubscriptionAsync` throws when there is no Stripe
  subscription identifier to cancel.

### 5.3 BlockService (9 tests)

Application-blocking rules.

- A free user at the configured `MaxBlockRules` limit cannot create a
  new rule — the request throws `BlockLimitExceededException`.
- A free user below the limit successfully creates a rule, and the
  blocked-package list is correctly serialized to JSON.
- A premium user bypasses the limit; the test verifies that the
  free-tier `FindAsync` count query is **not** executed at all.
- A `Focus` block rule sets `FocusStartedAtUtc` to "now" at creation
  time; a `Schedule` rule leaves the field null.
- Updating a non-existent rule, or one belonging to a different
  user, returns `null` (the service uses query predicates that
  combine `Id` and `AccountId`).
- For a `Bloomdo`-type rule pointing at an activity group, the
  `IsBloomdoSatisfied` flag reflects whether all the group's items
  are completed today.

### 5.4 ChatService (8 tests)

AI chat, conversations, and message limits.

- A free user at the daily message limit cannot send another message
  — the service throws `ChatLimitExceededException`.
- A new conversation is created when no `conversationId` is
  supplied; the conversation title is the first 50 characters of the
  message, suffixed with `...` when longer.
- A short message becomes the conversation title verbatim, without
  truncation.
- Sending into an existing conversation appends the user message and
  assistant reply without creating a new conversation.
- Sending into a non-existent conversation throws
  `InvalidOperationException` instead of silently creating one.
- The conversation list is ordered by `UpdatedAt` descending, and
  deleted messages are excluded from the preview field.
- `GetConversation` excludes deleted messages and orders them by
  `CreatedAt` ascending.
- `DeleteConversationAsync` delegates correctly to the repository.

### 5.5 StatsService (10 tests)

Daily usage synchronization and streak calculation.

- Sync of new daily-usage data creates new records when none exist
  for that date.
- Sync of existing data updates the existing record in place
  (verified by `Verify(..., Times.Never)` on the `Add*` methods).
- The `GoalMet` flag is recomputed correctly:
  - false when the user has no activity groups;
  - false when a Count-type task is below its target;
  - true when a Count-type task has reached its target.
- Weekly statistics:
  - return seven days of data, even when some days have no records;
  - include a comparison block (with `IsImproving` flag and
    percent-change values) when the previous week has data.
- The month calendar correctly computes the **current** and
  **longest** streak from goal-met dates.
- **Streak freezes bridge gaps**: a one-day gap between two goal-met
  days is treated as a continuous streak when a `StreakFreeze`
  record exists for the gap date.

### 5.6 AchievementService (5 tests)

Streak-based achievement unlocking.

- If the user has no goal-met days, no achievements are unlocked.
- A 3-day streak unlocks only the `streak_3` achievement.
- A 7-day streak unlocks both `streak_3` and `streak_7` (the rule
  cascades through all thresholds the user qualifies for).
- An achievement that is already unlocked is never re-unlocked
  (verified by `Verify(..., Times.Never)` on `AddAsync`).
- `GetAchievementsAsync` returns the full achievement list with the
  correct `IsUnlocked` flag per item.

### 5.7 DailyActivityService (9 tests)

Activity groups, items, completions, and photo verification.

- Creating a group assigns the next available `SortOrder` (max + 1).
- Creating an item under a group the user does not own throws
  `InvalidOperationException`.
- Updating an item via a group the user does not own returns
  `null`.
- Deleting a group also deletes its child items.
- Toggling completion for an item where the user is neither owner
  nor accepted member returns `false` and does not trigger goal
  recalculation.
- Toggling a new completion adds the record and triggers
  `RecalculateGoalMetAsync`.
- Toggling an existing completion (without a count value) deletes
  it; with a count value, the existing record is updated in place.
- Photo verification: when the vision service returns
  `VerificationStatus.Verified`, the item is automatically marked as
  completed; when rejected, no completion is added.

---

## 6. Verification Methodology

Each test is verified through two complementary mechanisms:

1. **State assertions** using Shouldly — the returned value or the
   mutated entity must satisfy the expected property values.
2. **Interaction verification** using `Mock.Verify(...)` — the test
   asserts that the service called the expected dependency methods
   the expected number of times with the expected arguments. This
   catches a common class of bugs where a method "appears" to work
   but actually skipped a critical side effect (for example,
   forgetting to persist an entity).

For example, the test
`CreateBlockRule_PremiumUser_BypassesLimit` does not only check that
the rule was created — it also asserts that the limit-counting
query (`FindAsync`) was never invoked, proving that premium users
genuinely skip the limit logic rather than coincidentally passing
the check.

The whole suite is executed with the command:

```
dotnet test tests/Bloomdo.Server.Application.Tests
```

A green run produces the following output (Russian system locale):

```
Пройден!: пройдено 65, не пройдено 0, пропущено 0, всего 65, длительность ~1s
```

Code coverage may be collected with:

```
dotnet test tests/Bloomdo.Server.Application.Tests --collect:"XPlat Code Coverage"
```

---

## 7. Limitations and Out-of-Scope Areas

The suite intentionally does not cover the following areas. Each is
listed with the rationale.

1. **Outbound Stripe HTTP calls** —
   `CreateCheckoutSessionAsync` and the outbound part of
   `CancelSubscriptionAsync` invoke the Stripe SDK through the
   concrete `StripeClient` type, which cannot be substituted by a
   mock without a thin wrapper interface. Webhook-handler tests
   exercise the inbound path through real `EventUtility.ParseEvent`
   calls with synthetic-but-Stripe-valid JSON, so the business
   logic in those handlers is still covered.
2. **Gemini API happy path in `ChatService`** — the Google.GenAI SDK
   exposes a concrete `Client` type that cannot be mocked directly.
   The error-path branch (all API keys exhausted) is covered by
   passing a `FakeGeminiSettings` instance with no keys. Verifying
   the success path would require either an integration test
   against a live Gemini endpoint or a refactor that introduces an
   `IGeminiClient` abstraction.
3. **Repository implementations** — these live in the
   `Bloomdo.Server.Infrastructure` project. Their EF Core LINQ
   translations should be verified through integration tests
   against a real (or in-memory) database, not through this unit
   suite.
4. **API controllers** — the thin controllers in
   `Bloomdo.Server.Api` are best covered by HTTP-level integration
   tests using `WebApplicationFactory`, which exercises the entire
   pipeline (routing, model binding, authentication, validation).
5. **Client ViewModels and Avalonia views** — these belong to the
   client-side test strategy and would require Avalonia's headless
   UI infrastructure.

These exclusions are deliberate; they keep the unit suite fast,
isolated, and deterministic.

---

## 8. Maintenance Notes

A few non-obvious points worth recording for future contributors:

- **Stripe API version pinning.** The synthetic webhook JSON uses
  the API version string that the installed Stripe.net package
  expects (currently `2026-02-25.clover`). If Stripe.net is
  upgraded, the version string in
  `SubscriptionServiceTests.BuildCheckoutSessionCompletedJson`
  must be updated to match — otherwise the Stripe SDK will throw a
  version-mismatch exception.
- **Moq last-setup-wins.** When multiple `Setup` calls match the
  same parameters, Moq uses the most recently registered one. The
  `StatsServiceTests` build a sensible default freeze-list of `[]`
  in `BuildSut()`; tests that need a non-empty list must call
  `Setup(...)` **after** `BuildSut()`. The
  `GetMonthCalendar_FreezeDayMergesIntoStreak` test includes an
  inline comment explaining this.
- **Type-name clashes.** Stripe defines its own `Subscription` and
  `SubscriptionService` types in the global `Stripe` namespace.
  The test file `SubscriptionServiceTests.cs` does not
  `using Stripe;` directly; instead it uses a type alias
  (`using SubscriptionService = Bloomdo.Server.Application.Services.SubscriptionService;`)
  and qualifies any Stripe-specific reference (`Stripe.EventTypes.CheckoutSessionCompleted`)
  inline.

---

## 9. Summary

The unit-test suite for `Bloomdo.Server.Application` verifies the
core business logic of the Bloomdo backend with **65 isolated,
deterministic tests** that execute in approximately one second. The
suite uses industry-standard libraries (xUnit, Shouldly, Moq), follows
the Arrange–Act–Assert pattern, isolates each service through mocked
dependencies, and verifies both observable state and interaction
sequences. The chosen scope deliberately excludes Infrastructure and
API layers, which require different testing strategies.

The resulting confidence allows the developer to refactor and extend
the Application layer rapidly while preserving the correctness of the
critical user-facing behaviours: authentication, subscription
management, screen-time tracking, streak calculation, achievement
unlocking, AI chat usage, and photo verification.
