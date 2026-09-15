using System;
using Application.Shared;

namespace Application.Members.Queries.ResolveId;

/// <summary>
/// Resolves a member's stored identity name to the ID used by member commands and queries.
/// </summary>
/// <param name="IdentifyName">The exact stored identity name of the member.</param>
public sealed record ResolveMemberIdQuery(string IdentifyName) : IQuery<Guid>;
