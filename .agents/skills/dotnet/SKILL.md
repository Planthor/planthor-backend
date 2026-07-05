---
name: dotnet
description: Core guidelines for writing C# 14 and .NET 10 code in the planthor-backend project.
---

# .NET 10 & C# 14 Development Guidelines

You are an expert .NET software architect working on `planthor-backend`.

## Core Language Rules
- **Modern C#:** Always use the latest C# 14 / .NET 10 features.
- **Primary Constructors:** Use primary constructors for dependency injection in classes instead of explicit constructor bodies.
- **Nullability:** Nullable reference types are enabled. Treat all compiler warnings as errors. Use `?` appropriately and avoid `!` (null-forgiving operator) unless absolutely necessary.
- **Collection Expressions:** Use `[]` instead of `new List<T>()` or `Array.Empty<T>()`.
- **Async/Await:** All I/O operations MUST be asynchronous. Suffix methods with `Async` and ALWAYS pass `CancellationToken cancellationToken`.

## Documentation & XML Comments
- **Mandatory Documentation:** ALL public-facing components (classes, interfaces, methods, properties, records, and primary constructors) MUST have dedicated XML comments.
- **Standards:** Use standard tags (`<summary>`, `<param>`, `<returns>`, `<remarks>`, `<exception>`). The comments must be descriptive, clearly explaining the *why* and the *what*, not just restating the method name.
- **OpenAPI & Scalar:** For API controllers, use these XML comments in combination with `[ProducesResponseType]` and relevant OpenAPI attributes. Ensure all XML documentation is fully compatible with **Scalar** (`Scalar.AspNetCore`) for beautiful, modern API reference generation.

## Clean Architecture & MediatR Rules
- **API Layer:** Use API Controllers (inheriting from `ControllerBase` and decorated with `[ApiController]`) in the `src/Api/Controllers` directory. The controller should ONLY dispatch requests via MediatR (e.g., `_sender.Send(command)`). Do not put business logic in endpoints.
- **Application Layer:** Contains MediatR `IRequestHandler` implementations. This layer defines interfaces (e.g., `IRepository`) but NEVER implements them.
- **Domain Layer:** Contains pure C# entities, value objects, and domain events. NEVER reference Entity Framework, UI, or external packages here.
- **Infrastructure Layer:** Implements data access (EF Core) and external services. 

## Prohibited Patterns
- NEVER inject `DbContext` directly into a MediatR handler or API endpoint. Use the Repository pattern.
- NEVER use `DateTime.Now`. Use `TimeProvider.System` for testability.
