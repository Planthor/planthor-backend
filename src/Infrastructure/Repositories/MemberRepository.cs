using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Members;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="Member"/> entities, providing data access tailored to member-specific queries.
/// </summary>
public sealed class MemberRepository(PlanthorDbContext context) : BaseRepository<Member>(context), IMemberRepository
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

    /// <inheritdoc />
    public async Task<Member?> GetByExternalIdentityAsync(string providerId, string externalUserId, CancellationToken cancellationToken)
    {
        var provider = ExternalProvider.FromId(providerId);
        return await Context.Members.FirstOrDefaultAsync(m => 
            m.ExternalConnections.Any(c => 
                c.Provider == provider && 
                c.Type == ExternalConnectionType.Identity && 
                c.ExternalUserId == externalUserId &&
                c.Status == ConnectionStatus.Active), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Member?> GetByActiveExternalConnectionAsync(
        string providerId,
        string connectionTypeId,
        string externalUserId,
        CancellationToken cancellationToken)
    {
        var provider = ExternalProvider.FromId(providerId);
        var connectionType = ExternalConnectionType.FromId(connectionTypeId);

        return await Context.Members.FirstOrDefaultAsync(member =>
            member.ExternalConnections.Any(connection =>
                connection.Provider == provider &&
                connection.Type == connectionType &&
                connection.ExternalUserId == externalUserId &&
                connection.Status == ConnectionStatus.Active),
            cancellationToken);
    }
}
