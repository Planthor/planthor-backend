# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish "src/Api/Api.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
EXPOSE 8080

# Production Configuration Placeholders
# Overridable via environment variables or .env file
ENV ConnectionStrings__PlanthorDbContext=""
ENV Authentication__Authority=""
ENV Authentication__Audience="planthor-backend"
ENV Authentication__RequireHttpsMetadata="true"

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]
