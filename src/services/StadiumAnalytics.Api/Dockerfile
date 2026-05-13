FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.config ./
COPY StadiumAnalytics.slnx ./
COPY src/libraries/StadiumAnalytics.Domain/StadiumAnalytics.Domain.csproj src/libraries/StadiumAnalytics.Domain/
COPY src/libraries/StadiumAnalytics.Application/StadiumAnalytics.Application.csproj src/libraries/StadiumAnalytics.Application/
COPY src/libraries/StadiumAnalytics.Infrastructure/StadiumAnalytics.Infrastructure.csproj src/libraries/StadiumAnalytics.Infrastructure/
COPY src/services/StadiumAnalytics.Api/StadiumAnalytics.Api.csproj src/services/StadiumAnalytics.Api/

RUN dotnet restore src/services/StadiumAnalytics.Api/StadiumAnalytics.Api.csproj

COPY src/ src/

RUN dotnet publish src/services/StadiumAnalytics.Api/StadiumAnalytics.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "StadiumAnalytics.Api.dll"]
