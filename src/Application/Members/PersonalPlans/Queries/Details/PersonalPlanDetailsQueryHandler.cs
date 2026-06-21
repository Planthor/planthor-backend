using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.Shared;
using Domain.Members;

namespace Application.Members.Queries.PersonalPlanDetails;

/// <summary>
/// Handler for retrieving details of a specific personal plan.
/// </summary>
public class PersonalPlanDetailsQueryHandler(IReadOnlyContext readOnlyContext)
    : IQueryHandler<PersonalPlanDetailsQuery, PersonalPlanDto>
{
    /// <inheritdoc />
    public async Task<PersonalPlanDto> Handle(PersonalPlanDetailsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var personalPlanDto = await readOnlyContext.FirstOrDefaultAsync<Member, PersonalPlanDto?>(
            q => q.Where(m => m.IdentifyName == request.IdentifyName)
                .Select(m => m.PersonalPlans
                    .Where(p => p.PlanId == request.PlanId)
                    .Select(p => new PersonalPlanDto(
                        p.PlanId,
                        p.MemberId,
                        p.DisplayOnProfile,
                        p.Prioritize,
                        p.LinkUserAdapter
                    ))
                    .FirstOrDefault()),
            cancellationToken);

        if (personalPlanDto == null)
        {
            throw new KeyNotFoundException($"Personal plan with PlanID '{request.PlanId}' for member '{request.IdentifyName}' was not found.");
        }

        return personalPlanDto;
    }
}
