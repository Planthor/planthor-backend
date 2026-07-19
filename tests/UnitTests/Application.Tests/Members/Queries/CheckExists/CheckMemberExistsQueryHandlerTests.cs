using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.Queries.CheckExists;
using Application.Shared;
using Domain.Members;
using NSubstitute;

namespace Application.Tests.Members.Queries.CheckExists;

public class CheckMemberExistsQueryHandlerTests
{
    private readonly IReadOnlyContext _mockReadOnlyContext;
    private readonly CheckMemberExistsQueryHandler _handler;

    public CheckMemberExistsQueryHandlerTests()
    {
        _mockReadOnlyContext = Substitute.For<IReadOnlyContext>();
        _handler = new CheckMemberExistsQueryHandler(_mockReadOnlyContext);
    }

    [Fact]
    public async Task Handle_WhenMemberDoesNotExist_ReturnsFalse()
    {
        var query = new CheckMemberExistsQuery("nonexistent_user");
        _mockReadOnlyContext
            .AnyAsync<Member>(
                Arg.Any<Func<IQueryable<Member>, IQueryable<Member>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.False(result);
        _mockReadOnlyContext.Received(1).AnyAsync<Member>(
                Arg.Any<Func<IQueryable<Member>, IQueryable<Member>>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToRepository()
    {
        var query = new CheckMemberExistsQuery("testuser");
        var cancellationToken = new CancellationToken();
        _mockReadOnlyContext
            .AnyAsync<Member>(
                Arg.Any<Func<IQueryable<Member>, IQueryable<Member>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        _ = await _handler.Handle(query, cancellationToken);

        _mockReadOnlyContext.Received(1).AnyAsync<Member>(
                Arg.Any<Func<IQueryable<Member>, IQueryable<Member>>>(),
                cancellationToken);
    }

    [Fact]
    public async Task Handle_WhenMemberExists_ReturnsTrue()
    {
        var query = new CheckMemberExistsQuery("testuser123");
        _mockReadOnlyContext
            .AnyAsync<Member>(
                Arg.Any<Func<IQueryable<Member>, IQueryable<Member>>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result);
    }
}


