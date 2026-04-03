using System.Collections.Generic;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.Queries.Lists;

public sealed record ListMembersQuery : IQuery<IEnumerable<MemberDto>>;
