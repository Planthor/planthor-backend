using System;
using System.Collections.Generic;
using Application.Dtos;
using Application.ExternalSync.Commands.ProcessExternalActivitySync;

namespace Application.Tests.Dtos;

public class DtoConstructionTests
{
    [Fact]
    public void SportPlanDto_Construction_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var sportTypes = new List<string> { "Run", "Ride" };

        var dto = new SportPlanDto(
            id, memberId, "Run 100km", "km", 100f, 50f,
            now, now.AddDays(30),
            "2026-01-01", "2026-12-31",
            "UTC", true, "PlanStatus_Planned_Desc", 5, sportTypes.AsReadOnly());

        Assert.Equal(id, dto.Id);
        Assert.Equal(memberId, dto.MemberId);
        Assert.Equal("Run 100km", dto.Name);
        Assert.Equal("km", dto.Unit);
        Assert.Equal(100f, dto.Target);
        Assert.Equal(50f, dto.CurrentValue);
        Assert.Equal(now, dto.From);
        Assert.Equal(now.AddDays(30), dto.To);
        Assert.Equal("2026-01-01", dto.StartDateLocal);
        Assert.Equal("2026-12-31", dto.EndDateLocal);
        Assert.Equal("UTC", dto.Timezone);
        Assert.True(dto.EnableActivityLog);
        Assert.Equal("PlanStatus_Planned_Desc", dto.StatusI18nKey);
        Assert.Equal(5, dto.LikeCount);
        Assert.Equal(2, dto.SportTypes.Count);
    }

    [Fact]
    public void ActivitySyncOutcome_KnownAndUnknownIds_ReturnExpectedResults()
    {
        // Arrange
        ActivitySyncOutcome[] expectedOutcomes =
        [
            ActivitySyncOutcome.Success,
            ActivitySyncOutcome.NotFound,
            ActivitySyncOutcome.AuthorizationRequired,
            ActivitySyncOutcome.RateLimited,
            ActivitySyncOutcome.TransientFailure
        ];

        // Act
        var outcomes = ActivitySyncOutcome.All;
        var caseInsensitiveMatch = ActivitySyncOutcome.FromId("s");
        var exception = Assert.Throws<ArgumentException>(() => ActivitySyncOutcome.FromId("unknown"));

        // Assert
        Assert.Equal(expectedOutcomes, outcomes);
        Assert.Same(ActivitySyncOutcome.Success, caseInsensitiveMatch);
        Assert.Equal("S", ActivitySyncOutcome.Success.Id);
        Assert.Equal("SUCCESS", ActivitySyncOutcome.Success.Name);
        Assert.Contains("unknown", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessExternalActivitySyncResult_Construction_SetsAllProperties()
    {
        // Arrange
        var retryAt = NodaTime.Instant.FromUtc(2026, 1, 1, 0, 0);

        // Act
        var result = new ProcessExternalActivitySyncResult(
            3,
            retryAt,
            "retry_required");

        // Assert
        Assert.Equal(3, result.LogsCreated);
        Assert.Equal(retryAt, result.RetryAt);
        Assert.Equal("retry_required", result.ErrorCode);
    }

    [Fact]
    public void MemberDto_Construction_SetsAllProperties()
    {
        var id = Guid.NewGuid();

        var dto = new MemberDto(id, "Alice", "M", "Smith", "desc", "/avatar.jpg");

        Assert.Equal(id, dto.Id);
        Assert.Equal("Alice", dto.FirstName);
        Assert.Equal("M", dto.MiddleName);
        Assert.Equal("Smith", dto.LastName);
        Assert.Equal("desc", dto.Description);
        Assert.Equal("/avatar.jpg", dto.PathAvatar);
    }

    [Fact]
    public void MemberDto_RecordEquality_EqualDtosAreEqual()
    {
        var id = Guid.NewGuid();
        var dto1 = new MemberDto(id, "Alice", "", "Smith", null, "");
        var dto2 = new MemberDto(id, "Alice", "", "Smith", null, "");

        Assert.Equal(dto1, dto2);
    }
}
