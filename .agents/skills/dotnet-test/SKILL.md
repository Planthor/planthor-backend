---
name: dotnet-test
description: Guidelines for writing automated tests using xUnit for the planthor-backend project.
---

# .NET 10 Testing Guidelines (xUnit)

You are a QA automation expert writing tests for `planthor-backend`.

## Core Framework & Structure
- **Framework:** Use **xUnit** (`[Fact]`, `[Theory]`, `[InlineData]`). Never use NUnit or MSTest.
- **Structure:** Strictly adhere to the AAA pattern (Arrange, Act, Assert). Leave an empty line between each phase in the code.
- **Naming:** Name test methods clearly using the pattern: `MethodName_StateUnderTest_ExpectedBehavior` (e.g., `CalculateTotal_WithEmptyCart_ReturnsZero()`).

## Testing Strategy & Priorities
- **High-Level Priority:** ALWAYS prioritize high-level automation tests (E2E Tests, API Tests, and Integration Tests) over low-level Unit Tests. We want to test behavior, not implementation details.
- **Integration Tests Default:** When asked to write a test, default to writing an Integration Test in the `tests/IntegrationTests` folder that exercises the full vertical slice (API -> MediatR -> Database) unless explicitly instructed otherwise.
- **Testcontainers:** Use `Testcontainers` to spin up real database instances during integration tests. Avoid using InMemory Database providers.
- **API Setup:** Use `WebApplicationFactory<Program>` to spin up the API in memory for E2E/API tests.

## Coverage Requirements
- **Coverage Thresholds:** Code must maintain a minimum of **80% line coverage** and **80% conditional logic (branch) coverage**.
- **Verification:** Ensure tests explore both the "happy path" and edge cases (especially `if/else` logic) to satisfy the 80% branch coverage requirement. (Note: Coverlet is used to enforce these metrics).

## Mocking & Fakes (For Unit Tests Only)
- If a low-level Unit Test is absolutely necessary, mock external dependencies using a mocking framework or hand-rolled fakes.
- Use `TimeProvider.Fake` (from `Microsoft.Extensions.TimeProvider.Testing`) when testing time-dependent logic.
