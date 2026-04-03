# Unit Tests

Unit tests validate the behavior of individual units of code within the application. They ensure that each component functions correctly in isolation.

## Collect code coverage result with coverlet cli

```bash
coverlet tests/UnitTests/Application.Tests/bin/Debug/net10.0/Application.dll \
    --target "dotnet" \
    --targetargs "test --no-build" \
    -f="opencover" \
    -o="./coverage-report/coverage.xml"
```
