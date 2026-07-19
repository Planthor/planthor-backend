# Testing

Run all projects in the solution:

```bash
dotnet test PlanthorBackend.slnx
```

Run one test project:

```bash
dotnet test tests/UnitTests/Application.Tests/Application.Tests.csproj
```

Run a focused test class:

```bash
dotnet test tests/UnitTests/Application.Tests/Application.Tests.csproj \
  --filter FullyQualifiedName~CreateMemberCommandHandlerTests
```

## Test layout

| Tests | Location | Scope |
| --- | --- | --- |
| API tests | `tests/ApiTests/Api.Tests` | Full vertical slices using `WebApplicationFactory`, Testcontainers, and mocked external HTTP boundaries. |
| Domain tests | `tests/UnitTests/Domain.Tests` | Pure domain behavior. |
| Other unit-test projects | `tests/UnitTests/{Application,Infrastructure,Adapters}.Tests` | Existing targeted coverage for those layers. Prefer API tests for new behavior that crosses boundaries. |

## Strategy

The project follows a testing-honeycomb approach: prefer API tests that exercise the
observable vertical slice (HTTP endpoint through persistence) over isolated handler
or controller tests. Keep unit tests focused on pure domain behavior. New tests use
xUnit and should cover both expected behavior and meaningful failure paths.
