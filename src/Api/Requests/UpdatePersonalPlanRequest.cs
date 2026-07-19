using System;

namespace Api.Requests;

/// <summary>
/// Request model to update the details of a member's personal plan, such as its target, dates, and period type.
/// </summary>
public record UpdatePersonalPlanRequest(
    string Unit,
    double Target,
    double Current,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    string PeriodType
);
