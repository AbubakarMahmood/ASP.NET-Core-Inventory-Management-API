# Getting Started on Windows

Complete guide for running and testing the Inventory Management API on Windows.

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Option 1: Docker (Easiest - Recommended)](#option-1-docker-easiest---recommended)
3. [Option 2: Direct on Windows with .NET SDK](#option-2-direct-on-windows-with-net-sdk)
4. [Option 3: Using WSL (Windows Subsystem for Linux)](#option-3-using-wsl-windows-subsystem-for-linux)
5. [Testing the Application](#testing-the-application)
6. [Default Credentials](#default-credentials)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

Choose based on your preferred method:

### For Docker Approach (Recommended)
- **Docker Desktop for Windows** (already installed ✓)
- That's it! Everything else runs in containers.

### For Direct Windows Approach
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) - Download and install
- [PostgreSQL 14+](https://www.enterprisedb.com/downloads/postgres-postgresql-downloads) - Windows installer
- OR use Docker for PostgreSQL only
- Visual Studio 2022 or VS Code (optional but recommended)

### For WSL Approach
- WSL 2 (already installed ✓)
- .NET 8 SDK installed in WSL
- Docker Desktop with WSL 2 integration

---

## Option 1: Docker (Easiest - Recommended)

This approach runs everything in containers - no local installations needed except Docker!

### Step 1: Start Docker Desktop

Make sure Docker Desktop is running on Windows.

### Step 2: Navigate to Project Directory

Open **PowerShell** or **Command Prompt**:

```powershell
cd path\to\ASP.NET-Core-Inventory-Management-API-
```

### Step 3: Build and Run Everything

Run this single command to start the entire application:

```powershell
docker-compose up --build
```

**What this does:**
- ✅ Starts PostgreSQL database (port 5433 on host, to avoid conflicts)
- ✅ Builds and runs the API (port 5000)
- ✅ Builds and runs the Blazor UI (port 3000)
- ✅ Creates network connectivity between all services
- ✅ Automatically creates database and runs migrations

### Step 4: Wait for Startup

You'll see logs from all services. Wait for these messages:

```
inventory-postgres | database system is ready to accept connections
inventory-api     | Now listening on: http://0.0.0.0:80
inventory-ui      | Now listening on: http://[::]:80
```

### Step 5: Access the Application

- **Blazor UI**: http://localhost:3000
- **API Swagger**: http://localhost:5000/swagger
- **API Base URL**: http://localhost:5000/api/v1

### Step 6: Stop the Application

Press `Ctrl+C` in the terminal, then run:

```powershell
docker-compose down
```

To stop and remove all data (including database):

```powershell
docker-compose down -v
```

---

## Option 2: Direct on Windows with .NET SDK

This approach runs the application directly on Windows.

### Step 1: Install .NET 8 SDK

1. Download from: https://dotnet.microsoft.com/download/dotnet/8.0
2. Run the installer
3. Verify installation in PowerShell:

```powershell
dotnet --version
# Should output: 8.0.x
```

### Step 2: Set Up PostgreSQL

**Option A: Use Docker for PostgreSQL only** (Recommended)

```powershell
docker run --name inventory-postgres `
  -e POSTGRES_DB=inventorydb `
  -e POSTGRES_USER=inventoryuser `
  -e POSTGRES_PASSWORD=InventoryPass123! `
  -p 5433:5432 `
  -d postgres:14-alpine
```

**Note:** We use port 5433 on the host to avoid conflicts if you have PostgreSQL already installed.

**Option B: Install PostgreSQL on Windows**

1. Download from: https://www.enterprisedb.com/downloads/postgres-postgresql-downloads
2. Run installer (default port: 5432)
3. Create database using pgAdmin or command line:

```sql
CREATE DATABASE inventorydb;
CREATE USER inventoryuser WITH PASSWORD 'InventoryPass123!';
GRANT ALL PRIVILEGES ON DATABASE inventorydb TO inventoryuser;
```

### Step 3: Configure Connection String

The connection string is already configured in `src/InventoryAPI.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=inventorydb;Username=inventoryuser;Password=Dev123Password"
  }
}
```

**Update the password** to match what you set (default: `InventoryPass123!` or `Dev123Password`).

### Step 4: Install Entity Framework Tools

```powershell
dotnet tool install --global dotnet-ef
```

Verify installation:

```powershell
dotnet ef
```

### Step 5: Create Database and Run Migrations

Navigate to the Infrastructure project:

```powershell
cd src\InventoryAPI.Infrastructure
```

Create initial migration (if doesn't exist):

```powershell
dotnet ef migrations add InitialCreate -s ..\InventoryAPI.Api
```

Apply migrations to database:

```powershell
dotnet ef database update -s ..\InventoryAPI.Api
```

You should see:
```
Applying migration '20240101000000_InitialCreate'.
Done.
```

### Step 6: Run the API

Navigate to API project:

```powershell
cd ..\InventoryAPI.Api
```

Run the API:

```powershell
dotnet run
```

You should see:

```
Now listening on: https://localhost:5001
Now listening on: http://localhost:5000
```

API is now running! Access Swagger at: https://localhost:5001/swagger

### Step 7: Run the Blazor UI (Separate Terminal)

Open a **new PowerShell window**, navigate to the Blazor project:

```powershell
cd path\to\ASP.NET-Core-Inventory-Management-API-\src\InventoryAPI.BlazorUI
```

Run the UI:

```powershell
dotnet run
```

Access the UI at: https://localhost:5002 (or the port shown in console)

---

## Option 3: Using WSL (Windows Subsystem for Linux)

This approach runs everything in WSL for a Linux-like development environment.

### Step 1: Open WSL

```powershell
wsl
```

### Step 2: Install .NET 8 SDK in WSL

```bash
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

Verify:

```bash
dotnet --version
```

### Step 3: Navigate to Project

Your Windows drives are mounted at `/mnt/`:

```bash
cd /mnt/c/path/to/ASP.NET-Core-Inventory-Management-API-
```

### Step 4: Use Docker from WSL

Since you have Docker Desktop with WSL integration, you can use docker-compose directly:

```bash
docker-compose up --build
```

Or follow the same steps as Option 2 but in WSL terminal.

---

## Testing the Application

### 1. Access the Blazor UI

Open browser: http://localhost:3000 (Docker) or https://localhost:5002 (direct)

**Login with default credentials:**

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@inventory.com | Admin123! |
| Manager | manager@inventory.com | Manager123! |
| Operator | operator@inventory.com | Operator123! |

### 2. Explore the Features

**After logging in, you can:**

✅ **Products Page**
- View all products with advanced filtering
- Create new products
- Apply filter presets
- Use keyboard shortcuts (Ctrl+S to save preset, Ctrl+K to clear filters)
- Export to Excel/PDF

✅ **Work Orders Page**
- View all work orders with status filters
- Create new work orders
- Submit for approval (Operator)
- Approve work orders (Manager/Admin)
- Track work order lifecycle

✅ **Stock Movements**
- Record stock movements (receipt, issue, adjustment, transfer)
- View movement history
- Track stock levels

✅ **Users Page** (Admin only)
- Manage users
- Assign roles

✅ **Audit Log Viewer** (Admin/Manager)
- View all system changes
- Filter by user, entity, action
- Track who did what and when

✅ **Reports**
- Low Stock Report (products below reorder point)
- Pending Orders Report

### 3. Test the API with Swagger

Open: http://localhost:5000/swagger (Docker) or https://localhost:5001/swagger (direct)

**Step-by-step:**

1. **Authenticate:**
   - Expand `POST /api/v1/auth/login`
   - Click "Try it out"
   - Enter credentials:
     ```json
     {
       "email": "admin@inventory.com",
       "password": "Admin123!"
     }
     ```
   - Click "Execute"
   - Copy the `accessToken` from response

2. **Authorize Swagger:**
   - Click the green "Authorize" button at the top
   - Enter: `Bearer <your-access-token>`
   - Click "Authorize"

3. **Try API Endpoints:**
   - `GET /api/v1/products` - Get all products
   - `POST /api/v1/products` - Create a product
   - `GET /api/v1/work-orders` - Get work orders
   - All endpoints are now accessible!

### 4. Test with PowerShell (REST API)

```powershell
# Login and get token
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/auth/login" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"email":"admin@inventory.com","password":"Admin123!"}'

$token = $loginResponse.accessToken

# Get products
$headers = @{
    "Authorization" = "Bearer $token"
}

Invoke-RestMethod -Uri "http://localhost:5000/api/v1/products?pageSize=10" `
  -Method Get `
  -Headers $headers
```

### 5. Test with curl (in PowerShell or WSL)

```bash
# Login
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@inventory.com","password":"Admin123!"}'

# Use the token (replace <TOKEN> with actual token)
curl -X GET http://localhost:5000/api/v1/products \
  -H "Authorization: Bearer <token>"
```

### 6. Run Unit and Integration Tests

Navigate to solution root:

```powershell
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test project
dotnet test tests\InventoryAPI.UnitTests
dotnet test tests\InventoryAPI.IntegrationTests

# Run with coverage (requires coverlet)
dotnet test /p:CollectCoverage=true
```

---

## Default Credentials

The application seeds default users on first run:

| User Type | Email | Password | Role |
|-----------|-------|----------|------|
| Administrator | admin@inventory.com | Admin123! | Admin |
| Manager | manager@inventory.com | Manager123! | Manager |
| Operator | operator@inventory.com | Operator123! | Operator |

**Default Products Created:**
- Laptop Computer (SKU: LAP-001)
- Wireless Mouse (SKU: MOU-001)
- USB Cable (SKU: CAB-001)
- Monitor 24" (SKU: MON-001)
- Keyboard Mechanical (SKU: KEY-001)

---

## Troubleshooting

### Docker Issues

**Problem:** `Error response from daemon: driver failed`

**Solution:**
1. Restart Docker Desktop
2. In Docker Desktop settings, enable WSL 2 integration
3. Run `docker-compose down` then `docker-compose up --build`

**Problem:** `Port 5433 already in use`

**Solution:**
```powershell
# Find what's using port 5433
netstat -ano | findstr :5433

# Kill the process (replace PID)
taskkill /PID <PID> /F

# Or change the port in docker-compose.yml to another port
ports:
  - "5434:5432"  # Use 5434 on host instead
```

**Note:** The docker-compose.yml already uses port 5433 to avoid conflicts with locally-installed PostgreSQL on the default port 5432.

### .NET SDK Issues

**Problem:** `dotnet: command not found`

**Solution:**
1. Download and install from: https://dotnet.microsoft.com/download/dotnet/8.0
2. Restart PowerShell/Command Prompt
3. Verify: `dotnet --version`

**Problem:** `Could not execute because the specified command or file was not found`

**Solution:**
```powershell
# Restore dependencies
dotnet restore

# Clear caches
dotnet clean
dotnet restore
```

### Database Issues

**Problem:** `Failed to connect to PostgreSQL`

**Solution:**

1. **Check if PostgreSQL is running:**

   Docker:
   ```powershell
   docker ps
   # Should show inventory-postgres container
   ```

   Windows Service:
   ```powershell
   Get-Service -Name postgresql*
   # Should show "Running"
   ```

2. **Check connection string** in `appsettings.Development.json`

3. **Test connection manually:**
   ```powershell
   # If PostgreSQL is installed locally
   psql -h localhost -U inventoryuser -d inventorydb
   ```

**Problem:** `Migration failed`

**Solution:**
```powershell
# Drop and recreate database (WARNING: deletes all data)
dotnet ef database drop -s ..\InventoryAPI.Api --force
dotnet ef database update -s ..\InventoryAPI.Api
```

### Blazor UI Issues

**Problem:** `API calls failing with CORS errors`

**Solution:**

The API must be running first. Check `src/InventoryAPI.BlazorUI/wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "http://localhost:5000"
}
```

Update the URL to match your API address.

**Problem:** `Cannot connect to SignalR hub`

**Solution:**

1. Make sure API is running
2. Check browser console for errors
3. SignalR endpoint: `http://localhost:5000/notifications`

### Port Conflicts

**Default Ports:**
- API: 5000 (HTTP), 5001 (HTTPS)
- Blazor UI: 5002, 5003, or 3000 (Docker)
- PostgreSQL: 5433 (host) → 5432 (container)

**To use different ports:**

Edit `src/InventoryAPI.Api/Properties/launchSettings.json`:

```json
"applicationUrl": "https://localhost:7001;http://localhost:7000"
```

---

## Performance Tips

### 1. Use Docker for Development

Docker is the fastest way to get started - no complex setup required!

### 2. Use Visual Studio 2022

Visual Studio has excellent debugging and IntelliSense for ASP.NET Core:
- Press F5 to run with debugging
- Breakpoints work seamlessly
- Database tools built-in

### 3. Enable Hot Reload

```powershell
dotnet watch run
```

Code changes automatically reload without restarting!

### 4. Use Connection Pooling

Already configured in the connection string. PostgreSQL will reuse connections for better performance.

---

## Next Steps

After successfully running the application:

1. **Explore the API** - Try all endpoints in Swagger
2. **Test the UI** - Create products, work orders, record stock movements
3. **Review the Code** - Check out CLAUDE.md for architecture details
4. **Run Tests** - Ensure all unit and integration tests pass
5. **Customize** - Add your own features or modify existing ones

---

## Need Help?

- **Documentation**: See CLAUDE.md for detailed architecture info
- **API Reference**: http://localhost:5000/swagger when running
- **Logs**: Check console output or `logs/` directory

**Common Commands Quick Reference:**

```powershell
# Docker
docker-compose up              # Start everything
docker-compose up -d           # Start in background
docker-compose down            # Stop everything
docker-compose logs -f api     # View API logs

# .NET
dotnet run                     # Run project
dotnet watch run              # Run with hot reload
dotnet test                    # Run tests
dotnet ef database update      # Apply migrations

# Database
docker exec -it inventory-postgres psql -U inventoryuser -d inventorydb
```

---

**You're all set! 🚀**

Choose your preferred method (Docker recommended) and start exploring the Inventory Management API!
