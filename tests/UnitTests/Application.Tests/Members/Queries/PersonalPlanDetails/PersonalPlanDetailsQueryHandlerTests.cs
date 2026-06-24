using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Members.Queries.PersonalPlanDetails;
using Application.Shared;
using Domain.Members;
using Moq;

namespace Application.Tests.Members.Queries.PersonalPlanDetails;

public class PersonalPlanDetailsQueryHandlerTests
{
    private readonly Mock<IReadOnlyContext> _mockContext;
    private readonly PersonalPlanDetailsQueryHandler _handler;

    public PersonalPlanDetailsQueryHandlerTests()
    {
        _mockContext = new Mock<IReadOnlyContext>();
        _handler = new PersonalPlanDetailsQueryHandler(_mockContext.Object);
    }

    private void SetupContext(PersonalPlanDto? dto)
    {
        _mockContext
            .Setup(c => c.FirstOrDefaultAsync<Member, PersonalPlanDto?>(
                It.IsAny<Func<IQueryable<Member>, IQueryable<PersonalPlanDto?>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
    }

    [Fact]
    public async Task Handle_WhenPlanFound_ReturnsPersonalPlanDto()
    {
        var planId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var expected = new PersonalPlanDto(planId, memberId, true, 3, false);

        SetupContext(expected);

        var result = await _handler.Handle(
            new PersonalPlanDetailsQuery("user1", planId),
            CancellationToken.None);

        Assert.Equal(expected.PlanId, result.PlanId);
        Assert.Equal(expected.MemberId, result.MemberId);
        Assert.Equal(expected.DisplayOnProfile, result.DisplayOnProfile);
        Assert.Equal(expected.Prioritize, result.Prioritize);
        Assert.Equal(expected.LinkUserAdapters, result.LinkUserAdapters);
    }

    [Fact]
    public async Task Handle_WhenPlanNotFound_ThrowsKeyNotFoundException()
    {
        var planId = Guid.NewGuid();
        SetupContext(null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(new PersonalPlanDetailsQuery("user1", planId), CancellationToken.None));

        Assert.Contains(planId.ToString(), ex.Message);
        Assert.Contains("user1", ex.Message);
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToContext()
    {
        var cancellationToken = new CancellationToken(canceled: false);
        SetupContext(null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(new PersonalPlanDetailsQuery("user1", Guid.NewGuid()), cancellationToken));

        _mockContext.Verify(
            c => c.FirstOrDefaultAsync<Member, PersonalPlanDto?>(
                It.IsAny<Func<IQueryable<Member>, IQueryable<PersonalPlanDto?>>>(),
                cancellationToken),
            Times.Once);
    }
}
