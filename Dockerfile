FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DataIngestService.sln ./
COPY src/DataIngestService/DataIngestService.csproj src/DataIngestService/
COPY tests/DataIngestService.Tests/DataIngestService.Tests.csproj tests/DataIngestService.Tests/
RUN dotnet restore src/DataIngestService/DataIngestService.csproj

COPY . .
RUN dotnet publish src/DataIngestService/DataIngestService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DataIngestService.dll"]
