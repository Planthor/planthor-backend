using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Members.Queries.CheckExists;
using Application.Shared;
using Domain.Members;
using Moq;

namespace Application.Tests.Members.Queries.CheckExists;

public class CheckMemberExistsQueryHandlerTests
{
    private readonly Mock<IReadOnlyContext> _mockReadOnlyContext;
    private readonly CheckMemberExistsQueryHandler _handler;

    public CheckMemberExistsQueryHandlerTests()
    {
        _mockReadOnlyContext = new Mock<IReadOnlyContext>();
        _handler = new CheckMemberExistsQueryHandler(_mockReadOnlyContext.Object);
    }

    [Fact]
    public async Task Handle_WhenMemberDoesNotExist_ReturnsFalse()
    {
        var query = new CheckMemberExistsQuery("nonexistent_user");
        _mockReadOnlyContext
            .Setup(c => c.AnyAsync<Member>(
                It.IsAny<Func<IQueryable<Member>, IQueryable<Member>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.False(result);
        _mockReadOnlyContext.Verify(
            c => c.AnyAsync<Member>(
                It.IsAny<Func<IQueryable<Member>, IQueryable<Member>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToRepository()
    {
        var query = new CheckMemberExistsQuery("testuser");
        var cancellationToken = new CancellationToken();
        _mockReadOnlyContext
            .Setup(c => c.AnyAsync<Member>(
                It.IsAny<Func<IQueryable<Member>, IQueryable<Member>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _ = await _handler.Handle(query, cancellationToken);

        _mockReadOnlyContext.Verify(
            c => c.AnyAsync<Member>(
                It.IsAny<Func<IQueryable<Member>, IQueryable<Member>>>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMemberExists_ReturnsTrue()
    {
        var query = new CheckMemberExistsQuery("testuser123");
        _mockReadOnlyContext
            .Setup(c => c.AnyAsync<Member>(
                It.IsAny<Func<IQueryable<Member>, IQueryable<Member>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result);
    }
}


