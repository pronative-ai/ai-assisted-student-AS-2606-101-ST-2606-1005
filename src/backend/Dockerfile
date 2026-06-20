FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Bookkeeping.slnx .
COPY src/backend/backend.csproj src/backend/
RUN dotnet restore src/backend/backend.csproj
COPY src/backend/ src/backend/
RUN dotnet publish src/backend/backend.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app .
ENTRYPOINT ["dotnet", "backend.dll"]
