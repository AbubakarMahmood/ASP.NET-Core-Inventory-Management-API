# Self-contained demo image for a Hugging Face Docker Space:
# PostgreSQL and the API run in one container on port 7860.
# The build clones the public GitHub repository, so this Space only needs
# this Dockerfile, start.sh, and README.md.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN git clone --depth 1 https://github.com/AbubakarMahmood/ASP.NET-Core-Inventory-Management-API /src
WORKDIR /src
RUN dotnet publish src/InventoryAPI.Api/InventoryAPI.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql \
    && rm -rf /var/lib/apt/lists/*

RUN useradd -m -u 1000 appuser
WORKDIR /app

COPY --from=build /app/publish .
COPY --chmod=755 start.sh /app/start.sh
RUN mkdir -p /data/pg /app/logs /app/data-protection-keys \
    && chown -R appuser:appuser /app /data

USER appuser

ENV ASPNETCORE_URLS=http://+:7860 \
    ASPNETCORE_ENVIRONMENT=Development \
    ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=inventorydb;Username=appuser" \
    JwtSettings__SecretKey=huggingface-demo-signing-key-not-for-real-use-0001 \
    DataProtection__KeysPath=/app/data-protection-keys

EXPOSE 7860

ENTRYPOINT ["bash", "/app/start.sh"]
