using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Members;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="Member"/> entities, providing data access tailored to member-specific queries.
/// </summary>
public class MemberRepository(PlanthorDbContext context) : BaseRepository<Member>(context), IMemberRepository
{
    /// <inheritdoc />
    public async Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Context.Members.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Member?> GetByIdentifyNameAsync(string identifyName, CancellationToken cancellationToken)
    {
        return await Context.Members.FirstOrDefaultAsync(m => m.IdentifyName == identifyName, cancellationToken);
    }
}
