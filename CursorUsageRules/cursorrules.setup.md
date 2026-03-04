# Cursor Rules: PersistenceToolkit - Installation & Setup

This file contains comprehensive rules and guidelines for **installing and setting up PersistenceToolkit** in your solution. Follow these rules to ensure proper integration across all layers.

---

## 🎯 Core Principles

### 1. **Layer Separation is Critical**
- ✅ **DO**: Install packages in the correct layers only
- ✅ **DO**: Maintain strict layer boundaries
- ❌ **DON'T**: Never install Infrastructure packages in Domain/Application layers
- ❌ **DON'T**: Never install Domain packages in Infrastructure layer

### 2. **Dependency Direction**
- Domain layer: **Zero dependencies** on persistence
- Application layer: Depends only on **Abstractions**
- Infrastructure layer: Implements all persistence concerns

### 3. **One DbContext Per Solution**
- ✅ **DO**: Create one `BaseContext` implementation in Infrastructure
- ✅ **DO**: Register it as `BaseContext` in DI container
- ❌ **DON'T**: Never create multiple DbContext implementations

---

## 📦 Package Installation by Layer

### Layer 1: Domain Layer

**Package to Install:**
```bash
dotnet add package PersistenceToolkit.Domain
```

**What This Layer Gets:**
- `Entity` base class
- `IAggregateRoot` marker interface
- `AggregateWalker` utility

**What This Layer Should NOT Have:**
- ❌ No references to `PersistenceToolkit.Abstractions`
- ❌ No references to `PersistenceToolkit` (Persistence package)
- ❌ No EF Core references
- ❌ No `DbContext` references

**Example Domain Project Structure:**
```
YourSolution.Domain/
├── Entities/
│   ├── Order.cs (inherits Entity, implements IAggregateRoot)
│   ├── OrderItem.cs (inherits Entity)
│   └── Customer.cs (inherits Entity, implements IAggregateRoot)
├── ValueObjects/
└── YourSolution.Domain.csproj
```

**Domain Project File (.csproj):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="PersistenceToolkit.Domain" Version="10.0.2" />
  </ItemGroup>
</Project>
```

---

### Layer 2: Application Layer

**Package to Install:**
```bash
dotnet add package PersistenceToolkit.Abstractions
```

**What This Layer Gets:**
- `IAggregateRepository<T>`
- `IEntityReadRepository<T>`
- `IGenericReadRepository<T>`
- `ISystemUser` interface
- `ISpecification<T>` and `EntitySpecification<T>`
- `Specification<T>` builder

**What This Layer Should NOT Have:**
- ❌ No references to `PersistenceToolkit` (Persistence package)
- ❌ No `DbContext` references
- ❌ No EF Core references
- ❌ No concrete repository implementations

**Example Application Project Structure:**
```
YourSolution.Application/
├── Services/
│   ├── OrderService.cs (uses IAggregateRepository<Order>)
│   └── OrderQueryService.cs (uses IEntityReadRepository<Order>)
├── Specifications/
│   └── ActiveOrdersSpec.cs (inherits EntitySpecification<Order>)
├── DTOs/
└── YourSolution.Application.csproj
```

**Application Project File (.csproj):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="PersistenceToolkit.Abstractions" Version="10.0.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\YourSolution.Domain\YourSolution.Domain.csproj" />
  </ItemGroup>
</Project>
```

---

### Layer 3: Infrastructure Layer

**Package to Install:**
```bash
dotnet add package PersistenceToolkit
```

**What This Layer Gets:**
- `BaseContext` abstract class
- `EntityStateProcessor`
- `NavigationIgnoreTracker`
- Concrete repository implementations
- Specification evaluators
- EF Core integration

**What This Layer MUST Have:**
- ✅ Reference to `PersistenceToolkit.Abstractions`
- ✅ Reference to `PersistenceToolkit.Domain` (for entity configurations)
- ✅ EF Core packages (`Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.SqlServer`)

**Example Infrastructure Project Structure:**
```
YourSolution.Infrastructure/
├── Persistence/
│   ├── YourDbContext.cs (inherits BaseContext)
│   └── Configurations/
│       ├── OrderConfiguration.cs (inherits BaseConfiguration<Order>)
│       └── OrderItemConfiguration.cs
├── Repositories/ (optional - if you need custom repositories)
├── Services/
│   └── SystemUserService.cs (implements ISystemUser)
└── YourSolution.Infrastructure.csproj
```

**Infrastructure Project File (.csproj):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="PersistenceToolkit" Version="10.0.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.7" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.7" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\YourSolution.Domain\YourSolution.Domain.csproj" />
    <ProjectReference Include="..\YourSolution.Application\YourSolution.Application.csproj" />
  </ItemGroup>
</Project>
```

---

## 🚀 Step-by-Step Setup Guide

### Step 1: Install Packages in Correct Layers

```bash
# Navigate to Domain project
cd src/YourSolution.Domain
dotnet add package PersistenceToolkit.Domain --version 10.0.2

# Navigate to Application project
cd ../YourSolution.Application
dotnet add package PersistenceToolkit.Abstractions --version 10.0.2

# Navigate to Infrastructure project
cd ../YourSolution.Infrastructure
dotnet add package PersistenceToolkit --version 10.0.2
dotnet add package Microsoft.EntityFrameworkCore --version 9.0.7
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.7
```

---

### Step 2: Create Domain Entities

**Location:** `YourSolution.Domain/Entities/`

```csharp
// Order.cs
using PersistenceToolkit.Domain;

namespace YourSolution.Domain.Entities
{
    public class Order : Entity, IAggregateRoot
    {
        public string OrderNumber { get; set; }
        public int CustomerId { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        
        // Navigation properties
        public virtual Customer Customer { get; set; }
        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        
        // Domain methods
        public void AddItem(OrderItem item)
        {
            Items.Add(item);
            RecalculateTotal();
        }
        
        private void RecalculateTotal()
        {
            TotalAmount = Items.Sum(i => i.Quantity * i.UnitPrice);
        }
    }
}

// OrderItem.cs
using PersistenceToolkit.Domain;

namespace YourSolution.Domain.Entities
{
    public class OrderItem : Entity
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        
        // Navigation properties
        public virtual Order Order { get; set; }
    }
}
```

**Key Points:**
- ✅ All entities inherit from `Entity`
- ✅ Aggregate roots implement `IAggregateRoot`
- ✅ Child entities do NOT implement `IAggregateRoot`
- ✅ Use virtual for navigation properties (EF Core requirement)

---

### Step 3: Implement ISystemUser

**Location:** `YourSolution.Infrastructure/Services/` or `YourSolution.Infrastructure/`

```csharp
// SystemUserService.cs
using PersistenceToolkit.Abstractions;
using Microsoft.AspNetCore.Http;

namespace YourSolution.Infrastructure.Services
{
    public class SystemUserService : ISystemUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public SystemUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        
        public int UserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User
                    .FindFirst("UserId")?.Value;
                return int.TryParse(userIdClaim, out var userId) ? userId : 0;
            }
        }
        
        public int TenantId
        {
            get
            {
                var tenantIdClaim = _httpContextAccessor.HttpContext?.User
                    .FindFirst("TenantId")?.Value;
                return int.TryParse(tenantIdClaim, out var tenantId) ? tenantId : 0;
            }
        }
    }
}

// Alternative: Simple implementation for testing
public class SystemUser : ISystemUser
{
    public int UserId { get; set; }
    public int TenantId { get; set; }
}
```

**Key Points:**
- ✅ Must implement `ISystemUser` interface
- ✅ Should extract `UserId` and `TenantId` from current context (HTTP, claims, session, etc.)
- ✅ Register as Scoped in DI container

---

### Step 4: Create Your DbContext

**Location:** `YourSolution.Infrastructure/Persistence/`

```csharp
// YourDbContext.cs
using Microsoft.EntityFrameworkCore;
using PersistenceToolkit.Persistence.Persistence;
using YourSolution.Domain.Entities;

namespace YourSolution.Infrastructure.Persistence
{
    public class YourDbContext : BaseContext
    {
        // Constructor with connection string
        public YourDbContext(string connectionString) : base(connectionString)
        {
        }
        
        // Constructor with DbContextOptions (for DI)
        public YourDbContext(DbContextOptions<BaseContext> options) : base(options)
        {
        }
        
        // DbSets
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Customer> Customers { get; set; }
        
        // Apply entity configurations
        protected override void ApplyConfiguration(ModelBuilder modelBuilder)
        {
            // Apply all IEntityTypeConfiguration<T> configurations
            modelBuilder.ApplyConfiguration(new OrderConfiguration());
            modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
            modelBuilder.ApplyConfiguration(new CustomerConfiguration());
            
            // Or scan assembly for all configurations
            // modelBuilder.ApplyConfigurationsFromAssembly(typeof(YourDbContext).Assembly);
        }
        
        // Manual configurations
        protected override void DefineManualConfiguration(ModelBuilder modelBuilder)
        {
            // Define any manual configurations here
            // Optionally mark navigation properties to ignore on update:
            // modelBuilder.Entity<Order>()
            //     .IgnoreOnUpdate(o => o.SomeNavigationProperty);
        }
    }
}
```

**Key Points:**
- ✅ Must inherit from `BaseContext` (not `DbContext`)
- ✅ Must implement `ApplyConfiguration()` method
- ✅ Must implement `DefineManualConfiguration()` method
- ✅ Can use either connection string or `DbContextOptions<BaseContext>` constructor

---

### Step 5: Create Entity Configurations

**Location:** `YourSolution.Infrastructure/Persistence/Configurations/`

```csharp
// OrderConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersistenceToolkit.Persistence.Configuration;
using PersistenceToolkit.Persistence.Persistence;
using YourSolution.Domain.Entities;

namespace YourSolution.Infrastructure.Persistence.Configurations
{
    public class OrderConfiguration : BaseConfiguration<Order>
    {
        public override void Configure(EntityTypeBuilder<Order> builder)
        {
            base.Configure(builder); // ✅ Always call base first
            
            // Table name
            builder.ToTable("Orders");
            
            // Properties
            builder.Property(o => o.OrderNumber)
                .IsRequired()
                .HasMaxLength(50);
            
            builder.Property(o => o.TotalAmount)
                .HasPrecision(18, 2);
            
            // Relationships
            builder.HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Navigation ignore (if needed)
            builder.IgnoreOnUpdate(o => o.Customer); // Customer won't be updated when Order is saved
        }
    }
}
```

**Key Points:**
- ✅ Inherit from `BaseConfiguration<T>` (not `IEntityTypeConfiguration<T>`)
- ✅ Always call `base.Configure(builder)` first
- ✅ Use `IgnoreOnUpdate()` for navigation properties that shouldn't be updated

---

### Step 6: Register Services in DI Container

**Location:** `YourSolution.Api/Program.cs` or `YourSolution.Api/Startup.cs`

#### For .NET 6+ (Minimal APIs / Program.cs):

```csharp
using PersistenceToolkit.Persistence;
using PersistenceToolkit.Persistence.Persistence;
using PersistenceToolkit.Abstractions;
using YourSolution.Infrastructure.Persistence;
using YourSolution.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor(); // Required for SystemUserService

// 1. Register ISystemUser
builder.Services.AddScoped<ISystemUser>(serviceProvider =>
{
    // Option A: Use service that reads from HTTP context
    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
    return new SystemUserService(httpContextAccessor);
    
    // Option B: Simple implementation (for testing)
    // return new SystemUser { UserId = 1, TenantId = 1 };
});

// 2. Register BaseContext (your DbContext)
builder.Services.AddScoped<BaseContext>(serviceProvider =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    return new YourDbContext(connectionString);
    
    // Alternative: Using DbContextOptions (recommended for production)
    // var optionsBuilder = new DbContextOptionsBuilder<BaseContext>();
    // optionsBuilder.UseSqlServer(connectionString);
    // return new YourDbContext(optionsBuilder.Options);
});

// 3. Register PersistenceToolkit repositories
builder.Services.AddPersistenceToolkit();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

#### For .NET Core 3.1 / .NET 5 (Startup.cs):

```csharp
using PersistenceToolkit.Persistence;
using PersistenceToolkit.Persistence.Persistence;
using PersistenceToolkit.Abstractions;
using YourSolution.Infrastructure.Persistence;
using YourSolution.Infrastructure.Services;

public class Startup
{
    public IConfiguration Configuration { get; }
    
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddHttpContextAccessor();
        
        // 1. Register ISystemUser
        services.AddScoped<ISystemUser>(serviceProvider =>
        {
            var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            return new SystemUserService(httpContextAccessor);
        });
        
        // 2. Register BaseContext
        services.AddScoped<BaseContext>(serviceProvider =>
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            return new YourDbContext(connectionString);
        });
        
        // 3. Register PersistenceToolkit
        services.AddPersistenceToolkit();
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Configure pipeline
    }
}
```

**Key Points:**
- ✅ Register `ISystemUser` as **Scoped**
- ✅ Register `BaseContext` as **Scoped** (not Singleton or Transient)
- ✅ Call `AddPersistenceToolkit()` extension method
- ✅ Ensure `IHttpContextAccessor` is registered if using HTTP-based `ISystemUser`

---

### Step 7: Configure Connection String

**Location:** `appsettings.json` or `appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=YourDatabase;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

---

### Step 8: Create Database Migration

```bash
# Navigate to Infrastructure project
cd src/YourSolution.Infrastructure

# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate --startup-project ../YourSolution.Api

# Apply migration to database
dotnet ef database update --startup-project ../YourSolution.Api
```

**Note:** If your startup project is different, adjust the path accordingly.

---

## ✅ Verification Checklist

After setup, verify the following:

### Package Installation
- [ ] `PersistenceToolkit.Domain` installed in Domain project only
- [ ] `PersistenceToolkit.Abstractions` installed in Application project only
- [ ] `PersistenceToolkit` installed in Infrastructure project only
- [ ] No cross-layer package references

### Domain Layer
- [ ] All entities inherit from `Entity`
- [ ] Aggregate roots implement `IAggregateRoot`
- [ ] No EF Core or persistence references

### Application Layer
- [ ] Services use repository interfaces (not concrete classes)
- [ ] No `DbContext` references
- [ ] Specifications inherit from `EntitySpecification<T>` or `Specification<T>`

### Infrastructure Layer
- [ ] `YourDbContext` inherits from `BaseContext`
- [ ] `ApplyConfiguration()` implemented
- [ ] `DefineManualConfiguration()` implemented
- [ ] Entity configurations inherit from `BaseConfiguration<T>`
- [ ] `ISystemUser` implementation created

### Dependency Injection
- [ ] `ISystemUser` registered as Scoped
- [ ] `BaseContext` registered as Scoped
- [ ] `AddPersistenceToolkit()` called
- [ ] `IHttpContextAccessor` registered (if needed)

### Database
- [ ] Connection string configured
- [ ] Migration created and applied
- [ ] Database created successfully

---

## 🚫 Common Mistakes to Avoid

### ❌ Wrong Package in Wrong Layer

```xml
<!-- ❌ BAD: Application project should NOT have PersistenceToolkit -->
<ItemGroup>
  <PackageReference Include="PersistenceToolkit" Version="10.0.2" />
</ItemGroup>

<!-- ✅ GOOD: Application project should only have Abstractions -->
<ItemGroup>
  <PackageReference Include="PersistenceToolkit.Abstractions" Version="10.0.2" />
</ItemGroup>
```

### ❌ Wrong DbContext Base Class

```csharp
// ❌ BAD: Inheriting from DbContext directly
public class YourDbContext : DbContext
{
    // Missing PersistenceToolkit functionality
}

// ✅ GOOD: Inheriting from BaseContext
public class YourDbContext : BaseContext
{
    // Has all PersistenceToolkit functionality
}
```

### ❌ Wrong Configuration Base Class

```csharp
// ❌ BAD: Implementing IEntityTypeConfiguration directly
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // Missing base configuration
    }
}

// ✅ GOOD: Inheriting from BaseConfiguration
public class OrderConfiguration : BaseConfiguration<Order>
{
    public override void Configure(EntityTypeBuilder<Order> builder)
    {
        base.Configure(builder); // ✅ Always call base first
        // Your configurations
    }
}
```

### ❌ Wrong Repository Registration

```csharp
// ❌ BAD: Registering concrete repositories manually
services.AddScoped<IAggregateRepository<Order>, AggregateRepository<Order>>();

// ✅ GOOD: Use AddPersistenceToolkit() extension method
services.AddPersistenceToolkit();
```

### ❌ Wrong DbContext Registration

```csharp
// ❌ BAD: Registering as YourDbContext instead of BaseContext
services.AddScoped<YourDbContext>(...);

// ✅ GOOD: Register as BaseContext
services.AddScoped<BaseContext>(serviceProvider =>
{
    return new YourDbContext(connectionString);
});
```

### ❌ Missing base.Configure() Call

```csharp
// ❌ BAD: Not calling base.Configure()
public override void Configure(EntityTypeBuilder<Order> builder)
{
    builder.ToTable("Orders");
    // Missing base.Configure(builder)
}

// ✅ GOOD: Always call base.Configure() first
public override void Configure(EntityTypeBuilder<Order> builder)
{
    base.Configure(builder); // ✅ Required
    builder.ToTable("Orders");
}
```

---

## 🎯 Complete Setup Example

Here's a complete minimal setup example:

### 1. Domain Entity
```csharp
// Domain/Entities/Product.cs
using PersistenceToolkit.Domain;

namespace YourSolution.Domain.Entities
{
    public class Product : Entity, IAggregateRoot
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
```

### 2. Application Service
```csharp
// Application/Services/ProductService.cs
using PersistenceToolkit.Abstractions.Repositories;
using YourSolution.Domain.Entities;

namespace YourSolution.Application.Services
{
    public class ProductService
    {
        private readonly IAggregateRepository<Product> _productRepository;
        
        public ProductService(IAggregateRepository<Product> productRepository)
        {
            _productRepository = productRepository;
        }
        
        public async Task<Product> CreateProductAsync(string name, decimal price)
        {
            var product = new Product { Name = name, Price = price };
            await _productRepository.Save(product);
            return product;
        }
    }
}
```

### 3. Infrastructure DbContext
```csharp
// Infrastructure/Persistence/YourDbContext.cs
using Microsoft.EntityFrameworkCore;
using PersistenceToolkit.Persistence.Persistence;
using YourSolution.Domain.Entities;

namespace YourSolution.Infrastructure.Persistence
{
    public class YourDbContext : BaseContext
    {
        public YourDbContext(string connectionString) : base(connectionString) { }
        public YourDbContext(DbContextOptions<BaseContext> options) : base(options) { }
        
        public DbSet<Product> Products { get; set; }
        
        protected override void ApplyConfiguration(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new ProductConfiguration());
        }
        
        protected override void DefineManualConfiguration(ModelBuilder modelBuilder)
        {
            // Empty or manual configurations
        }
    }
}
```

### 4. Entity Configuration
```csharp
// Infrastructure/Persistence/Configurations/ProductConfiguration.cs
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersistenceToolkit.Persistence.Configuration;
using YourSolution.Domain.Entities;

namespace YourSolution.Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : BaseConfiguration<Product>
    {
        public override void Configure(EntityTypeBuilder<Product> builder)
        {
            base.Configure(builder);
            builder.ToTable("Products");
            builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Price).HasPrecision(18, 2);
        }
    }
}
```

### 5. DI Registration
```csharp
// Api/Program.cs
using PersistenceToolkit.Persistence;
using PersistenceToolkit.Persistence.Persistence;
using PersistenceToolkit.Abstractions;
using YourSolution.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ISystemUser>(sp => 
    new SystemUser { UserId = 1, TenantId = 1 });

builder.Services.AddScoped<BaseContext>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    return new YourDbContext(connectionString);
});

builder.Services.AddPersistenceToolkit();

var app = builder.Build();
app.Run();
```

---

## 📝 Code Generation Guidelines

When generating setup code:

1. **Always install packages in correct layers** - Never suggest wrong package in wrong layer
2. **Always inherit from BaseContext** - Never suggest inheriting from DbContext
3. **Always inherit from BaseConfiguration** - Never suggest IEntityTypeConfiguration
4. **Always call base.Configure()** - In all configuration classes
5. **Always register as BaseContext** - Not as concrete DbContext type
6. **Always use AddPersistenceToolkit()** - Never register repositories manually
7. **Always implement ISystemUser** - Required for audit and tenant filtering
8. **Always use Scoped lifetime** - For ISystemUser and BaseContext
9. **Always check package references** - Ensure no cross-layer dependencies
10. **Always verify entity inheritance** - Entities must inherit from Entity, aggregates must implement IAggregateRoot

---

## 🎯 Summary Checklist

Before considering setup complete:

- [ ] Packages installed in correct layers
- [ ] Domain entities inherit from `Entity`
- [ ] Aggregate roots implement `IAggregateRoot`
- [ ] DbContext inherits from `BaseContext`
- [ ] Entity configurations inherit from `BaseConfiguration<T>`
- [ ] `base.Configure()` called in all configurations
- [ ] `ISystemUser` implemented and registered
- [ ] `BaseContext` registered (not concrete type)
- [ ] `AddPersistenceToolkit()` called
- [ ] Connection string configured
- [ ] Migration created and applied
- [ ] No cross-layer package references
- [ ] All services use repository interfaces (not concrete classes)

