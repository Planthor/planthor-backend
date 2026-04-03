using System;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.Queries.Details;

public sealed record MemberDetailsQuery(Guid Id) : IQuery<MemberDto>;
