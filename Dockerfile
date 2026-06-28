# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Api/Api.csproj"
RUN dotnet publish "src/Api/Api.csproj" -c Release -o /app/publish --no-restore

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
EXPOSE 8080

# Production defaults. Override via orchestration env vars (K8s, ECS, docker-compose).
ENV ASPNETCORE_ENVIRONMENT="Production"
ENV ConnectionStrings__PlanthorDbContext=""
ENV MediatR__LicenseKey=""
ENV Authentication__Keycloak__Authority=""
ENV Authentication__Keycloak__Audience="planthor-backend"
ENV Authentication__Keycloak__RequireHttpsMetadata="true"

COPY --from=build /app/publish .
HEALTHCHECK CMD wget -qO- http://localhost:8080/healthz || exit 1
ENTRYPOINT ["dotnet", "Api.dll"]
