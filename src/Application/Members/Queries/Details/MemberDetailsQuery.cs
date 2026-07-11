using System;
using Application.Dtos;
using Application.Shared;

namespace Application.Members.Queries.Details;

/// <summary>
/// Query to retrieve the detailed information of a specific member by their unique identifier.
/// </summary>
public sealed record MemberDetailsQuery(Guid Id) : IQuery<MemberDto>;
