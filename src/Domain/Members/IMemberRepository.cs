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
    /// Checks if a member exists by their identify name.
    /// </summary>
    Task<bool> AnyAsync(string identifyName, CancellationToken cancellationToken);
}
