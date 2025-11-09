# ASP.NET-Core-Inventory-Management-API-
I need you to create a CLAUDE.md file for building a production-grade Inventory Management REST API using ASP.NET Core 8. This should demonstrate enterprise-level patterns and best practices.

Project Overview:
Build a comprehensive inventory and work order management system API that could be used by warehouses or maintenance departments. No UI needed - just a robust, well-documented API.

Tech Stack:
- ASP.NET Core 8 (latest LTS)
- Entity Framework Core 8 with PostgreSQL
- JWT Authentication with refresh tokens
- Fluent Validation
- AutoMapper
- Serilog for structured logging
- xUnit + Moq for testing
- Swagger/OpenAPI documentation

Core Domain Entities:
1. Products/Items
   - SKU, Name, Description, Category
   - Current stock level, reorder point, reorder quantity
   - Unit of measure, cost, location in warehouse
   
2. Work Orders
   - Order number, priority, status workflow
   - Requested by, assigned to, due date
   - Items needed (many-to-many with quantities)
   - Status transitions with business rules
   
3. Stock Movements
   - Type (receipt, issue, adjustment, transfer)
   - Source/destination locations
   - Quantity, reason, timestamp
   - Linked to work orders when applicable

4. Users & Roles
   - Admin, Manager, Operator roles
   - JWT auth with role claims
   - Audit fields (created by, modified by)

Key Features:
- CQRS pattern using MediatR for complex operations
- Repository + Unit of Work patterns
- Global exception handling middleware
- Request/response logging middleware
- Pagination, filtering, and sorting for all GET endpoints
- Optimistic concurrency control using row versions
- Soft deletes with global query filters
- Database migrations and seed data
- Health checks endpoint
- API versioning setup

Business Logic to Implement:
1. Automatic reorder alerts when stock falls below reorder point
2. Work order approval workflow (draft → submitted → approved → in progress → completed)
3. Stock validation (can't issue more than available)
4. FIFO/LIFO/Average costing methods for inventory valuation
5. Stock take/cycle count functionality with variance reporting

API Endpoints Structure:
/api/v1/auth/login
/api/v1/auth/refresh
/api/v1/products (CRUD + bulk operations)
/api/v1/work-orders (CRUD + status transitions)
/api/v1/stock-movements (Create + Query)
/api/v1/reports/low-stock
/api/v1/reports/pending-orders
/api/v1/health

Testing Requirements:
- Unit tests for business logic
- Integration tests for API endpoints using WebApplicationFactory
- Test data builders for readable tests
- Minimum 70% code coverage

Project Structure:
InventoryAPI/
├── src/
│   ├── InventoryAPI.Api/
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Filters/
│   │   └── Program.cs
│   ├── InventoryAPI.Application/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   ├── Validators/
│   │   ├── Mappings/
│   │   └── Interfaces/
│   ├── InventoryAPI.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Exceptions/
│   └── InventoryAPI.Infrastructure/
│       ├── Data/
│       ├── Repositories/
│       └── Services/
└── tests/
├── InventoryAPI.UnitTests/
└── InventoryAPI.IntegrationTests/

Development Approach:
1. Start with clean architecture setup
2. Design database schema with proper indexes
3. Implement auth first
4. Build CRUD for products
5. Add work order workflow
6. Implement stock movement tracking
7. Add business rules and validation
8. Write comprehensive tests
9. Add performance optimizations (caching, query optimization)

Best Practices to Follow:
- Use strongly typed IDs (not just int/guid)
- Implement proper DTOs (no entity exposure)
- Use IAsyncEnumerable for streaming large datasets
- Add ETag support for caching
- Include correlation IDs for request tracking
- Use database transactions appropriately
- Implement idempotency for critical operations

Create a comprehensive CLAUDE.md that guides through building this API step-by-step, with code examples for key patterns, explanations of architectural decisions, and common pitfalls to avoid. Include sample test cases and example API calls.