using System.Collections.Generic;

namespace Application.Members.PersonalPlans.Commands.Create;

/// <summary>
/// Command for sport-specific plan details.
/// </summary>
/// <param name="SportTypes">
/// The canonical Planthor sport-type identifiers associated with the plan,
/// such as <c>RUN</c> or <c>RIDE</c>. <c>ALL</c> must be used alone.
/// </param>
public record CreateSportPlanDetailsCommand(IReadOnlyList<string> SportTypes) : CreatePlanDetailsCommand;
