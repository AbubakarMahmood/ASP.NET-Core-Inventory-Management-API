# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first so the restore layer is cached
COPY src/InventoryAPI.Domain/InventoryAPI.Domain.csproj src/InventoryAPI.Domain/
COPY src/InventoryAPI.Application/InventoryAPI.Application.csproj src/InventoryAPI.Application/
COPY src/InventoryAPI.Infrastructure/InventoryAPI.Infrastructure.csproj src/InventoryAPI.Infrastructure/
COPY src/InventoryAPI.Api/InventoryAPI.Api.csproj src/InventoryAPI.Api/
RUN dotnet restore src/InventoryAPI.Api/InventoryAPI.Api.csproj

COPY src/ src/
RUN dotnet publish src/InventoryAPI.Api/InventoryAPI.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Run as the non-root user that ships with the .NET images
RUN mkdir -p /app/logs /app/data-protection-keys && chown -R app:app /app
USER app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "InventoryAPI.Api.dll"]
