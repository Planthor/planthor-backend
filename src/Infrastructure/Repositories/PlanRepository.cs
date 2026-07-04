using Domain.Plans;
using Infrastructure.Context;

namespace Infrastructure.Repositories;

public class PlanRepository(PlanthorDbContext context) : BaseRepository<Plan>(context), IPlanRepository
{
}
