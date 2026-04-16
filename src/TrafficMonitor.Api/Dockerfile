# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TrafficMonitor.slnx ./
COPY src/TrafficMonitor.Domain/*.csproj         src/TrafficMonitor.Domain/
COPY src/TrafficMonitor.Application/*.csproj    src/TrafficMonitor.Application/
COPY src/TrafficMonitor.Infrastructure/*.csproj src/TrafficMonitor.Infrastructure/
COPY src/TrafficMonitor.Api/*.csproj            src/TrafficMonitor.Api/

RUN dotnet restore src/TrafficMonitor.Api/TrafficMonitor.Api.csproj

COPY src/ src/

RUN dotnet publish src/TrafficMonitor.Api \
    -c Release \
    -o /app \
    --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is needed for the compose healthcheck; the aspnet image does not ship with it.
USER root
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

USER app

EXPOSE 8080

ENTRYPOINT ["dotnet", "TrafficMonitor.Api.dll"]
