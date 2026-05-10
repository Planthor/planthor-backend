# Planthor Backend — Agent Notes

## Project snapshot
- Backend service built with .NET 10 and Clean Architecture style.
- Core layers: `Api`, `Application`, `Domain`, `Infrastructure`.
- External integrations live under `src/Adapters` (currently Facebook and Strava).
- Primary data store: MongoDB.
- Auth provider for local/dev flow: Keycloak (JWT bearer).

## Solution layout
- `/home/runner/work/planthor-backend/planthor-backend/src/Api`  
  HTTP controllers, auth setup, filters, OpenAPI/Scalar setup.
- `/home/runner/work/planthor-backend/planthor-backend/src/Application`  
  Commands, queries, handlers, validators (MediatR + FluentValidation).
- `/home/runner/work/planthor-backend/planthor-backend/src/Domain`  
  Business entities/events/value objects.
- `/home/runner/work/planthor-backend/planthor-backend/src/Infrastructure`  
  Mongo EF context, repositories, Quartz job client.
- `/home/runner/work/planthor-backend/planthor-backend/src/Adapters`  
  Provider-facing adapter modules.
- `/home/runner/work/planthor-backend/planthor-backend/tests`  
  Unit + integration tests.

## API overview (current)
- `v1/members`  
  create, update, list, details.
- `v1/members/{memberId}/personalplans`  
  create, update, list, details.
- `Strava/webhook`, `Strava/sync`  
  endpoints exist, but currently throw `NotSupportedException`.

## Local development quick start
1. Start infrastructure:
   - `docker compose -f /home/runner/work/planthor-backend/planthor-backend/infrastructure/compose.yaml up -d`
2. Configure API connection string (`ConnectionStrings:PlanthorDbContext`).
3. Run API from `/home/runner/work/planthor-backend/planthor-backend/src/Api`.
4. In development, OpenAPI and Scalar docs are enabled.

## Testing and CI
- Local tests:
  - `dotnet test --results-directory ./tests/CodeCoverageResults --collect:"XPlat Code Coverage;Format=lcov,opencover"`
- CI workflows:
  - `/home/runner/work/planthor-backend/planthor-backend/.github/workflows/dotnet-ci.yml`
  - `/home/runner/work/planthor-backend/planthor-backend/.github/workflows/qac.yml`
  - `/home/runner/work/planthor-backend/planthor-backend/.github/workflows/docker-build.yml`

## Observed notes / follow-up candidates
- Runtime target is .NET 10, but one CI workflow currently uses .NET 8.
- README clone URL still references previous repository naming (`PlanthorWebApi`).
- Strava adapter controllers are scaffolded and ready for implementation work.

## Suggested next development sequence
1. Align docs + CI toolchain versioning.
2. Implement Strava webhook/sync flow in adapter + application layers.
3. Add/expand integration tests for auth/session provisioning behavior.
4. Harden API contracts and error responses.
5. Add health checks and deployment profile refinements.
