using System;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.Queries.ExternalConnections.Details;

/// <summary>
/// Query to retrieve details of a specific external connection for a member.
/// </summary>
/// <param name="Identifier">The member identifier, which can be '@me' or a valid GUID.</param>
/// <param name="CurrentIdentifyName">The current user's identify name.</param>
/// <param name="ConnectionId">The unique identifier of the external connection.</param>
public sealed record ExternalConnectionDetailsQuery(string Identifier, string CurrentIdentifyName, Guid ConnectionId) : IQuery<ExternalConnectionDto>;
