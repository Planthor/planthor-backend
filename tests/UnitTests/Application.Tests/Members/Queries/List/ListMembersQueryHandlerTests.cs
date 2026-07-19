using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Members.Queries.List;
using Application.Shared;
using Domain.Members;
using NSubstitute;
using NodaTime;

namespace Application.Tests.Members.Queries.List;

public class ListMembersQueryHandlerTests
{
    private readonly IReadOnlyContext _mockContext;
    private readonly ListMembersQueryHandler _handler;

    public ListMembersQueryHandlerTests()
    {
        _mockContext = Substitute.For<IReadOnlyContext>();
        _handler = new ListMembersQueryHandler(_mockContext);
    }

    private void SetupMembers(List<MemberDto> dtos)
    {
        _mockContext
            .QueryAsync<Member, MemberDto>(
                Arg.Any<Func<IQueryable<Member>, IQueryable<MemberDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(dtos);
    }

    [Fact]
    public async Task Handle_NoMembers_ReturnsEmptyList()
    {
        SetupMembers([]);

        var result = await _handler.Handle(new ListMembersQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_WithMembers_ReturnsAll()
    {
        var dtos = new List<MemberDto>
        {
            new(Guid.NewGuid(), "John", "", "Doe", null, ""),
            new(Guid.NewGuid(), "Jane", "", "Smith", null, "")
        };
        SetupMembers(dtos);

        var result = await _handler.Handle(new ListMembersQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        var ct = new CancellationToken(canceled: false);
        SetupMembers([]);

        await _handler.Handle(new ListMembersQuery(), ct);

        _mockContext.Received(1).QueryAsync<Member, MemberDto>(
                Arg.Any<Func<IQueryable<Member>, IQueryable<MemberDto>>>(),
                ct);
    }

    [Fact]
    public async Task Handle_ExecutesQueryLambda_ProjectsToDto()
    {
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 1, 1, 0, 0));
        var member1 = Member.Create("alice", "Alice", "", "Smith", "desc", "UTC", clock);
        var member2 = Member.Create("bob", "Bob", "A", "Jones", null!, "UTC", clock);

        _mockContext
            .QueryAsync<Member, MemberDto>(
                Arg.Any<Func<IQueryable<Member>, IQueryable<MemberDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(c => Task.FromResult(c.ArgAt<Func<IQueryable<Member>, IQueryable<MemberDto>>>(0)(new[] { member1, member2 }.AsQueryable()).ToList()));

        var result = (await _handler.Handle(new ListMembersQuery(), CancellationToken.None)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.FirstName == "Alice");
        Assert.Contains(result, r => r.FirstName == "Bob");
    }
}
