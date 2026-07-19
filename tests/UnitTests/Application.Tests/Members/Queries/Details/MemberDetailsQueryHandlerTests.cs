using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Members.Queries.Details;
using Application.Shared;
using Domain.Members;
using NSubstitute;
using NodaTime;

namespace Application.Tests.Members.Queries.Details;

public class MemberDetailsQueryHandlerTests
{
    private readonly IReadOnlyContext _mockContext;
    private readonly MemberDetailsQueryHandler _handler;

    public MemberDetailsQueryHandlerTests()
    {
        _mockContext = Substitute.For<IReadOnlyContext>();
        _handler = new MemberDetailsQueryHandler(_mockContext);
    }

    private void SetupContext(MemberDto? dto)
    {
        _mockContext
            .FirstOrDefaultAsync<Member, MemberDto>(
                Arg.Any<Func<IQueryable<Member>, IQueryable<MemberDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(dto));
    }

    [Fact]
    public async Task Handle_MemberFound_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var dto = new MemberDto(id, "John", "", "Doe", null, "");
        SetupContext(dto);

        var result = await _handler.Handle(new MemberDetailsQuery(id), CancellationToken.None);

        Assert.Equal(dto.Id, result.Id);
        Assert.Equal("John", result.FirstName);
    }

    [Fact]
    public async Task Handle_MemberNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        SetupContext(null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(new MemberDetailsQuery(id), CancellationToken.None));

        Assert.Contains(id.ToString(), ex.Message);
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        var ct = new CancellationToken(canceled: false);
        SetupContext(null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(new MemberDetailsQuery(Guid.NewGuid()), ct));

        _mockContext.Received(1).FirstOrDefaultAsync<Member, MemberDto>(
                Arg.Any<Func<IQueryable<Member>, IQueryable<MemberDto>>>(),
                ct);
    }

    [Fact]
    public async Task Handle_ExecutesQueryLambda_ProjectsToDto()
    {
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 1, 1, 0, 0));
        var member = Member.Create("john", "John", "M", "Doe", "desc", "UTC", clock);

        _mockContext
            .FirstOrDefaultAsync<Member, MemberDto>(
                Arg.Any<Func<IQueryable<Member>, IQueryable<MemberDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(c => Task.FromResult<MemberDto?>(c.ArgAt<Func<IQueryable<Member>, IQueryable<MemberDto>>>(0)(new[] { member }.AsQueryable()).FirstOrDefault()));

        var result = await _handler.Handle(new MemberDetailsQuery(member.Id), CancellationToken.None);

        Assert.Equal(member.Id, result.Id);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("M", result.MiddleName);
        Assert.Equal("Doe", result.LastName);
    }
}
