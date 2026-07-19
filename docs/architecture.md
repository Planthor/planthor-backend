# Architecture

Planthor Backend follows Clean Architecture with CQRS and domain-driven design.
Dependencies point inward: outer layers may depend on inner layers, but the Domain
does not depend on persistence, HTTP, or third-party SDKs.

```text
HTTP client
    |
    v
Api --> Application --> Domain
 |          ^
 v          |
Infrastructure --------+
Adapters --> Application / Domain
```

## Layers

| Layer | Location | Responsibility |
| --- | --- | --- |
| API | `src/Api` | ASP.NET Core controllers, authentication, exception handling, OpenAPI and dependency injection. |
| Application | `src/Application` | CQRS commands and queries, MediatR handlers, validators, DTOs, and interfaces for external concerns. |
| Domain | `src/Domain` | Aggregates, entities, value objects, domain events, and business rules. |
| Infrastructure | `src/Infrastructure` | MongoDB context/configuration, repository implementations, storage, and service clients. |
| Adapters | `src/Adapters` | Translators for specific external integrations such as Facebook and Strava. |

## Request flow

1. A controller validates an HTTP request and sends a command or query through MediatR.
2. An Application handler coordinates the use case through Domain behavior and repository interfaces.
3. Infrastructure implements the interfaces and persists aggregates or calls external services.
4. The controller maps the outcome to an HTTP response. API error responses use structured codes; clients localize messages.

## Design rules

- Controllers remain thin: no business or persistence logic.
- MediatR handlers use repository abstractions, never a `DbContext` directly.
- Domain models expose behavior rather than public state setters.
- I/O is asynchronous and accepts a `CancellationToken`.
- Time-dependent behavior uses `IClock`/`TimeProvider`, not `DateTime.Now`.
