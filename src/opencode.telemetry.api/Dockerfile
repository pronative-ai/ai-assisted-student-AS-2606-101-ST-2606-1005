FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/opencode.telemetry.api/Opencode.Telemetry.Api.csproj", "src/opencode.telemetry.api/"]
COPY ["src/opencode.telemetry.application/Opencode.Telemetry.Application.csproj", "src/opencode.telemetry.application/"]
COPY ["src/opencode.telemetry.domain/Opencode.Telemetry.Domain.csproj", "src/opencode.telemetry.domain/"]
COPY ["src/opencode.telemetry.contracts/Opencode.Telemetry.Contracts.csproj", "src/opencode.telemetry.contracts/"]
COPY ["src/opencode.telemetry.infrastructure/Opencode.Telemetry.Infrastructure.csproj", "src/opencode.telemetry.infrastructure/"]
COPY ["src/opencode.telemetry.infrastructure/Proto/", "src/opencode.telemetry.infrastructure/Proto/"]
RUN dotnet restore "src/opencode.telemetry.api/Opencode.Telemetry.Api.csproj"

COPY . .
WORKDIR "/src/src/opencode.telemetry.api"
RUN dotnet build "Opencode.Telemetry.Api.csproj" -c Release --no-restore

FROM build AS publish
RUN dotnet publish "Opencode.Telemetry.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Opencode.Telemetry.Api.dll"]
