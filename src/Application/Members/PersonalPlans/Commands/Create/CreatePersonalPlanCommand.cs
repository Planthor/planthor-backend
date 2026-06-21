using System;
using Application.Shared;

namespace Application.Members.Commands.CreatePersonalPlan;

/// <summary>
///
/// </summary>
/// <param name="IdentifyName"></param>
/// <param name="Unit"></param>
/// <param name="FromDate"></param>
/// <param name="ToDate"></param>
/// <param name="Target"></param>
/// <param name="Current"></param>
public record CreatePlanCommand(
    string IdentifyName,
    string Unit,
    double Target,
    double Current,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate
) : ICommand<Guid>;
