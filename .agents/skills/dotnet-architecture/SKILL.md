---
name: dotnet-architecture
description: Guidelines and strict rules for applying Clean Architecture and Domain-Driven Design (DDD) in the .NET planthor-backend project.
---

# .NET Clean Architecture & DDD Guidelines

This skill defines the architectural rules for `planthor-backend`. It enforces Clean Architecture principles combined with Domain-Driven Design (DDD) patterns. 

## 1. Core Principles
* **The Dependency Rule:** Source code dependencies must point **inward** toward the `Domain`. 
* **Separation of Concerns:** Keep business logic completely independent of UI, databases, frameworks, and external agencies.
* **Ignorance:** The `Domain` layer must be entirely ignorant of persistence or UI concerns.

## 2. Project Structure & Dependencies
The solution is divided into the following layers in the `src/` directory. Strict dependency enforcement must be maintained.

### `Domain` (Core)
* **Dependencies:** None. This project cannot reference any other project.
* **Contains:** Entities, Aggregates, Value Objects, Domain Events, Enums, and Custom Exceptions.
* **Rules:**
  * No persistence-related attributes (e.g., `[Table]`, `[Column]`).
  * Behavior-rich models: Entities should have methods to mutate their state, rather than public setters.

### `Application` (Use Cases)
* **Dependencies:** `Domain`
* **Contains:** Interfaces (Repositories, External Services), CQRS Models (Commands/Queries), Command/Query Handlers, DTOs, and Validators.
* **Rules:**
  * Uses **CQRS** (typically via MediatR). Separate read operations (Queries) from write operations (Commands).
  * Use **FluentValidation** for validating Commands/Queries before they reach the handler.
  * Define interfaces for everything that requires external interaction (e.g., `IUserRepository`, `IEmailService`).

### `Infrastructure`
* **Dependencies:** `Application`, `Domain`
* **Contains:** Entity Framework Core DbContext, Migrations, Repository Implementations, External API Clients.
* **Rules:**
  * Implements the interfaces defined in the `Application` layer.
  * EF Core configurations should be done using `IEntityTypeConfiguration` (Fluent API) rather than data annotations in the Domain.
  * Contains infrastructure-specific exception handling and mapping.

### `Adapters`
* **Dependencies:** `Application`, `Domain` (and external integrations)
* **Contains:** Integration points for message brokers (e.g., RabbitMQ, Kafka), specific third-party SDK wrappers.
* **Rules:**
  * Translates external contracts into application-friendly formats.

### `Api` (Presentation)
* **Dependencies:** `Application`, `Infrastructure` (only for Dependency Injection setup)
* **Contains:** ASP.NET Core Controllers or Minimal APIs, Middleware, Program.cs.
* **Rules:**
  * **Thin Controllers/Endpoints:** Controllers should only route HTTP requests to the Application layer (e.g., via `ISender` / MediatR) and return HTTP responses based on the result. No business logic here.
  * Translates Application layer exceptions into appropriate HTTP status codes (e.g., NotFoundException -> 404).

## 3. DDD Specifics
* **Aggregates:** Group related entities into an Aggregate. Modifications to the Aggregate should only occur through the Aggregate Root.
* **Value Objects:** Use for types that are defined by their values, not identity (e.g., `Money`, `Address`). Value Objects must be immutable.
* **Domain Events:** Use domain events to express side effects explicitly. Entities can raise events that are later published to handlers (e.g., after saving to the DB).
* **Primitive IDs:** Use primitive types (`Guid` for standard entities, `string` for enum-like objects) for entity identifiers. Avoid strongly-typed IDs to minimize serialization complexity with the EF Core MongoDB provider.

## 4. Best Practices & Naming Conventions
* **Handlers:** Name command/query handlers clearly, e.g., `CreateUserCommandHandler`.
* **Repositories:** Keep repositories simple. Return Aggregates, not partial models. (e.g., `Task<User?> GetByIdAsync(UserId id)`).
* **Mapping:** Prefer manual mapping or use tools like Mapster/AutoMapper strictly at the boundary (e.g., API to Application, Application to Domain).
* **Asynchronous:** Always use `async/await` for I/O operations (Database, External APIs).
* **Error Handling & i18n:** 
  * Internationalization (i18n) must be handled entirely by the client-side, as they hold the user's location preferences.
  * The API must NOT return localized strings or human-readable messages meant for end-users. 
  * Instead, the API must return structured responses containing explicit `ErrorCode`, `MessageCode`, `Status`, and `TypeCode` properties. The client will use these codes to resolve the correct localized string.

## 5. Testing Strategy (The Honeycomb)
* **API/Integration Tests Preferred:** Favor the "Testing Honeycomb" approach over the traditional testing pyramid. The vast majority of automated tests should be API and Integration tests that execute the full vertical slice (API endpoint -> MediatR Handler -> DB) using `WebApplicationFactory` and `Testcontainers`.
* **Unit Tests for Domain:** Limit Unit Testing primarily to the `Domain` layer, where business logic is pure and independent.
* **Avoid Handlers/Controller Unit Tests:** Do not write isolated unit tests for `Api` Controllers or `Application` MediatR Handlers, as this leads to fragile tests coupled to implementation details rather than observable behavior.
