namespace Api.Responses;

/// <summary>
/// Response model for a supported sport type.
/// </summary>
/// <param name="Id">The unique identifier of the sport type.</param>
/// <param name="Name">The display name of the sport type.</param>
public record SportTypeResponse(string Id, string Name);
