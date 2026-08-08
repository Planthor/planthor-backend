using System.Collections.Generic;

namespace Application.Members.PersonalPlans.Commands.Create;

/// <summary>
/// Command for sport-specific plan details.
/// </summary>
/// <param name="SportTypes">A list of sport types (e.g., "Run", "Ride") associated with the plan.</param>
public record CreateSportPlanDetailsCommand(IReadOnlyList<string> SportTypes) : CreatePlanDetailsCommand;
