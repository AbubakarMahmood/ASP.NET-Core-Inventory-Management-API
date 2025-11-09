# Building a Production-Grade Inventory Management API with ASP.NET Core 8

This comprehensive guide walks through building a production-ready REST API for inventory and work order management, demonstrating enterprise-level patterns and best practices.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Domain Layer](#domain-layer)
3. [Infrastructure Layer](#infrastructure-layer)
4. [Application Layer](#application-layer)
5. [API Layer](#api-layer)
6. [Testing Strategy](#testing-strategy)
7. [Deployment](#deployment)
8. [Common Patterns](#common-patterns)
9. [Best Practices](#best-practices)

## Architecture Overview

This API follows **Clean Architecture** principles with clear separation of concerns:

```
┌──────────────────────────────────────────────────────┐
│                     API Layer                         │
│  Controllers, Middleware, Authentication, Swagger     │
└────────────────────┬─────────────────────────────────┘
                     │
┌────────────────────▼─────────────────────────────────┐
│                Application Layer                      │
│     CQRS Commands/Queries, DTOs, Validators          │
└────────────────────┬─────────────────────────────────┘
                     │
┌────────────────────▼─────────────────────────────────┐
│               Infrastructure Layer                    │
│    DbContext, Repositories, External Services         │
└────────────────────┬─────────────────────────────────┘
                     │
┌────────────────────▼─────────────────────────────────┐
│                  Domain Layer                         │
│     Entities, Value Objects, Domain Logic             │
└──────────────────────────────────────────────────────┘
```

### Key Architectural Decisions

1. **Dependency Rule**: Dependencies point inward. Domain has no dependencies, Application depends on Domain, Infrastructure implements interfaces from Application, and API orchestrates everything.

2. **CQRS Pattern**: Separates read and write operations for better scalability and maintainability.

3. **Repository + Unit of Work**: Abstracts data access and manages transactions.

4. **Mediator Pattern**: Using MediatR to decouple request handling.

---

## Domain Layer

The Domain layer is the **heart of your application** containing business logic and rules.

### 1. Base Entity Classes

Start with common base classes that all entities inherit:

```csharp
// BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
```

**Why Guid?**
- Natural keys avoid collisions in distributed systems
- No database roundtrip needed to get ID after insert
- Better for security (harder to guess)

**Why RowVersion?**
- Implements optimistic concurrency control
- Prevents lost updates when multiple users edit same record
- EF Core automatically manages versioning

### 2. Auditable Entity

```csharp
// BaseAuditableEntity.cs
public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

**Key Features:**
- **Audit Trail**: Track who created/modified records and when
- **Soft Deletes**: Mark records as deleted without actually removing them
- **UTC Timestamps**: Always use UTC to avoid timezone issues

### 3. Domain Entities with Business Logic

```csharp
// WorkOrder.cs - Example of rich domain model
public class WorkOrder : BaseAuditableEntity
{
    // Properties...

    // Business logic methods enforce rules
    public void Submit()
    {
        if (Status != WorkOrderStatus.Draft)
            throw new BusinessRuleViolationException(
                "Only draft work orders can be submitted.");

        if (!Items.Any())
            throw new BusinessRuleViolationException(
                "Cannot submit work order without items.");

        Status = WorkOrderStatus.Submitted;
    }
}
```

**Why Business Logic in Entities?**
- **Domain-Driven Design**: Entities aren't just data containers
- **Encapsulation**: Rules are enforced at the domain level
- **Testability**: Easy to unit test business rules
- **Consistency**: One place to enforce business rules

### 4. Domain Exceptions

```csharp
// Custom exceptions for different scenarios
public class NotFoundException : DomainException { }
public class ValidationException : DomainException { }
public class BusinessRuleViolationException : DomainException { }
public class InsufficientStockException : BusinessRuleViolationException { }
```

**Benefits:**
- Clear intent in code
- Different handling strategies per exception type
- Better error messages for users

---

## Infrastructure Layer

The Infrastructure layer implements the interfaces defined by the Application layer.

### 1. DbContext Configuration

```csharp
public class ApplicationDbContext : DbContext
{
    // Automatic soft delete filtering
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global query filter for soft deletes
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(GenerateSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    // Automatic audit field management
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedAt = DateTime.UtcNow;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
```

**Key Features:**
- **Automatic Soft Deletes**: Globally filter deleted records from all queries
- **Audit Automation**: Timestamps automatically set on create/update/delete
- **Entity Configurations**: Separate files for each entity keep code organized

### 2. Entity Configuration Pattern

```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.SKU)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.SKU)
            .IsUnique();

        builder.Property(p => p.UnitCost)
            .HasPrecision(18, 2);

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        // Indexes for performance
        builder.HasIndex(p => p.Category);
        builder.HasIndex(p => p.CurrentStock);
    }
}
```

**Why Separate Configuration Files?**
- **Single Responsibility**: Each file configures one entity
- **Readability**: Easy to find and modify entity configuration
- **Testability**: Can test configurations in isolation

**Key Configuration Elements:**
- **Indexes**: Improve query performance on frequently filtered columns
- **Precision**: Control decimal precision for money values
- **Unique Constraints**: Enforce business rules at database level
- **Row Version**: Enable optimistic concurrency

### 3. Repository Pattern

```csharp
// Generic repository for common operations
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}
```

**Benefits:**
- **Abstraction**: Hide EF Core details from business logic
- **Testability**: Easy to mock for unit tests
- **Consistency**: Standard way to access data across the application

### 4. Unit of Work Pattern

```csharp
public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Product> Products { get; }
    IRepository<WorkOrder> WorkOrders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

**Why Unit of Work?**
- **Transaction Management**: Coordinate multiple repository operations
- **Single SaveChanges**: All changes committed together or rolled back
- **Consistency**: Ensures data integrity across multiple operations

**Example Usage:**
```csharp
await _unitOfWork.BeginTransactionAsync();
try
{
    var product = await _unitOfWork.Products.GetByIdAsync(productId);
    product.AdjustStock(-quantity);

    var movement = new StockMovement { /* ... */ };
    await _unitOfWork.StockMovements.AddAsync(movement);

    await _unitOfWork.CommitTransactionAsync();
}
catch
{
    await _unitOfWork.RollbackTransactionAsync();
    throw;
}
```

### 5. JWT Token Service

```csharp
public class TokenService : ITokenService
{
    public string GenerateAccessToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!));
        var credentials = new SigningCredentials(securityKey,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

**Security Best Practices:**
- Use strong secret keys (minimum 32 characters)
- Short token expiration (60 minutes)
- Include JTI (JWT ID) for token revocation
- Store refresh tokens securely in database

### 6. Password Hashing Service

```csharp
public class PasswordService : IPasswordService
{
    private const int Iterations = 100000; // PBKDF2 iterations

    public string HashPassword(string password)
    {
        // Generate random salt
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Hash password with salt
        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: 32);

        // Combine salt and hash
        byte[] hashBytes = new byte[48]; // 16 for salt, 32 for hash
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);

        return Convert.ToBase64String(hashBytes);
    }
}
```

**Why PBKDF2?**
- **Industry Standard**: Recommended by NIST
- **Configurable Iterations**: Adjust difficulty as hardware improves
- **Salt**: Prevents rainbow table attacks

---

## Application Layer

The Application layer contains business logic orchestration using CQRS pattern.

### 1. CQRS with MediatR

**Command Example (Write Operation):**
```csharp
// Command
public class CreateProductCommand : IRequest<ProductDto>
{
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    // ... other properties
}

// Handler
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public async Task<ProductDto> Handle(CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        // Check business rules
        var existingProduct = await _unitOfWork.Products
            .FirstOrDefaultAsync(p => p.SKU == request.SKU, cancellationToken);

        if (existingProduct != null)
            throw new ValidationException("SKU", "Product with this SKU already exists");

        // Create entity
        var product = _mapper.Map<Product>(request);

        // Save
        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProductDto>(product);
    }
}
```

**Query Example (Read Operation):**
```csharp
// Query
public class GetProductsQuery : IRequest<PaginatedResult<ProductDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Category { get; set; }
    public bool? LowStockOnly { get; set; }
}

// Handler
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery,
    PaginatedResult<ProductDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public async Task<PaginatedResult<ProductDto>> Handle(GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Products.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(p => p.Category == request.Category);

        if (request.LowStockOnly == true)
            query = query.Where(p => p.CurrentStock <= p.ReorderPoint);

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var productDtos = _mapper.Map<List<ProductDto>>(products);

        return new PaginatedResult<ProductDto>(productDtos, totalCount,
            request.PageNumber, request.PageSize);
    }
}
```

**Why CQRS?**
- **Separation of Concerns**: Read and write logic separated
- **Optimization**: Optimize queries differently than commands
- **Scalability**: Can scale read and write sides independently
- **Simplicity**: Each handler does one thing

### 2. FluentValidation

```csharp
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required")
            .MaximumLength(50).WithMessage("SKU cannot exceed 50 characters");

        RuleFor(x => x.UnitCost)
            .GreaterThan(0).WithMessage("Unit cost must be greater than zero");

        RuleFor(x => x.ReorderQuantity)
            .GreaterThan(0).WithMessage("Reorder quantity must be greater than zero");
    }
}
```

### 3. Validation Pipeline Behavior

```csharp
public class ValidationBehavior<TRequest, TResponse> :
    IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);
        }

        return await next();
    }
}
```

**Benefits:**
- **Automatic Validation**: All requests validated before reaching handlers
- **Declarative**: Validation rules are clear and maintainable
- **Centralized**: One place to handle validation errors

---

## API Layer

The API layer is the entry point for external clients.

### 1. Program.cs Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Logging with Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/inventory-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(MappingProfile).Assembly);
});

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey))
        };
    });

// Swagger with JWT support
builder.Services.AddSwaggerGen(c => {
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
});
```

### 2. Controllers

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Get all products with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<ProductDto>), 200)]
    public async Task<ActionResult<PaginatedResult<ProductDto>>> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? category = null)
    {
        var query = new GetProductsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Category = category
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ProductDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        [FromBody] CreateProductCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
    }
}
```

**Controller Best Practices:**
- **Thin Controllers**: Delegate all logic to MediatR handlers
- **XML Documentation**: Generate Swagger docs automatically
- **ProducesResponseType**: Document all possible responses
- **Role-Based Authorization**: Protect sensitive operations

### 3. Global Exception Handling

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            Message = exception.Message,
            TraceId = context.TraceIdentifier
        };

        response.StatusCode = exception switch
        {
            NotFoundException => (int)HttpStatusCode.NotFound,
            ValidationException => (int)HttpStatusCode.BadRequest,
            BusinessRuleViolationException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        await response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}
```

**Benefits:**
- **Consistent Error Format**: All errors follow same structure
- **Security**: Hide sensitive details in production
- **Logging**: Centralized error logging
- **HTTP Status Codes**: Correct codes for different scenarios

---

## Testing Strategy

### 1. Unit Tests

Test business logic in isolation:

```csharp
public class CreateProductCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CreateProductCommandHandler _handler;

    [Fact]
    public async Task Handle_ValidProduct_ReturnsProductDto()
    {
        // Arrange
        var command = new CreateProductCommand { SKU = "TEST-001", Name = "Test" };
        var product = new Product { Id = Guid.NewGuid(), SKU = "TEST-001" };

        _unitOfWorkMock.Setup(x => x.Products.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<Product, bool>>>(),
            default))
            .ReturnsAsync((Product)null);

        _mapperMock.Setup(x => x.Map<Product>(command)).Returns(product);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.SKU.Should().Be("TEST-001");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateSKU_ThrowsValidationException()
    {
        // Arrange
        var command = new CreateProductCommand { SKU = "TEST-001" };
        var existingProduct = new Product { SKU = "TEST-001" };

        _unitOfWorkMock.Setup(x => x.Products.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<Product, bool>>>(),
            default))
            .ReturnsAsync(existingProduct);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(command, default));
    }
}
```

### 2. Integration Tests

Test full request/response cycle:

```csharp
public class ProductsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    [Fact]
    public async Task GetProducts_ReturnsSuccessAndProducts()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/products?pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResult<ProductDto>>(content);

        result.Should().NotBeNull();
        result.Items.Should().NotBeEmpty();
    }
}
```

---

## Deployment

### Docker Deployment

```bash
# Build and run with Docker Compose
docker-compose up --build

# The API will be available at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger
```

### Production Checklist

- [ ] Use strong JWT secret keys
- [ ] Enable HTTPS only
- [ ] Configure proper CORS policies
- [ ] Set up centralized logging (ELK, Splunk)
- [ ] Configure health checks
- [ ] Set up monitoring and alerts
- [ ] Use connection pooling
- [ ] Enable response caching where appropriate
- [ ] Set up CI/CD pipeline
- [ ] Configure database backups
- [ ] Use secrets management (Azure Key Vault, AWS Secrets Manager)

---

## Common Patterns

### 1. Pagination Pattern

```csharp
public class PaginatedResult<T>
{
    public List<T> Items { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}
```

### 2. Result Pattern (Optional)

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }

    public static Result<T> Success(T value) => new Result<T>(true, value, null);
    public static Result<T> Failure(string error) => new Result<T>(false, default, error);
}
```

### 3. Specification Pattern

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
}

public class LowStockProductsSpecification : ISpecification<Product>
{
    public Expression<Func<Product, bool>> Criteria =>
        p => p.CurrentStock <= p.ReorderPoint;
}
```

---

## Best Practices

### 1. Security

- **Never trust user input**: Always validate
- **Use parameterized queries**: Prevent SQL injection
- **Hash passwords**: Never store plain text passwords
- **Use HTTPS**: Encrypt data in transit
- **Implement rate limiting**: Prevent abuse
- **Regular security audits**: Keep dependencies updated

### 2. Performance

- **Use async/await**: Don't block threads
- **Implement caching**: Redis, MemoryCache
- **Index database columns**: Speed up queries
- **Use pagination**: Don't load all data at once
- **Optimize N+1 queries**: Use Include()
- **Monitor slow queries**: Use EF Core logging

### 3. Maintainability

- **Keep methods small**: Single Responsibility Principle
- **Write descriptive names**: Code should be self-documenting
- **Add XML comments**: Especially for public APIs
- **Follow SOLID principles**: Better architecture
- **Write tests**: Prevent regressions
- **Use dependency injection**: Loose coupling

### 4. Code Quality

- **Use StyleCop/EditorConfig**: Consistent formatting
- **Enable nullable reference types**: Catch null errors at compile time
- **Use code analyzers**: Roslyn analyzers
- **Code reviews**: Peer review all changes
- **Follow conventions**: Consistent naming and structure

---

## Common Pitfalls to Avoid

### 1. DbContext Pitfalls

❌ **Don't:**
```csharp
// Tracking entities unnecessarily
var products = await _context.Products.ToListAsync();
```

✅ **Do:**
```csharp
// Use AsNoTracking for read-only queries
var products = await _context.Products.AsNoTracking().ToListAsync();
```

### 2. Async/Await Mistakes

❌ **Don't:**
```csharp
// Blocking async code
var result = _mediator.Send(command).Result;
```

✅ **Do:**
```csharp
// Use await properly
var result = await _mediator.Send(command);
```

### 3. Exception Handling

❌ **Don't:**
```csharp
try { /* */ } catch { return null; } // Swallowing exceptions
```

✅ **Do:**
```csharp
try { /* */ }
catch (Exception ex)
{
    _logger.LogError(ex, "Error occurred");
    throw;
}
```

### 4. Security Mistakes

❌ **Don't:**
```csharp
// String interpolation in SQL
_context.Products.FromSqlRaw($"SELECT * FROM Products WHERE Id = {id}");
```

✅ **Do:**
```csharp
// Parameterized queries
_context.Products.FromSqlRaw("SELECT * FROM Products WHERE Id = {0}", id);
```

---

## Conclusion

This API demonstrates production-ready patterns:

- ✅ Clean Architecture with clear separation of concerns
- ✅ CQRS pattern for scalability
- ✅ Repository + Unit of Work for data access
- ✅ Comprehensive validation with FluentValidation
- ✅ JWT authentication with role-based authorization
- ✅ Global exception handling
- ✅ Structured logging with Serilog
- ✅ API versioning for future-proofing
- ✅ Swagger documentation
- ✅ Docker support for easy deployment

## Next Steps

1. **Extend the API**: Add work order and stock movement endpoints
2. **Add more tests**: Increase code coverage
3. **Implement caching**: Redis or MemoryCache
4. **Add real-time features**: SignalR for notifications
5. **Performance monitoring**: Application Insights or Prometheus
6. **CI/CD Pipeline**: GitHub Actions or Azure DevOps

---

**Built with ❤️ following enterprise-grade best practices**
