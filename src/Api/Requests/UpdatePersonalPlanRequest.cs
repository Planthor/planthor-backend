using System;
using System.Text.Json.Serialization;

namespace Api.Requests;

/// <summary>
/// Request model to update the details of a member's personal plan, such as its target, dates, and period type.
/// </summary>
public record UpdatePersonalPlanRequest(
    string Unit,
    [property: JsonRequired] double Target,
    [property: JsonRequired] double Current,
    [property: JsonRequired] DateTimeOffset FromDate,
    [property: JsonRequired] DateTimeOffset ToDate,
    string PeriodType
);
