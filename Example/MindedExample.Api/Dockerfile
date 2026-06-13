# Multi-stage build for the Minded Example API.
#
# Build from the REPOSITORY ROOT (the project references Framework/ and Extensions/
# and the central Directory.Build.props / Directory.Packages.props / key.snk):
#
#   docker build -f Example/MindedExample.Api/Dockerfile -t minded-example-api .
#
# Or via compose (with the frontend and PostgreSQL):
#
#   docker compose -f docker-compose.tests.yml --profile full up -d --build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Example/MindedExample.Api/MindedExample.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# curl is used by the container healthcheck (the aspnet image ships without it)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
# The aspnet images listen on 8080 by default (ASPNETCORE_HTTP_PORTS)
EXPOSE 8080
ENTRYPOINT ["dotnet", "MindedExample.Api.dll"]
