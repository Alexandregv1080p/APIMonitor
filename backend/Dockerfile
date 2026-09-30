# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore primeiro, só com os .csproj, para aproveitar o cache de camadas.
COPY ApiMonitor.Api/ApiMonitor.Api.csproj ApiMonitor.Api/
COPY ApiMonitor.Application/ApiMonitor.Application.csproj ApiMonitor.Application/
COPY ApiMonitor.Domain/ApiMonitor.Domain.csproj ApiMonitor.Domain/
COPY ApiMonitor.Infrastructure/ApiMonitor.Infrastructure.csproj ApiMonitor.Infrastructure/
RUN dotnet restore ApiMonitor.Api/ApiMonitor.Api.csproj

COPY . .
RUN dotnet publish ApiMonitor.Api/ApiMonitor.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "ApiMonitor.Api.dll"]
