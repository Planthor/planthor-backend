# Unit Tests

Unit tests validate the behavior of individual units of code within the application. They ensure that each component functions correctly in isolation.

> **Note:** According to our "Testing Honeycomb" strategy, Unit Tests should be strictly reserved for testing pure business logic within the `Domain` layer. For testing `Application` (MediatR), `Infrastructure`, or `Adapters`, you should default to writing vertical slice API Tests in the `tests/ApiTests` project instead.

## Collect code coverage result with coverlet cli

```bash
coverlet tests/UnitTests/Application.Tests/bin/Debug/net10.0/Application.dll \
    --target "dotnet" \
    --targetargs "test --no-build" \
    -f="opencover" \
    -o="./coverage-report/coverage.xml"
```
