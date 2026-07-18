using System.Collections.Generic;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.Queries.ExternalConnections.List;

/// <summary>
/// Query to retrieve a list of external connections for a member.
/// </summary>
/// <param name="Identifier">The member identifier, which can be '@me' or a valid GUID.</param>
/// <param name="CurrentIdentifyName">The current user's identify name.</param>
public sealed record ListExternalConnectionsQuery(string Identifier, string CurrentIdentifyName) : IQuery<IEnumerable<ExternalConnectionDto>>;
