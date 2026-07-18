using System;
using NodaTime;

namespace Application.Dtos;

/// <summary>
/// Data transfer object representing an external connection.
/// </summary>
/// <param name="Id">The unique identifier of the connection.</param>
/// <param name="MemberId">The unique identifier of the member.</param>
/// <param name="ProviderId">The identifier of the external provider.</param>
/// <param name="TypeId">The identifier of the external connection type.</param>
/// <param name="ExternalUserId">The external user ID.</param>
/// <param name="StatusId">The status identifier.</param>
/// <param name="ConnectedAt">The instant the connection was established.</param>
/// <param name="DisconnectedAt">The instant the connection was disconnected, if any.</param>
public record ExternalConnectionDto(
    Guid Id,
    Guid MemberId,
    string ProviderId,
    string TypeId,
    string ExternalUserId,
    string StatusId,
    Instant ConnectedAt,
    Instant? DisconnectedAt);
