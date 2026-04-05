using System.Collections.Generic;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.Queries.List;

public sealed record ListMembersQuery : IQuery<IEnumerable<MemberDto>>;
