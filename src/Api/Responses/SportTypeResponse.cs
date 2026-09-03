namespace Api.Responses;

/// <summary>
/// Describes a canonical sport type supported by Planthor.
/// </summary>
/// <param name="Id">
/// The stable, uppercase identifier clients use in sport-plan requests.
/// </param>
/// <param name="Name">
/// The non-localized display name of the sport type. Clients must not use this value as an identifier.
/// </param>
public record SportTypeResponse(string Id, string Name);
