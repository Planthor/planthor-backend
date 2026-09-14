using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services;

/// <summary>
/// Data transfer object representing a linked federated identity in Keycloak.
/// </summary>
public record FederatedIdentityDto(string IdentityProvider, string UserId, string UserName);

/// <summary>
/// Lightweight client for interacting with the Keycloak Admin REST API.
/// </summary>
public interface IKeycloakAdminClient
{
    /// <summary>
    /// Retrieves all federated identities linked to a specific user.
    /// </summary>
    /// <param name="subjectId">The Keycloak subject ID stored in the member's external identity connection.</param>
    /// <returns>A list of federated identities linked to the user.</returns>
    Task<List<FederatedIdentityDto>> GetUserFederatedIdentitiesAsync(string subjectId);

    /// <summary>
    /// Retrieves all federated identities linked to a specific user.
    /// </summary>
    /// <param name="subjectId">The Keycloak subject ID stored in the member's external identity connection.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of federated identities linked to the user.</returns>
    Task<List<FederatedIdentityDto>> GetUserFederatedIdentitiesAsync(string subjectId, CancellationToken cancellationToken);
}
