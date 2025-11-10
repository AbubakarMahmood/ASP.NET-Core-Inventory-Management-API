# Inventory Management API

A production-grade REST API for inventory and work order management built with ASP.NET Core 8. This enterprise-level solution demonstrates modern architectural patterns and best practices for building scalable, maintainable APIs.

## 🚀 Quick Start (Windows Users)

**New to this project? Running on Windows?**

See **[GETTING_STARTED_WINDOWS.md](GETTING_STARTED_WINDOWS.md)** for detailed setup instructions with:
- ✅ Three setup options (Docker, .NET SDK, WSL)
- ✅ Step-by-step instructions with screenshots
- ✅ Troubleshooting guide
- ✅ Testing instructions
- ✅ Default credentials and sample data

**Super Quick Start with Docker:**
```powershell
docker-compose up --build
# Access UI at http://localhost:3000
# Login: admin@inventory.com / Admin123!
```

Or simply double-click **`quick-start.bat`** to start with Docker!

---

## 🎯 Overview

This API provides comprehensive inventory tracking and work order management capabilities suitable for warehouses, maintenance departments, and supply chain operations. Built with clean architecture principles, it emphasizes separation of concerns, testability, and extensibility.

## ✨ Features

### Core Functionality
- **Inventory Management**: Complete CRUD operations for products/items with SKU tracking, stock levels, and warehouse locations
- **Work Order System**: Full lifecycle management with approval workflows and status transitions
- **Stock Movement Tracking**: Detailed audit trail for receipts, issues, adjustments, and transfers
- **User Management**: Role-based access control with Admin, Manager, and Operator roles
- **Automated Alerts**: Reorder point notifications and low-stock warnings
- **Reporting**: Low stock reports, pending orders, and variance analysis

### Technical Features
- **CQRS Pattern**: Command Query Responsibility Segregation using MediatR
- **Repository + Unit of Work**: Clean data access abstraction
- **JWT Authentication**: Secure authentication with refresh token support
- **API Versioning**: Future-proof API design
- **Global Exception Handling**: Consistent error responses
- **Request/Response Logging**: Full observability with Serilog
- **Optimistic Concurrency**: Row version-based conflict detection
- **Soft Deletes**: Data preservation with global query filters
- **Health Checks**: Comprehensive monitoring and diagnostics endpoints
- **Pagination & Filtering**: Efficient data retrieval for all collections

### 🎯 Production-Ready Database Management
- **Automatic Connection Resilience**: Built-in retry logic with exponential backoff (5 attempts, 30s max delay)
- **Environment-Aware Initialization**:
  - **Development**: Automatic migrations + seeding for rapid iteration
  - **Production**: Fail-fast verification with detailed error reporting
- **Comprehensive Health Checks**: `/api/v1/health` with detailed database status including:
  - Connection status and response time
  - Current migration version
  - Pending migrations count
  - Database health indicators
- **Database Status Endpoint**: `/api/v1/database/status` for programmatic monitoring
- **Transaction Safety**: All migrations executed within transactions with automatic rollback on failure
- **Detailed Logging**: Full audit trail of initialization, migrations, and errors
- **Deployment Scripts**: Ready-to-use bash scripts for backup, migration, and health checks

## 🛠️ Tech Stack

| Category | Technology |
|----------|-----------|
| **Framework** | ASP.NET Core 8 (LTS) |
| **Database** | PostgreSQL with Entity Framework Core 8 |
| **Authentication** | JWT with refresh tokens |
| **Validation** | Fluent Validation |
| **Object Mapping** | AutoMapper |
| **Logging** | Serilog (structured logging) |
| **Testing** | xUnit + Moq |
| **Documentation** | Swagger/OpenAPI 3.0 |
| **Mediator** | MediatR (CQRS implementation) |

## 🏗️ Architecture

This project follows **Clean Architecture** principles with clear separation of concerns:

```
InventoryAPI/
├── src/
│   ├── InventoryAPI.Api/              # Presentation Layer
│   │   ├── Controllers/               # API endpoints
│   │   ├── Middleware/                # Custom middleware
│   │   ├── Filters/                   # Action filters
│   │   └── Program.cs                 # Application entry point
│   │
│   ├── InventoryAPI.Application/      # Application Layer
│   │   ├── Commands/                  # CQRS commands
│   │   ├── Queries/                   # CQRS queries
│   │   ├── Validators/                # FluentValidation validators
│   │   ├── Mappings/                  # AutoMapper profiles
│   │   └── Interfaces/                # Application contracts
│   │
│   ├── InventoryAPI.Domain/           # Domain Layer
│   │   ├── Entities/                  # Domain models
│   │   ├── Enums/                     # Enumerations
│   │   └── Exceptions/                # Domain exceptions
│   │
│   └── InventoryAPI.Infrastructure/   # Infrastructure Layer
│       ├── Data/                      # DbContext, migrations
│       ├── Repositories/              # Data access implementations
│       └── Services/                  # External service integrations
│
└── tests/
    ├── InventoryAPI.UnitTests/        # Unit tests
    └── InventoryAPI.IntegrationTests/ # Integration tests
```

### Architecture Principles
- **Dependency Inversion**: Core business logic has no dependencies on external concerns
- **Single Responsibility**: Each layer has a well-defined purpose
- **Interface Segregation**: Contracts define clear boundaries between layers
- **Domain-Driven Design**: Rich domain models with business logic encapsulation

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 14+](https://www.postgresql.org/download/)
- [Docker](https://www.docker.com/get-started) (optional, for containerized PostgreSQL)
- Your favorite IDE ([Visual Studio 2022](https://visualstudio.microsoft.com/), [VS Code](https://code.visualstudio.com/), or [Rider](https://www.jetbrains.com/rider/))

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/ASP.NET-Core-Inventory-Management-API-.git
   cd ASP.NET-Core-Inventory-Management-API-
   ```

2. **Set up PostgreSQL**

   Using Docker:
   ```bash
   docker run --name inventory-postgres \
     -e POSTGRES_DB=inventorydb \
     -e POSTGRES_USER=inventoryuser \
     -e POSTGRES_PASSWORD=your_password_here \
     -p 5432:5432 \
     -d postgres:14
   ```

   Or install PostgreSQL locally and create a database.

3. **Configure Connection String**

   Update `appsettings.Development.json` in the `InventoryAPI.Api` project:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=inventorydb;Username=inventoryuser;Password=your_password_here"
     },
     "JwtSettings": {
       "SecretKey": "your-super-secret-key-min-32-characters-long",
       "Issuer": "InventoryAPI",
       "Audience": "InventoryAPIUsers",
       "ExpiryMinutes": 60,
       "RefreshTokenExpiryDays": 7
     }
   }
   ```

4. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

5. **Apply Database Migrations**
   ```bash
   cd src/InventoryAPI.Api
   dotnet ef database update
   ```

6. **Run the Application**
   ```bash
   dotnet run
   ```

7. **Access Swagger UI**

   Navigate to: `https://localhost:5001/swagger`

### Quick Start with Seed Data

The application includes seed data for testing:
- **Admin User**: `admin@inventory.com` / `Admin123!`
- **Manager User**: `manager@inventory.com` / `Manager123!`
- **Operator User**: `operator@inventory.com` / `Operator123!`

Sample products and work orders are automatically created on first run.

## 📡 API Endpoints

### Authentication
```
POST   /api/v1/auth/login         # Authenticate user
POST   /api/v1/auth/refresh       # Refresh access token
POST   /api/v1/auth/register      # Register new user (Admin only)
```

### Products
```
GET    /api/v1/products           # List all products (paginated)
GET    /api/v1/products/{id}      # Get product by ID
POST   /api/v1/products           # Create new product
PUT    /api/v1/products/{id}      # Update product
DELETE /api/v1/products/{id}      # Soft delete product
POST   /api/v1/products/bulk      # Bulk create/update products
```

### Work Orders
```
GET    /api/v1/work-orders        # List work orders (paginated)
GET    /api/v1/work-orders/{id}   # Get work order by ID
POST   /api/v1/work-orders        # Create work order
PUT    /api/v1/work-orders/{id}   # Update work order
DELETE /api/v1/work-orders/{id}   # Cancel work order
POST   /api/v1/work-orders/{id}/submit    # Submit for approval
POST   /api/v1/work-orders/{id}/approve   # Approve work order
POST   /api/v1/work-orders/{id}/start     # Start work order
POST   /api/v1/work-orders/{id}/complete  # Complete work order
```

### Stock Movements
```
GET    /api/v1/stock-movements    # Query stock movements
POST   /api/v1/stock-movements    # Record stock movement
GET    /api/v1/stock-movements/product/{productId}  # Get product history
```

### Reports
```
GET    /api/v1/reports/low-stock         # Products below reorder point
GET    /api/v1/reports/pending-orders    # Work orders pending action
GET    /api/v1/reports/stock-valuation   # Inventory valuation report
GET    /api/v1/reports/movement-summary  # Stock movement summary
```

### System & Monitoring
```
GET    /api/v1/health             # Comprehensive health check with DB status
GET    /api/v1/database/status    # Detailed database status (public)
POST   /api/v1/database/verify    # Verify database readiness (Admin only)
```

## 🔐 Authentication

This API uses JWT (JSON Web Tokens) for authentication.

### Getting a Token

```bash
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@inventory.com",
    "password": "Admin123!"
  }'
```

Response:
```json
{
  "accessToken": "<issued at runtime>",
  "refreshToken": "<issued at runtime>",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

### Using the Token

Include the token in the `Authorization` header:

```bash
curl -X GET https://localhost:5001/api/v1/products \
  -H "Authorization: Bearer <token>"
```

### Role-Based Access

| Role | Permissions |
|------|-------------|
| **Admin** | Full access to all endpoints, user management |
| **Manager** | Approve work orders, manage inventory, view reports |
| **Operator** | Create work orders, record stock movements, view inventory |

## 🧪 Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsFolder=./coverage

# Run specific test project
dotnet test tests/InventoryAPI.UnitTests
dotnet test tests/InventoryAPI.IntegrationTests
```

### Test Structure

- **Unit Tests**: Test business logic in isolation using Moq for dependencies
- **Integration Tests**: Test API endpoints using `WebApplicationFactory` with in-memory database
- **Test Data Builders**: Fluent builders for creating test data
- **Coverage Target**: Minimum 70% code coverage

Example test:
```csharp
[Fact]
public async Task CreateProduct_WithValidData_ReturnsCreatedProduct()
{
    // Arrange
    var command = new CreateProductCommandBuilder()
        .WithSku("TEST-001")
        .WithName("Test Product")
        .WithStockLevel(100)
        .Build();

    // Act
    var result = await _mediator.Send(command);

    // Assert
    result.Should().NotBeNull();
    result.Sku.Should().Be("TEST-001");
}
```

## 📊 Business Logic

### Stock Management Rules

1. **Reorder Alerts**: Automatic notifications when `CurrentStock < ReorderPoint`
2. **Stock Validation**: Cannot issue more stock than available
3. **Concurrency Control**: Row version checks prevent lost updates
4. **Audit Trail**: All movements tracked with user, timestamp, and reason

### Work Order Workflow

```
Draft → Submitted → Approved → In Progress → Completed
                  ↓
               Rejected
```

**Transitions**:
- Draft → Submitted: Operator submits for approval
- Submitted → Approved: Manager approves
- Submitted → Rejected: Manager rejects
- Approved → In Progress: Operator starts work
- In Progress → Completed: Operator completes work

### Inventory Valuation Methods

- **FIFO** (First In, First Out)
- **LIFO** (Last In, First Out)
- **Average Cost**: Weighted average calculation

## 🔧 Development

### Adding a New Feature

1. **Define Domain Entity** in `InventoryAPI.Domain/Entities`
2. **Create DTOs** for request/response
3. **Implement Commands/Queries** in `InventoryAPI.Application`
4. **Add Validators** using FluentValidation
5. **Create Controller** in `InventoryAPI.Api/Controllers`
6. **Add Migrations**: `dotnet ef migrations add FeatureName`
7. **Write Tests** for business logic and endpoints

### Code Quality

- **StyleCop**: Enforces coding standards
- **Code Analysis**: Roslyn analyzers enabled
- **EditorConfig**: Consistent formatting rules
- **SonarQube**: Static code analysis (recommended for CI/CD)

### Database Migrations

```bash
# Create a new migration
dotnet ef migrations add MigrationName -p src/InventoryAPI.Infrastructure -s src/InventoryAPI.Api

# Update database
dotnet ef database update -p src/InventoryAPI.Infrastructure -s src/InventoryAPI.Api

# Rollback migration
dotnet ef database update PreviousMigrationName -p src/InventoryAPI.Infrastructure -s src/InventoryAPI.Api
```

## 🚀 Deployment

### Quick Deployment with Docker Compose

```bash
# Complete stack (API + PostgreSQL + UI)
docker-compose up -d --build

# Access the API
curl http://localhost:5000/api/v1/health
```

### Production Deployment

For production deployments, use the provided deployment scripts and comprehensive documentation:

#### 📚 **Complete Deployment Documentation**

- **[DEPLOYMENT.md](docs/DEPLOYMENT.md)** - Complete production deployment guide covering:
  - All deployment scenarios (fresh install, updates, zero-downtime)
  - Database migration strategies
  - Rollback procedures
  - Health check monitoring
  - Troubleshooting guide

- **[MIGRATIONS.md](docs/MIGRATIONS.md)** - Database migrations guide with:
  - Creating and managing migrations
  - Migration best practices
  - Common scenarios and examples
  - Troubleshooting tips

#### 🔧 **Automated Deployment Scripts** (`scripts/`)

```bash
# Create database backup
./scripts/backup-database.sh

# Apply database migrations (Production)
./scripts/migrate-database.sh Production

# Run health checks
./scripts/health-check.sh

# Complete production deployment workflow
./scripts/deploy-production.sh
```

See [scripts/README.md](scripts/README.md) for detailed script documentation.

#### 🎯 **Environment-Aware Behavior**

The application intelligently handles database initialization based on the environment:

**Development Mode** (`ASPNETCORE_ENVIRONMENT=Development`):
- ✅ Automatically applies pending migrations
- ✅ Seeds sample data
- ✅ Continues on database errors (better DX)
- ✅ Logs detailed initialization steps

**Production Mode** (`ASPNETCORE_ENVIRONMENT=Production`):
- ✅ Verifies database is ready (fail-fast)
- ✅ **Blocks startup** if migrations pending
- ✅ Requires manual migration before deployment
- ✅ Prevents accidental data changes

#### 📊 **Health Check Endpoints**

Monitor your deployment with comprehensive health checks:

```bash
# Basic health check
curl http://localhost:5000/api/v1/health

# Response:
{
  "status": "Healthy",
  "database": {
    "status": "Connected",
    "currentMigration": "20250110154530_InitialCreate",
    "totalMigrations": 1,
    "pendingMigrations": 0,
    "responseTimeMs": 45
  }
}

# Detailed database status
curl http://localhost:5000/api/v1/database/status | jq

# Admin verification (requires auth)
curl -H "Authorization: Bearer <token>" \
  -X POST http://localhost:5000/api/v1/database/verify
```

#### 🔄 **Connection Resilience**

Built-in automatic retry logic handles transient connection failures:
- **5 retry attempts** with exponential backoff
- **Maximum delay**: 30 seconds
- **Command timeout**: 120 seconds for migrations
- Automatic reconnection on network glitches

### Docker Deployment

```bash
# Build image
docker build -t inventory-api:latest .

# Run container
docker run -d \
  -p 5000:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Database=inventorydb;Username=user;Password=pass" \
  -e JwtSettings__SecretKey="your-secret-key-min-32-chars" \
  --name inventory-api \
  inventory-api:latest
```

### Kubernetes Deployment

Example health check configuration:

```yaml
livenessProbe:
  httpGet:
    path: /api/v1/health
    port: 80
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /api/v1/database/status
    port: 80
  initialDelaySeconds: 10
  periodSeconds: 5
```

### Environment Variables

| Variable | Description | Required | Default |
|----------|-------------|----------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) | Yes | Development |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | Yes | - |
| `JwtSettings__SecretKey` | JWT signing key (min 32 chars) | Yes | - |
| `JwtSettings__Issuer` | JWT issuer | No | InventoryAPI |
| `JwtSettings__Audience` | JWT audience | No | InventoryAPIUsers |
| `JwtSettings__ExpiryMinutes` | Token expiration time | No | 60 |
| `Serilog__MinimumLevel__Default` | Logging level | No | Information |

### Pre-Deployment Checklist

Before deploying to production:

- [ ] Database backup created
- [ ] Migrations reviewed and tested
- [ ] Environment variables configured
- [ ] Secrets properly secured (use Key Vault/Secrets Manager)
- [ ] Health check endpoints verified
- [ ] Monitoring and alerts configured
- [ ] Rollback plan documented
- [ ] All tests passing

See [DEPLOYMENT.md](docs/DEPLOYMENT.md) for the complete production deployment guide.

## 📝 API Documentation

Interactive API documentation is available via Swagger UI when running the application:

- **Swagger UI**: `https://localhost:5001/swagger`
- **OpenAPI JSON**: `https://localhost:5001/swagger/v1/swagger.json`

The Swagger interface provides:
- Complete endpoint documentation
- Request/response schemas
- Try-it-out functionality
- Authentication support

## 🔧 Troubleshooting

### Migration Error: "relation already exists"

If you see this error when starting the API:
```
Npgsql.PostgresException: 42P07: relation "Users" already exists
```

**This means:** The database tables exist but the migration history is out of sync.

**Solution 1: Fresh Start (Recommended if no important data)**
```bash
# Use the provided reset script
./reset-database.sh

# Or manually:
docker-compose down -v
docker-compose up --build
```

**Solution 2: Keep Existing Data**
```bash
# Connect to PostgreSQL
docker exec -it inventory-postgres psql -U inventory_user -d inventory_db

# Mark migration as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251110020900_InitialCreate', '8.0.11');
```

### EF Core Warnings About Query Filters

If you see warnings like:
```
Entity 'User' has a global query filter defined and is the required end of a relationship...
```

**Status:** These warnings have been fixed by adding proper entity configurations in version `v1.1.0+`.

**To get the fix:**
1. Pull the latest code
2. Run `./reset-database.sh` to regenerate migrations
3. Restart the containers

### Port Already in Use

If Docker fails with "port already allocated":
```bash
# Check what's using the ports
netstat -ano | findstr :5000  # Windows
lsof -i :5000                 # Linux/Mac

# Change ports in docker-compose.yml or stop conflicting service
```

### Docker Permission Denied (Linux)

```bash
# Add user to docker group
sudo usermod -aG docker $USER
newgrp docker
```

### Cannot Connect to Database

Ensure PostgreSQL container is running:
```bash
docker ps | grep inventory-postgres

# View logs
docker-compose logs postgres
```

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Coding Standards

- Follow C# coding conventions
- Write unit tests for new features
- Update documentation as needed
- Ensure all tests pass before submitting PR
- Maintain code coverage above 70%

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [ASP.NET Core](https://docs.microsoft.com/aspnet/core)
- Inspired by Clean Architecture principles from Robert C. Martin
- CQRS pattern implementation using [MediatR](https://github.com/jbogard/MediatR)

## 📞 Support

For questions, issues, or feature requests:
- Open an [issue](https://github.com/yourusername/ASP.NET-Core-Inventory-Management-API-/issues)
- Check existing [documentation](https://github.com/yourusername/ASP.NET-Core-Inventory-Management-API-/wiki)
- Review [API examples](https://github.com/yourusername/ASP.NET-Core-Inventory-Management-API-/tree/main/examples)

---

**Built with ❤️ using ASP.NET Core 8**
