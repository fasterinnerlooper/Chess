# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy solution and project files
COPY Chesster.sln ./
COPY Chesster.Api/Chesster.Api.csproj ./Chesster.Api/
COPY Chesster.Api.Tests/Chesster.Api.Tests.csproj ./Chesster.Api.Tests/

# Restore dependencies
RUN dotnet restore Chesster.sln

# Copy source code
COPY Chesster.Api/ ./Chesster.Api/
COPY Chesster.Api.Tests/ ./Chesster.Api.Tests/

# Build and publish
WORKDIR /src/Chesster.Api
RUN dotnet publish -c Debug -o /app/publish --no-self-contained

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    libicu-dev \
    && rm -rf /var/lib/apt/lists/*

COPY --from=builder /app/publish .

EXPOSE 8080

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Chesster.Api.dll"]

# Development stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS development
WORKDIR /src

COPY Chesster.sln ./
COPY Chesster.Api/Chesster.Api.csproj ./Chesster.Api/
COPY Chesster.Api.Tests/Chesster.Api.Tests.csproj ./Chesster.Api.Tests/

RUN dotnet restore Chesster.sln

COPY Chesster.Api/ ./Chesster.Api/
COPY Chesster.Api.Tests/ ./Chesster.Api.Tests/

RUN dotnet tool install -g vsdbg

EXPOSE 8080 4020

ENV ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_STARTUP_HOOKS=/root/.vsdbg/vsdbg/hook \
    DEBUGGER_PORT=4020

WORKDIR /src/Chesster.Api
ENTRYPOINT ["vsdbg", "dotnet", "run", "--no-launch-profile"]