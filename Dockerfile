FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EvolutionApi.DotNet.sln ./
COPY src/EvolutionApi.API/EvolutionApi.API.csproj src/EvolutionApi.API/
COPY src/EvolutionApi.Application/EvolutionApi.Application.csproj src/EvolutionApi.Application/
COPY src/EvolutionApi.Domain/EvolutionApi.Domain.csproj src/EvolutionApi.Domain/
COPY src/EvolutionApi.Infrastructure/EvolutionApi.Infrastructure.csproj src/EvolutionApi.Infrastructure/
RUN dotnet restore EvolutionApi.DotNet.sln

COPY src/ src/
RUN dotnet publish src/EvolutionApi.API/EvolutionApi.API.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

RUN apt-get update && apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/* && \
    mkdir -p /var/lib/evolution-api/data-protection-keys && \
    chown -R $APP_UID /app /var/lib/evolution-api

COPY --from=build /app/publish ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "EvolutionApi.API.dll"]
