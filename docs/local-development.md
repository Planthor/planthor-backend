# Local development

## Prerequisites

- .NET 10 SDK
- Docker and Docker Compose
- Azure CLI, only when using `start-infra.sh` to retrieve the local Facebook integration secret from Azure Key Vault

## Configuration

The API reads its development defaults from `src/Api/appsettings.Development.json`.
Do not commit credentials. Supply secrets with .NET user secrets, for example:

```bash
dotnet user-secrets set --project src/Api/Api.csproj "Storage:Azure:ConnectionString" "<connection-string>"
dotnet user-secrets set --project src/Api/Api.csproj "MediatR:LicenseKey" "<license-key>"
```

Environment variables can override settings with double underscores. The most useful
local override is:

```bash
export ConnectionStrings__PlanthorDbContext="mongodb://localhost:27017/?directConnection=true"
```

## Start local infrastructure

The infrastructure compose file starts a MongoDB replica set, Keycloak, PostgreSQL,
Mongo Express, and pgAdmin. The supported setup script templates the Keycloak realm
and starts the compose stack:

```bash
az login
./start-infra.sh
```

The script requires access to the configured Azure Key Vault. See
[`infrastructure/README.md`](../infrastructure/README.md) for the infrastructure
configuration and port details.

## Run the API

```bash
dotnet run --project src/Api/Api.csproj
```

For hot reload:

```bash
dotnet watch --project src/Api/Api.csproj run
```

The default launch profile listens on `https://localhost:7259` and
`http://localhost:5008`. In Development, the API exposes the health endpoint at
`/v1/healthz`, OpenAPI at `/openapi/v1.json`, and Scalar at `/scalar/v1`.
