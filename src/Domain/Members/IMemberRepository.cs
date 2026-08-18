using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Shared;

namespace Domain.Members;

/// <summary>
///
/// </summary>
public interface IMemberRepository : IWriteRepository<Member>
{
    /// <summary>
    /// Gets a member by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the member.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the member, or null if not found.</returns>
    Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a member by their external identity name.
    /// </summary>
    /// <param name="identifyName">The external identity name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the member, or null if not found.</returns>
    Task<Member?> GetByIdentifyNameAsync(string identifyName, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a member by their external identity connection.
    /// </summary>
    /// <param name="providerId">The external provider ID.</param>
    /// <param name="externalUserId">The external user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the member, or null if not found.</returns>
    Task<Member?> GetByExternalIdentityAsync(string providerId, string externalUserId, CancellationToken cancellationToken);
}
