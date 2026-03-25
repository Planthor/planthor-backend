FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
COPY . /app
WORKDIR /app
RUN dotnet restore && \
    dotnet publish -c Release -o out

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
LABEL authors="Planthor"
WORKDIR /app
EXPOSE 5001
COPY --from=build /app/out ./
RUN echo 'dotnet PlanthorWebApi.dll /seed' > run_seed.sh && \
    chmod +x run_seed.sh
ENTRYPOINT ["dotnet", "PlanthorWebApi.dll"]
