using System;
using System.Text.Json.Serialization;

namespace Api.Requests;

/// <summary>
/// Request model to update the details of a member's personal plan, such as its target and dates.
/// </summary>
public record UpdatePersonalPlanRequest(
    string Unit,
    [property: JsonRequired] double Target,
    [property: JsonRequired] DateTimeOffset FromDate,
    [property: JsonRequired] DateTimeOffset ToDate
);
