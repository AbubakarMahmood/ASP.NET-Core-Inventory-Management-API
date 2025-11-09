# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["InventoryAPI.sln", "./"]
COPY ["src/InventoryAPI.Api/InventoryAPI.Api.csproj", "src/InventoryAPI.Api/"]
COPY ["src/InventoryAPI.Application/InventoryAPI.Application.csproj", "src/InventoryAPI.Application/"]
COPY ["src/InventoryAPI.Domain/InventoryAPI.Domain.csproj", "src/InventoryAPI.Domain/"]
COPY ["src/InventoryAPI.Infrastructure/InventoryAPI.Infrastructure.csproj", "src/InventoryAPI.Infrastructure/"]

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/InventoryAPI.Api"
RUN dotnet build "InventoryAPI.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "InventoryAPI.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

ENTRYPOINT ["dotnet", "InventoryAPI.Api.dll"]
