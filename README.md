# PlanthorResourceApi

[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=Planthor_PlanthorWebApi&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=Planthor_PlanthorWebApi)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=Planthor_PlanthorWebApi&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=Planthor_PlanthorWebApi)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=Planthor_PlanthorWebApi&metric=bugs)](https://sonarcloud.io/summary/new_code?id=Planthor_PlanthorWebApi)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=Planthor_PlanthorWebApi&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=Planthor_PlanthorWebApi)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Planthor_PlanthorWebApi&metric=coverage)](https://sonarcloud.io/summary/new_code?id=Planthor_PlanthorWebApi)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Planthor_PlanthorWebApi&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Planthor_PlanthorWebApi)

PlanthorWebApi is a RESTful API project designed to support the Planthor application. This project is built using .NET 10 and utilizes a NoSQL database.

## Features

TODO: Add details feature to WebApi

## Requirements

- .NET 10 SDK or higher
- NoSQL database (MongoDB, etc.)
- Docker/Docker Desktop

## Installation

1. Clone the repository:

```bash
git clone https://github.com/Planthor/PlanthorWebApi.git
```

## Run Unit + Integration Tests

```bash
dotnet test --results-directory ./tests/CodeCoverageResults --collect:"XPlat Code Coverage;Format=lcov,opencover"
```

## Start the Planthor Web Api application in local development

### For Windows

1. Start the infrastructure

```bash
cd .\infrastructure\;
docker compose up --build -d
```

1. Start application with secret

```bash
$env:ConnectionStrings__PlanthorDbContext = "mongodb://admin:Planthor_123@localhost:27017/";
dotnet build;
dotnet run
```

## Documentation

Please check WIKI or individual README(s) for more documentation.
