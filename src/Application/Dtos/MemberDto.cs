using System;

namespace Application.Dtos;

public record MemberDto(
    Guid Id,
    string FirstName,
    string MiddleName,
    string LastName,
    string? Description,
    string PathAvatar);
