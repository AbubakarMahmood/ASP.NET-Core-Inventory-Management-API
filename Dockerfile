# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0.423 AS build
WORKDIR /src

COPY global.json ./
COPY src/InventoryAPI.Domain/InventoryAPI.Domain.csproj src/InventoryAPI.Domain/
COPY src/InventoryAPI.Application/InventoryAPI.Application.csproj src/InventoryAPI.Application/
COPY src/InventoryAPI.Infrastructure/InventoryAPI.Infrastructure.csproj src/InventoryAPI.Infrastructure/
COPY src/InventoryAPI.Api/InventoryAPI.Api.csproj src/InventoryAPI.Api/
RUN dotnet restore src/InventoryAPI.Api/InventoryAPI.Api.csproj

COPY src/ src/
RUN dotnet publish src/InventoryAPI.Api/InventoryAPI.Api.csproj \
    -c Release \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0.29 AS final
WORKDIR /app

LABEL org.opencontainers.image.title="StockVerity API" \
      org.opencontainers.image.description="Integrity-focused maintenance-parts inventory and work-order API"

COPY --from=build /app/publish .
RUN mkdir -p /app/logs /app/data-protection-keys \
    && chown -R app:app /app

USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "InventoryAPI.Api.dll"]
