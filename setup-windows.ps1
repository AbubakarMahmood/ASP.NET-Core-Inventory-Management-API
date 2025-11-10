# Windows Setup Script for Inventory Management API
# Run this in PowerShell as Administrator

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Inventory Management API - Setup" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check if .NET SDK is installed
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK $dotnetVersion found" -ForegroundColor Green
}
catch {
    Write-Host "✗ .NET SDK not found!" -ForegroundColor Red
    Write-Host "  Please install .NET 8 SDK from:" -ForegroundColor Yellow
    Write-Host "  https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}

# Check if Docker is installed
Write-Host "Checking Docker..." -ForegroundColor Yellow
try {
    $dockerVersion = docker --version
    Write-Host "✓ Docker found: $dockerVersion" -ForegroundColor Green
    $useDocker = Read-Host "Do you want to use Docker for PostgreSQL? (Y/N)"
}
catch {
    Write-Host "✗ Docker not found (optional)" -ForegroundColor Yellow
    $useDocker = "N"
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Setup Options" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "1. Full Docker Setup (Recommended)" -ForegroundColor Green
Write-Host "2. .NET SDK with Docker PostgreSQL" -ForegroundColor Yellow
Write-Host "3. .NET SDK with Local PostgreSQL" -ForegroundColor Yellow
Write-Host ""

$choice = Read-Host "Select option (1-3)"

switch ($choice) {
    "1" {
        Write-Host ""
        Write-Host "Starting Full Docker Setup..." -ForegroundColor Green
        Write-Host ""

        # Build and run with docker-compose
        docker-compose up --build -d

        Write-Host ""
        Write-Host "✓ Application started successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Access the application at:" -ForegroundColor Cyan
        Write-Host "  - Blazor UI:    http://localhost:3000" -ForegroundColor White
        Write-Host "  - API Swagger:  http://localhost:5000/swagger" -ForegroundColor White
        Write-Host "  - PostgreSQL:   localhost:5433" -ForegroundColor White
        Write-Host ""
        Write-Host "Default credentials:" -ForegroundColor Cyan
        Write-Host "  Email:    admin@inventory.com" -ForegroundColor White
        Write-Host "  Password: Admin123!" -ForegroundColor White
        Write-Host ""
        Write-Host "To view logs: docker-compose logs -f" -ForegroundColor Yellow
        Write-Host "To stop:      docker-compose down" -ForegroundColor Yellow
    }

    "2" {
        Write-Host ""
        Write-Host "Starting Docker PostgreSQL..." -ForegroundColor Green

        # Start PostgreSQL only
        docker run --name inventory-postgres `
            -e POSTGRES_DB=inventorydb `
            -e POSTGRES_USER=inventoryuser `
            -e POSTGRES_PASSWORD=InventoryPass123! `
            -p 5433:5432 `
            -d postgres:14-alpine

        Write-Host "✓ PostgreSQL started" -ForegroundColor Green
        Write-Host "Waiting for PostgreSQL to be ready..." -ForegroundColor Yellow
        Start-Sleep -Seconds 5

        # Install EF Core tools if not installed
        Write-Host "Installing Entity Framework tools..." -ForegroundColor Yellow
        dotnet tool install --global dotnet-ef 2>$null

        # Update PATH for current session
        $env:PATH = $env:PATH + ";$env:USERPROFILE\.dotnet\tools"

        # Create and apply migrations
        Write-Host "Creating database migrations..." -ForegroundColor Yellow
        Set-Location src\InventoryAPI.Infrastructure

        try {
            dotnet ef migrations add InitialCreate -s ..\InventoryAPI.Api --force
            dotnet ef database update -s ..\InventoryAPI.Api
            Write-Host "✓ Database created and migrations applied" -ForegroundColor Green
        }
        catch {
            Write-Host "Note: Migrations may already exist or database may already be updated" -ForegroundColor Yellow
        }

        Set-Location ..\..

        Write-Host ""
        Write-Host "✓ Setup complete!" -ForegroundColor Green
        Write-Host ""
        Write-Host "To run the API:" -ForegroundColor Cyan
        Write-Host "  cd src\InventoryAPI.Api" -ForegroundColor White
        Write-Host "  dotnet run" -ForegroundColor White
        Write-Host ""
        Write-Host "To run the Blazor UI (in new terminal):" -ForegroundColor Cyan
        Write-Host "  cd src\InventoryAPI.BlazorUI" -ForegroundColor White
        Write-Host "  dotnet run" -ForegroundColor White
    }

    "3" {
        Write-Host ""
        Write-Host "Local PostgreSQL Setup" -ForegroundColor Green
        Write-Host ""
        Write-Host "Please ensure PostgreSQL is installed and running." -ForegroundColor Yellow
        Write-Host ""

        $pgHost = Read-Host "PostgreSQL host (default: localhost)"
        if ([string]::IsNullOrWhiteSpace($pgHost)) { $pgHost = "localhost" }

        $pgPort = Read-Host "PostgreSQL port (default: 5432)"
        if ([string]::IsNullOrWhiteSpace($pgPort)) { $pgPort = "5432" }

        $pgUser = Read-Host "PostgreSQL username (default: inventoryuser)"
        if ([string]::IsNullOrWhiteSpace($pgUser)) { $pgUser = "inventoryuser" }

        $pgPass = Read-Host "PostgreSQL password" -AsSecureString
        $pgPassPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($pgPass))

        # Update appsettings
        $connectionString = "Host=$pgHost;Port=$pgPort;Database=inventorydb;Username=$pgUser;Password=$pgPassPlain"

        $appsettings = Get-Content src\InventoryAPI.Api\appsettings.Development.json | ConvertFrom-Json
        $appsettings.ConnectionStrings.DefaultConnection = $connectionString
        $appsettings | ConvertTo-Json -Depth 10 | Set-Content src\InventoryAPI.Api\appsettings.Development.json

        Write-Host "✓ Connection string updated" -ForegroundColor Green

        # Install EF Core tools
        Write-Host "Installing Entity Framework tools..." -ForegroundColor Yellow
        dotnet tool install --global dotnet-ef 2>$null
        $env:PATH = $env:PATH + ";$env:USERPROFILE\.dotnet\tools"

        # Create and apply migrations
        Write-Host "Creating database migrations..." -ForegroundColor Yellow
        Set-Location src\InventoryAPI.Infrastructure

        try {
            dotnet ef migrations add InitialCreate -s ..\InventoryAPI.Api --force
            dotnet ef database update -s ..\InventoryAPI.Api
            Write-Host "✓ Database created and migrations applied" -ForegroundColor Green
        }
        catch {
            Write-Host "Note: Check your PostgreSQL connection and try again" -ForegroundColor Yellow
        }

        Set-Location ..\..

        Write-Host ""
        Write-Host "✓ Setup complete!" -ForegroundColor Green
        Write-Host ""
        Write-Host "To run the API:" -ForegroundColor Cyan
        Write-Host "  cd src\InventoryAPI.Api" -ForegroundColor White
        Write-Host "  dotnet run" -ForegroundColor White
    }

    default {
        Write-Host "Invalid option selected" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Setup Complete! 🚀" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Access the Blazor UI at http://localhost:3000" -ForegroundColor White
Write-Host "  2. Login with: admin@inventory.com / Admin123!" -ForegroundColor White
Write-Host "  3. Explore the API at http://localhost:5000/swagger" -ForegroundColor White
Write-Host ""
Write-Host "For detailed instructions, see GETTING_STARTED_WINDOWS.md" -ForegroundColor Cyan
Write-Host ""
