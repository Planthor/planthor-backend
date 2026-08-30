using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;

namespace Application.Members.Queries.CheckExists;

/// <summary>
/// Handler for the <see cref="CheckMemberExistsQuery"/>.
/// </summary>
public sealed class CheckMemberExistsQueryHandler : IQueryHandler<CheckMemberExistsQuery, bool>
{
    private readonly IReadOnlyContext _readOnlyContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckMemberExistsQueryHandler"/> class.
    /// </summary>
    public CheckMemberExistsQueryHandler(IReadOnlyContext readOnlyContext)
    {
        ArgumentNullException.ThrowIfNull(readOnlyContext);
        _readOnlyContext = readOnlyContext;
    }

    /// <inheritdoc />
    public Task<bool> Handle(CheckMemberExistsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _readOnlyContext.AnyAsync<Member>(
            q => q.Where(m => m.IdentifyName == request.IdentifyName),
            cancellationToken);
    }
}
