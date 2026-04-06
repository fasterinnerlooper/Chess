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
RUN dotnet publish -c Release -o /app/publish --no-self-contained

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
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

EXPOSE 8080

ENV ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS=http://+:$ASPNETCORE_PORT

WORKDIR /src/Chesster.Api
ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile"]