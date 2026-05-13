FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.config ./
COPY StadiumAnalytics.slnx ./
COPY src/libraries/StadiumAnalytics.Domain/StadiumAnalytics.Domain.csproj src/libraries/StadiumAnalytics.Domain/
COPY src/libraries/StadiumAnalytics.Application/StadiumAnalytics.Application.csproj src/libraries/StadiumAnalytics.Application/
COPY src/libraries/StadiumAnalytics.Infrastructure/StadiumAnalytics.Infrastructure.csproj src/libraries/StadiumAnalytics.Infrastructure/
COPY src/services/StadiumAnalytics.Simulator/StadiumAnalytics.Simulator.csproj src/services/StadiumAnalytics.Simulator/

RUN dotnet restore src/services/StadiumAnalytics.Simulator/StadiumAnalytics.Simulator.csproj

COPY src/ src/

RUN dotnet publish src/services/StadiumAnalytics.Simulator/StadiumAnalytics.Simulator.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "StadiumAnalytics.Simulator.dll"]
