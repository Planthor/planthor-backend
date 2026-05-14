# Planthor Backend

[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=Planthor_PlanthorWebApi&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=Planthor_PlanthorWebApi)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=Planthor_PlanthorWebApi&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=Planthor_PlanthorWebApi)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=Planthor_PlanthorWebApi&metric=bugs)](https://sonarcloud.io/summary/new_code?id=Planthor_PlanthorWebApi)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=Planthor_PlanthorWebApi&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=Planthor_PlanthorWebApi)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Planthor_PlanthorWebApi&metric=coverage)](https://sonarcloud.io/summary/new_code?id=Planthor_PlanthorWebApi)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Planthor_PlanthorWebApi&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Planthor_PlanthorWebApi)

## 📋 Table of Contents

- [About Planthor](#about-planthor)
- [Architecture](#architecture)
- [Requirements](#requirements)
- [Getting Started](#getting-started)
  - [Windows](#windows)
  - [macOS & Linux](#macos--linux)
- [Running the Application](#running-the-application)
- [API Documentation](#api-documentation)
- [Running Tests](#running-tests)
- [External Integrations](#external-integrations)
- [Deployment](#deployment)
- [Contributing](#contributing)
- [Troubleshooting](#troubleshooting)
- [Resources](#resources)

---

## About Planthor

**Planthor** is a fitness planning and activity tracking backend API that enables users to create personalized training plans integrated with external fitness platforms. Users can synchronize their activities from popular fitness services like Strava and manage comprehensive training regimens.

### Key Features

- **User & Member Management** — Create and manage user profiles with role-based access
- **Training Plans** — Design personalized and sport-specific training plans
- **Activity Synchronization** — Sync activities from Strava and other fitness platforms
- **Social Integration** — Connect Facebook profiles and other social networks
- **Real-time Webhooks** — Receive real-time notifications from external services (e.g., Strava activity events)
- **JWT Authentication** — Secure API access with token-based authentication via Keycloak

For more features, see [Planthor Wiki - Features](https://github.com/Planthor/planthor-documentation/wiki/Features).

---

## Architecture

Planthor follows a **Clean Architecture** pattern with clear separation of concerns across five layers:

### Architecture Layers

| Layer | Responsibility | Location |
|-------|---|---|
| **API** | HTTP controllers, request/response handling, JWT authentication | `src/Api/Controllers/` |
| **Application** | Business logic using CQRS pattern (Commands/Queries via MediatR) | `src/Application/` |
| **Domain** | Core entities, domain events, value objects, business rules | `src/Domain/` |
| **Infrastructure** | Data persistence (MongoDB), repositories, external service clients | `src/Infrastructure/` |
| **Adapters** | Third-party integrations (Strava, Facebook, activity sync) | `src/Adapters/` |

### Technology Stack

| Component | Technology |
|---|---|
| **Framework** | .NET 10 with ASP.NET Core |
| **Database** | MongoDB 8.2 |
| **Authentication** | Keycloak 26.5 + JWT |
| **Identity DB** | PostgreSQL 16 |
| **Patterns** | CQRS (MediatR), Clean Architecture, Domain-Driven Design |
| **Logging** | Serilog (structured logging) |
| **API Documentation** | OpenAPI 3.0 + Scalar UI |
| **Validation** | FluentValidation |

### Project Structure

```
src/
├── Api/                    # REST API controllers (v1 endpoints)
├── Application/            # CQRS commands, queries, DTOs
├── Domain/                 # Core domain entities & business logic
├── Infrastructure/         # MongoDB, repositories, services
└── Adapters/               # External integrations (Strava, Facebook)

tests/
├── UnitTests/              # Domain & Application layer tests
└── IntegrationTests/       # API integration tests

infrastructure/
├── compose.yaml            # Docker Compose services
└── keycloak/realms/        # Keycloak realm configuration
```

---

## Requirements

### Prerequisites

- **.NET 10 SDK** or higher ([download](https://dotnet.microsoft.com/download))
- **Docker & Docker Compose** ([install](https://docs.docker.com/get-docker/))
- **Git**
- **Terminal/Command Prompt** (PowerShell on Windows, bash on macOS/Linux)

### Optional

- **MongoDB Compass** — GUI for MongoDB administration
- **pgAdmin Web UI** — Included in Docker Compose (port 5050)
- **Mongo Express Web UI** — Included in Docker Compose (port 8081)

---

## Getting Started

### Clone the Repository

```bash
git clone https://github.com/Planthor/PlanthorWebApi.git
cd PlanthorWebApi
```

### Windows

#### Step 1: Start Infrastructure Services

Open **PowerShell** and navigate to the project directory:

```powershell
cd .\infrastructure\
docker compose up --build -d
cd ..
```

This starts:
- **MongoDB** (port 27017)
- **Mongo Express** (port 8081) — database UI
- **Keycloak** (port 8180) — authentication server
- **PostgreSQL** (port 5432) — Keycloak database
- **pgAdmin** (port 5050) — PostgreSQL UI

#### Step 2: Set MongoDB Connection String

In PowerShell, set the connection string environment variable:

```powershell
$env:ConnectionStrings__PlanthorDbContext = "mongodb://admin:Planthor_123@localhost:27017/"
```

#### Step 3: Build & Run the Application

```powershell
dotnet build
dotnet run --project src/Api/Api.csproj
```

The API will start at `https://localhost:5001`. You'll see:
```
The app started.
```

#### Step 4: Access the API

- **Scalar UI (API Docs):** `https://localhost:5001/scalar/v1` *(development only)*
- **OpenAPI JSON:** `https://localhost:5001/openapi/v1.json`

---

### macOS & Linux

#### Step 1: Start Infrastructure Services

Open your terminal and navigate to the project directory:

```bash
cd infrastructure/
docker compose up --build -d
cd ..
```

#### Step 2: Set MongoDB Connection String

```bash
export ConnectionStrings__PlanthorDbContext="mongodb://admin:Planthor_123@localhost:27017/"
```

#### Step 3: Build & Run the Application

```bash
dotnet build
dotnet run --project src/Api/Api.csproj
```

The API will start at `https://localhost:5001`.

#### Step 4: Access the API

- **Scalar UI (API Docs):** `https://localhost:5001/scalar/v1`
- **OpenAPI JSON:** `https://localhost:5001/openapi/v1.json`

---

## Running the Application

### Development Mode with Hot-Reload

For rapid development iterations with automatic reloading on file changes:

#### Windows (PowerShell)

```powershell
$env:ConnectionStrings__PlanthorDbContext = "mongodb://admin:Planthor_123@localhost:27017/"
dotnet watch run --project src/Api/Api.csproj
```

#### macOS & Linux

```bash
export ConnectionStrings__PlanthorDbContext="mongodb://admin:Planthor_123@localhost:27017/"
dotnet watch run --project src/Api/Api.csproj
```

### Production Mode

Build an optimized release build:

```bash
dotnet publish src/Api/Api.csproj -c Release -o out
dotnet out/PlanthorBackend.dll
```

### Configuration via Environment Variables

The application reads configuration from:

1. **Environment Variables** (highest priority)
2. **appsettings.json** (default values)
3. **appsettings.Development.json** (development overrides)

Key environment variables:

| Variable | Example | Required |
|---|---|---|
| `ConnectionStrings__PlanthorDbContext` | `mongodb://admin:pass@localhost:27017/` | ✅ |
| `Authentication__Authority` | `http://localhost:8180/realms/planthor-realm` | ✅ (with auth) |
| `Authentication__Audience` | `planthor-backend` | ✅ (with auth) |
| `Authentication__RequireHttpsMetadata` | `false` (local dev), `true` (prod) | ❌ (default: true) |

---

## API Documentation

### Authentication

The API uses **JWT Bearer tokens** from Keycloak. All endpoints (except health checks) require authentication.

**Add a Bearer token to requests:**

```bash
curl -H "Authorization: Bearer <your-jwt-token>" https://localhost:5001/v1/members
```

### Main API Endpoints

#### Members

- `GET /v1/members/{id}` — Get member profile
- `POST /v1/members` — Create new member
- `PUT /v1/members/{id}` — Update member profile

#### Personal Plans

- `GET /v1/members/{memberId}/personalplans` — List member's training plans
- `POST /v1/members/{memberId}/personalplans` — Create new training plan
- `GET /v1/members/{memberId}/personalplans/{planId}` — Get specific plan
- `PUT /v1/members/{memberId}/personalplans/{planId}` — Update training plan

### Interactive API Documentation

**Scalar UI** (development environment only):

1. Navigate to `https://localhost:5001/scalar/v1`
2. Log in with Keycloak credentials (default: `admin` / `Planthor_123`)
3. Authorize the Scalar interface to obtain a JWT token
4. Test endpoints directly from the UI

**OpenAPI Specification:**

- JSON format: `https://localhost:5001/openapi/v1.json`
- Use with Postman, Insomnia, or other OpenAPI clients

---

## Running Tests

### Unit & Integration Tests

Run the complete test suite with code coverage:

```bash
dotnet test --results-directory ./tests/CodeCoverageResults --collect:"XPlat Code Coverage;Format=lcov,opencover"
```

### Test Structure

- **UnitTests/** — Domain & Application layer tests (fast, isolated)
  - `Domain.Tests/` — Domain entity and business logic tests
  - `Application.Tests/` — CQRS command/query handler tests
- **IntegrationTests/Api.Tests/** — Full API integration tests (slower, end-to-end)

### Code Coverage Report

After running tests, view the coverage report:

- **Summary:** `./tests/CodeCoverageResults/Summary.md`
- **Detailed Reports:** `./tests/CodeCoverageResults/`

---

## External Integrations

### Strava Integration

The Strava adapter syncs activities and processes webhook events in real-time.

**Local Development Note:** Strava webhooks require a publicly accessible URL. For localhost development:

1. Use **Cloudflare Tunnel** to expose your local API:
   ```bash
   cloudflared tunnel run --url http://localhost:5001
   ```

2. Register the tunnel URL with Strava as the webhook callback:
   ```
   https://<tunnel-id>.trycloudflare.com/v1/webhooks/strava
   ```

### Facebook Integration

The Facebook adapter connects user social profiles. Requires:

- Facebook App ID and Secret (in `appsettings.json`)
- User authorization via OAuth 2.0 flow

See [Adapters README](src/Adapters/README.md) for detailed setup.

---

## Deployment

### Docker Build & Run

Build a production Docker image:

```bash
docker build -t planthor-backend:latest .
```

Run the container:

```bash
docker run -d \
  -e ConnectionStrings__PlanthorDbContext="mongodb://admin:pass@mongodb:27017/" \
  -e Authentication__Authority="https://auth.example.com/realms/planthor-realm" \
  -e Authentication__Audience="planthor-backend" \
  -e Authentication__RequireHttpsMetadata="true" \
  -p 5001:5001 \
  planthor-backend:latest
```

### Cloud Service Configuration

To use hosted cloud services instead of local Docker services:

#### MongoDB (e.g., MongoDB Atlas)

```powershell
$env:ConnectionStrings__PlanthorDbContext = "mongodb+srv://username:password@cluster.mongodb.net/planthordb"
```

#### Keycloak (e.g., Keycloak Cloud or self-hosted)

```powershell
$env:Authentication__Authority = "https://auth.youromain.com/realms/planthor-realm"
$env:Authentication__Audience = "planthor-backend"
$env:Authentication__RequireHttpsMetadata = "true"
```

### Environment-Specific Configuration

Create environment-specific `appsettings` files:

- `appsettings.json` — Default/production values
- `appsettings.Development.json` — Local development overrides
- `appsettings.Staging.json` — Staging environment
- `appsettings.Production.json` — Production hardened config

Set the environment:

```bash
# Windows
$env:ASPNETCORE_ENVIRONMENT = "Production"

# macOS / Linux
export ASPNETCORE_ENVIRONMENT=Production
```

### Runtime Dependencies

Ensure these libraries are available in your deployment environment:

- ca-certificates
- libc6
- libgcc-s1
- libicu74
- liblttng-ust1
- libssl3
- libstdc++6
- libunwind8
- zlib1g

For Debian/Ubuntu:

```bash
sudo apt install ca-certificates libc6 libgcc-s1 libicu74 liblttng-ust1 libssl3 libstdc++6 libunwind8 zlib1g
```

---

## Contributing

### Development Workflow

1. **Create a feature branch:**
   ```bash
   git checkout -b feature/my-feature
   ```

2. **Make changes following Clean Architecture patterns:**
   - Domain changes go in `src/Domain/`
   - Business logic in `src/Application/`
   - API endpoints in `src/Api/Controllers/`
   - Data access in `src/Infrastructure/Repositories/`

3. **Write unit tests** for domain and application logic
4. **Write integration tests** for API endpoints

5. **Run tests locally:**
   ```bash
   dotnet test
   ```

6. **Push your branch and create a Pull Request**

### Code Quality Standards

- **Architecture:** Follow Clean Architecture with CQRS pattern
- **Testing:** All public methods should have corresponding unit tests
- **Code Coverage:** Maintain SonarCloud quality gates (see badges above)
- **Naming:** Use clear, descriptive names for classes, methods, and variables
- **Comments:** Document complex business logic and assumptions
- **Security:** No hardcoded secrets; use configuration/environment variables

### Build Tasks

VS Code includes pre-configured tasks:

```bash
# Build solution
dotnet build

# Publish for release
dotnet publish -c Release

# Watch mode (auto-rebuild on changes)
dotnet watch run
```

---

## Troubleshooting

### MongoDB Connection Failed

**Error:** `Unable to connect to MongoDB`

**Solution:**
1. Verify Docker containers are running: `docker ps`
2. Check MongoDB is healthy: `docker logs mongodb`
3. Verify connection string: `ConnectionStrings__PlanthorDbContext`
4. Ensure port 27017 is not blocked by firewall

### Keycloak Authentication Issues

**Error:** `Invalid Authority or Audience`

**Solution:**
1. Verify Keycloak is running: `http://localhost:8180`
2. Check realm configuration: `infrastructure/keycloak/realms/planthor-realm.json`
3. Ensure `Authentication:Authority` and `Authentication:Audience` are correct
4. For development, set `Authentication:RequireHttpsMetadata` to `false`

### Strava Webhook Not Receiving Events

**Error:** `Webhook callback not responding`

**Solution:**
1. Localhost is not publicly accessible; use Cloudflare Tunnel
2. Verify tunnel is running: `cloudflared tunnel run --url http://localhost:5001`
3. Register tunnel URL with Strava webhook configuration
4. Check API logs for webhook errors: `dotnet run` (verbose output)

### Port Already in Use

**Error:** `Address already in use`

**Solution:**
1. Find process using the port (Windows):
   ```powershell
   netstat -ano | findstr :5001
   taskkill /PID <pid> /F
   ```

2. macOS/Linux:
   ```bash
   lsof -i :5001
   kill -9 <pid>
   ```

3. Or change the port in `launchSettings.json`

---

## Resources

### Documentation

- **[Planthor Wiki](https://github.com/Planthor/planthor-documentation/wiki)** — Feature documentation and architectural decisions
- **[Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)** — Architectural pattern reference
- **[CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)** — Command Query Responsibility Segregation
- **[MediatR Documentation](https://github.com/jbogard/MediatR)** — In-process mediator library

### Project READMEs

Each layer includes detailed documentation:

- [Domain README](src/Domain/README.md)
- [Application README](src/Application/README.md)
- [Infrastructure README](src/Infrastructure/README.md)
- [Adapters README](src/Adapters/README.md)
- [API README](src/Api/README.md)

### External Services

- **[.NET Documentation](https://learn.microsoft.com/en-us/dotnet/)**
- **[Keycloak Documentation](https://www.keycloak.org/documentation.html)**
- **[MongoDB Documentation](https://docs.mongodb.com/)**
- **[Strava API Documentation](https://developers.strava.com/)**

### Code Quality

- **[SonarCloud Project](https://sonarcloud.io/project/overview?id=Planthor_PlanthorWebApi)** — Code quality metrics and analysis
- **Current Code Coverage:** See badge at top of this README
