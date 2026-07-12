# API Tests (Formerly Integration Tests)

In this project, we prioritize **API Tests** (Vertical Slices) over traditional Integration or Unit tests for external-facing layers. 

## Definitions & Strategy

- **API Tests:** This folder contains tests that exercise the full vertical slice of the application. They start with an HTTP Request to the API, flow through MediatR handlers, and persist/retrieve data using a real, containerized database (via Testcontainers).
- **Integration Tests:** The concept of testing components together across I/O boundaries is fully handled by our API Tests. We do not write isolated "Integration Tests" that only test one piece of the infrastructure; we test the whole slice.
- **Unit Tests:** Reserved *only* for pure business logic within the `Domain` layer (e.g., entity state changes).
- **E2E / Performance Tests:** Not maintained here. Rely on API tests for confidence in the backend's functional correctness.

## Best Practices

- **Testcontainers:** Always use Testcontainers for databases (e.g., MongoDB). Avoid `InMemoryDatabase`.
- **WireMock.Net:** Use WireMock to simulate external third-party HTTP dependencies (e.g., Google Identity, Strava API). Do not hit real external services in these tests.
- **CustomWebApplicationFactory:** Use the factory to spin up the API in memory and orchestrate the Testcontainers and WireMock instances.
