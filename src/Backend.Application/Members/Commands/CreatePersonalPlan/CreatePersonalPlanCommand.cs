using System;
using Backend.Application.Shared;

namespace Backend.Application.Members.Commands.CreatePersonalPlan;

/// <summary>
///
/// </summary>
/// <param name="MemberId"></param>
/// <param name="Unit"></param>
/// <param name="FromDate"></param>
/// <param name="ToDate"></param>
/// <param name="Target"></param>
/// <param name="Current"></param>
public record CreatePlanCommand(
    Guid MemberId,
    string Unit,
    double Target,
    double Current,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate
) : ICommand<Guid>;
