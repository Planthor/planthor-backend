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

## Test Type Definitions
- **Unit Tests:** Test a single class/method in pure isolation. Used exclusively for pure business logic in the `Domain` layer.
- **Integration Tests:** Test how two or more components work together across an I/O boundary (e.g., MediatR + DB). In this project, this concept is entirely merged into API Tests.
- **API Tests (Core Focus):** Vertical slice tests that start at the HTTP endpoint and go all the way down to a real database (via Testcontainers) and back. External HTTP boundaries should be mocked (e.g., with WireMock.Net).
- **E2E / Performance Tests:** Avoid writing these unless strictly required for a critical smoke test or SLA. Rely on API Tests for backend confidence.

## Testing Strategy & Priorities (Testing Honeycomb)
- **The Testing Honeycomb:** Shift away from the traditional testing pyramid. Favor a "Honeycomb" approach where the vast majority of tests are **API Tests and Integration Tests**.
- **High-Level Priority:** ALWAYS prioritize high-level automation tests over low-level Unit Tests for the Application, Infrastructure, and Api layers. We want to test behavior, HTTP routing, and database integrations, not just mock isolated implementation details.
- **API Tests Default:** When asked to write a test, default to writing an API Test in the `tests/ApiTests/Api.Tests` folder. These tests should exercise the full vertical slice (API -> MediatR -> Database) using `CustomWebApplicationFactory`.
- **Unit Tests for Domain Only:** Keep Unit Tests restricted mostly to the `Domain` layer, where business logic is pure and has no external dependencies. Avoid writing unit tests for MediatR handlers or Controllers.
- **Testcontainers:** Use `Testcontainers` to spin up real database instances (Postgres, Keycloak, etc.) during integration tests. Avoid using InMemory Database providers.
- **API Setup:** Use `WebApplicationFactory<Program>` (e.g., `CustomWebApplicationFactory`) to spin up the API in memory for E2E/API tests.

## Coverage Requirements
- **Coverage Thresholds:** Code must maintain a minimum of **80% line coverage** and **80% conditional logic (branch) coverage**.
- **Verification:** Ensure tests explore both the "happy path" and edge cases (especially `if/else` logic) to satisfy the 80% branch coverage requirement. (Note: Coverlet is used to enforce these metrics).

## Mocking & Fakes (For Unit Tests Only)
- If a low-level Unit Test is absolutely necessary, mock external dependencies using a mocking framework or hand-rolled fakes.
- Use `TimeProvider.Fake` (from `Microsoft.Extensions.TimeProvider.Testing`) when testing time-dependent logic.
