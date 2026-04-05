using Application.Shared;

namespace Application.Members.Queries.CheckExists;

/// <summary>
///
/// </summary>
/// <param name="IdentifyName"></param>
public sealed record CheckMemberExistsQuery(string IdentifyName) : IQuery<bool>;
