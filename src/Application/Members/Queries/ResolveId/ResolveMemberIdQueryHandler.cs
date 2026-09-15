using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;

namespace Application.Members.Queries.ResolveId;

/// <summary>
/// Looks up a member by identity name without changing their profile or identity.
/// </summary>
/// <param name="readOnlyContext">The context used to look up the member.</param>
public sealed class ResolveMemberIdQueryHandler(IReadOnlyContext readOnlyContext)
    : IQueryHandler<ResolveMemberIdQuery, Guid>
{
    private readonly IReadOnlyContext _readOnlyContext = readOnlyContext
        ?? throw new ArgumentNullException(nameof(readOnlyContext));

    /// <inheritdoc />
    public async Task<Guid> Handle(ResolveMemberIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var member = await _readOnlyContext.FirstOrDefaultAsync<Member, Member>(
            query => query.Where(member => member.IdentifyName == request.IdentifyName),
            cancellationToken);

        return member?.Id ?? throw new KeyNotFoundException("Member was not found.");
    }
}
