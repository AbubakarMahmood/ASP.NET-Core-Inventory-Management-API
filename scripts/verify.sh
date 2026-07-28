#!/usr/bin/env bash
set -euo pipefail

repo_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
cd "$repo_root"

dotnet --info
dotnet restore InventoryAPI.sln
dotnet build InventoryAPI.sln -c Release --no-restore
dotnet test tests/InventoryAPI.UnitTests -c Release --no-build --logger "console;verbosity=minimal"
dotnet test tests/InventoryAPI.IntegrationTests -c Release --no-build --logger "console;verbosity=minimal"
dotnet publish src/InventoryAPI.Api/InventoryAPI.Api.csproj -c Release --no-build -o artifacts/api
dotnet publish src/InventoryAPI.BlazorUI/InventoryAPI.BlazorUI.csproj -c Release --no-build -o artifacts/ui
