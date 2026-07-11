using System.Collections.Generic;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.Queries.List;

/// <summary>
/// Query to retrieve a paginated or full list of members in the system.
/// </summary>
public sealed record ListMembersQuery : IQuery<IEnumerable<MemberDto>>;
