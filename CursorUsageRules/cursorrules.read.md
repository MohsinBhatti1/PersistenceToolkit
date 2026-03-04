# Cursor Rules: PersistenceToolkit - Read Operations

This file contains rules and best practices for implementing **read operations** using PersistenceToolkit in your solution.

---

## 🎯 Core Principles

### 1. **Always Use Repository Abstractions**
- ✅ **DO**: Inject `IGenericReadRepository<T>`, `IEntityReadRepository<T>`, or `IAggregateReadRepository<T>` in your Application layer
- ❌ **DON'T**: Never inject `DbContext` or concrete repository implementations in Application/Domain layers
- ❌ **DON'T**: Never use `DbSet<T>` directly in business logic

### 2. **Use Specifications for All Queries**
- ✅ **DO**: Always use `Specification<T>` or `EntitySpecification<T>` for querying
- ✅ **DO**: Create derived classes from `EntitySpecification<T>` (it's abstract - cannot be instantiated directly)
- ✅ **DO**: Build specifications using fluent API: `.Where()`, `.Include()`, `.OrderBy()`, etc.
- ❌ **DON'T**: Never write raw LINQ queries outside of specifications
- ❌ **DON'T**: Never use `IQueryable<T>` directly in application services
- ❌ **DON'T**: Never try to instantiate `EntitySpecification<T>` directly (it's abstract - must inherit from it)

### 3. **Respect Aggregate Boundaries**
- ✅ **DO**: Use `IAggregateReadRepository<T>` for reading aggregate roots
- ✅ **DO**: Always load aggregates through their root entity
- ❌ **DON'T**: Never query child entities directly - always go through the aggregate root

---

## 📚 Repository Selection Guide

### When to Use `IGenericReadRepository<T>`
- For generic queries on any entity type
- When you need maximum flexibility
- For read-only operations that don't require entity-specific features

```csharp
// ✅ Good: Generic read repository
public class ReportService
{
    private readonly IGenericReadRepository<Order> _orderRepository;
    
    public async Task<ReportData> GenerateReportAsync()
    {
        var spec = new Specification<Order>()
            .Where(o => o.Status == OrderStatus.Confirmed)
            .OrderByDescending(o => o.CreatedOn);
            
        var orders = await _orderRepository.ListAsync(spec);
        // Process orders...
    }
}
```

### When to Use `IEntityReadRepository<T>`
- For reading entities that inherit from `Entity`
- When you need `GetByIdAsync()` convenience method
- When you want automatic tenant filtering and soft delete filtering

```csharp
// ✅ Good: Entity read repository with GetByIdAsync
public class OrderService
{
    private readonly IEntityReadRepository<Order> _orderRepository;
    
    public async Task<OrderDto> GetOrderAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null) throw new NotFoundException();
        return MapToDto(order);
    }
}
```

### When to Use `IAggregateReadRepository<T>`
- For reading aggregate roots (entities implementing `IAggregateRoot`)
- When you need to load entire aggregate graphs
- When working with DDD aggregates

```csharp
// ✅ Good: Aggregate read repository
public class OrderService
{
    private readonly IAggregateReadRepository<Order> _orderRepository;
    
    public async Task<Order> GetOrderWithItemsAsync(int orderId)
    {
        var spec = new EntitySpecification<Order>()
            .Where(o => o.Id == orderId)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Customer);
            
        return await _orderRepository.FirstOrDefaultAsync(spec);
    }
}
```

---

## 🔍 Specification Patterns

### Basic Query Pattern

```csharp
// ✅ Good: Simple query with specification
var spec = new Specification<Order>()
    .Where(o => o.Status == OrderStatus.Pending)
    .OrderBy(o => o.CreatedOn)
    .Take(10);

var orders = await _repository.ListAsync(spec);
```

### Entity Specification Pattern (Recommended for Entities)

**Important:** `EntitySpecification<T>` and `EntitySpecification<T, TResult>` are **abstract classes** - you **must** create derived classes. You cannot instantiate them directly.

```csharp
// ✅ Good: Entity specification with automatic tenant/soft delete filtering
// Create a derived class from EntitySpecification<T>
public class ActiveOrdersSpec : EntitySpecification<Order>
{
    public ActiveOrdersSpec()
    {
        Query
            .Where(o => o.Status == OrderStatus.Pending)
            .Include(o => o.Customer)
            .OrderByDescending(o => o.CreatedOn);
    }
}

// Usage - instantiate your derived class
var spec = new ActiveOrdersSpec();
var orders = await _orderRepository.ListAsync(spec);
```

**Why Abstract?**
- `EntitySpecification<T>` provides automatic tenant filtering and soft delete filtering
- The abstract design enforces creating reusable, named specification classes
- Makes specifications testable and maintainable

### Complex Query with Multiple Includes

```csharp
// ✅ Good: Create a derived class for complex queries
public class OrdersByDateRangeSpec : EntitySpecification<Order>
{
    public OrdersByDateRangeSpec(DateTime startDate, DateTime endDate)
    {
        Query
            .Where(o => o.CreatedOn >= startDate)
            .Where(o => o.CreatedOn <= endDate)
            .Include(o => o.Customer)
                .ThenInclude(c => c.Address)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Category)
            .OrderByDescending(o => o.TotalAmount)
            .Take(50);
    }
}

// Usage
var spec = new OrdersByDateRangeSpec(startDate, endDate);
var orders = await _orderRepository.ListAsync(spec);
```

### Pagination Pattern

```csharp
// ✅ Good: Create a derived class for paginated queries
public class ConfirmedOrdersSpec : EntitySpecification<Order>
{
    public ConfirmedOrdersSpec()
    {
        Query
            .Where(o => o.Status == OrderStatus.Confirmed)
            .OrderByDescending(o => o.CreatedOn);
    }
}

// Usage
public async Task<PaginatedResult<Order>> GetOrdersPageAsync(int pageNumber, int pageSize)
{
    var spec = new ConfirmedOrdersSpec();
    var skip = (pageNumber - 1) * pageSize;
    return await _orderRepository.PaginatedListAsync(spec, skip, pageSize);
}
```

### Projection Pattern (Select to DTO)

#### Using EntitySpecification<T, TResult> (Recommended for Entities)

When projecting from entities that inherit from `Entity`, use `EntitySpecification<T, TResult>` to get automatic tenant filtering and soft delete filtering:

```csharp
// ✅ Good: EntitySpecification with projection - gets automatic tenant/soft delete filtering
public class OrderSummarySpec : EntitySpecification<Order, OrderSummaryDto>
{
    public OrderSummarySpec()
    {
        Query
            .Where(o => o.Status == OrderStatus.Confirmed)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                TotalAmount = o.TotalAmount,
                CustomerName = o.Customer.Name
            })
            .OrderByDescending(o => o.CreatedOn)
            .Take(20);
    }
}

// Usage
var spec = new OrderSummarySpec();
var summaries = await _orderRepository.ListAsync(spec);
// ✅ Automatically filters by TenantId and excludes soft-deleted records
```

#### Using Specification<T, TResult> (For Non-Entity Types)

When projecting from non-entity types or when you don't need tenant/soft delete filtering:

```csharp
// ✅ Good: Generic Specification for non-entity projections
var spec = new Specification<Order, OrderSummaryDto>()
    .Where(o => o.Status == OrderStatus.Confirmed)
    .Select(o => new OrderSummaryDto
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        TotalAmount = o.TotalAmount,
        CustomerName = o.Customer.Name
    })
    .OrderByDescending(o => o.CreatedOn)
    .Take(20);

var summaries = await _orderRepository.ListAsync(spec);
```

#### Inline Projection Pattern (Using Derived Class)

```csharp
// ✅ Good: Create a derived class from EntitySpecification<T, TResult>
public class OrderSummarySpec : EntitySpecification<Order, OrderSummaryDto>
{
    public OrderSummarySpec()
    {
        Query
            .Where(o => o.Status == OrderStatus.Confirmed)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                TotalAmount = o.TotalAmount,
                CustomerName = o.Customer.Name
            })
            .OrderByDescending(o => o.CreatedOn)
            .Take(20);
    }
}

// Usage
var spec = new OrderSummarySpec();
var summaries = await _orderRepository.ListAsync(spec);
```

**Key Difference:**
- `EntitySpecification<T, TResult>`: Use when `T` is an `Entity` - provides automatic tenant filtering and soft delete filtering
- `Specification<T, TResult>`: Use for non-entity types or when you don't need automatic filtering

### Conditional Filtering Pattern

```csharp
// ✅ Good: Create a derived class that accepts parameters for conditional filtering
public class OrderSearchSpec : EntitySpecification<Order>
{
    public OrderSearchSpec(OrderSearchCriteria criteria)
    {
        if (criteria.Status.HasValue)
            Query.Where(o => o.Status == criteria.Status.Value);
        
        if (criteria.CustomerId.HasValue)
            Query.Where(o => o.CustomerId == criteria.CustomerId.Value);
        
        if (criteria.StartDate.HasValue)
            Query.Where(o => o.CreatedOn >= criteria.StartDate.Value);
        
        if (criteria.EndDate.HasValue)
            Query.Where(o => o.CreatedOn <= criteria.EndDate.Value);
        
        Query.OrderByDescending(o => o.CreatedOn);
    }
}

// Usage
public async Task<List<Order>> SearchOrdersAsync(OrderSearchCriteria criteria)
{
    var spec = new OrderSearchSpec(criteria);
    return await _orderRepository.ListAsync(spec);
}
```

---

## 🚫 Anti-Patterns to Avoid

### ❌ Direct DbContext Usage

```csharp
// ❌ BAD: Direct DbContext access
public class OrderService
{
    private readonly DbContext _context;
    
    public async Task<List<Order>> GetOrdersAsync()
    {
        return await _context.Set<Order>()
            .Where(o => o.Status == OrderStatus.Pending)
            .ToListAsync();
    }
}
```

### ❌ Raw LINQ Outside Specifications

```csharp
// ❌ BAD: Raw LINQ in application service
public class OrderService
{
    private readonly IQueryable<Order> _orders;
    
    public async Task<List<Order>> GetOrdersAsync()
    {
        return await _orders
            .Where(o => o.Status == OrderStatus.Pending)
            .ToListAsync();
    }
}
```

### ❌ Querying Child Entities Directly

```csharp
// ❌ BAD: Querying child entity directly
public class OrderItemService
{
    private readonly IEntityReadRepository<OrderItem> _itemRepository;
    
    public async Task<List<OrderItem>> GetItemsAsync(int orderId)
    {
        // ❌ Don't query child entities directly
        var spec = new EntitySpecification<OrderItem>()
            .Where(i => i.OrderId == orderId);
        return await _itemRepository.ListAsync(spec);
    }
}

// ✅ GOOD: Query through aggregate root
public class OrderService
{
    private readonly IAggregateReadRepository<Order> _orderRepository;
    
    public async Task<List<OrderItem>> GetItemsAsync(int orderId)
    {
        var spec = new EntitySpecification<Order>()
            .Where(o => o.Id == orderId)
            .Include(o => o.Items);
            
        var order = await _orderRepository.FirstOrDefaultAsync(spec);
        return order?.Items.ToList() ?? new List<OrderItem>();
    }
}
```

### ❌ Not Using EntitySpecification for Entities

```csharp
// ❌ BAD: Using generic Specification for Entity
var spec = new Specification<Order>() // Missing tenant/soft delete filtering
    .Where(o => o.Status == OrderStatus.Pending);

// ❌ BAD: Trying to instantiate EntitySpecification directly (it's abstract!)
var spec = new EntitySpecification<Order>() // ❌ Compilation error - cannot instantiate abstract class
    .Where(o => o.Status == OrderStatus.Pending);

// ✅ GOOD: Create a derived class from EntitySpecification
public class PendingOrdersSpec : EntitySpecification<Order>
{
    public PendingOrdersSpec()
    {
        Query.Where(o => o.Status == OrderStatus.Pending);
    }
}

// Usage
var spec = new PendingOrdersSpec();
var orders = await _orderRepository.ListAsync(spec);
```

### ❌ Using Generic Specification for Entity Projections

```csharp
// ❌ BAD: Using Specification<T, TResult> for Entity projections - missing tenant/soft delete filtering
var spec = new Specification<Order, OrderSummaryDto>()
    .Where(o => o.Status == OrderStatus.Confirmed)
    .Select(o => new OrderSummaryDto { /* ... */ });

// ❌ BAD: Trying to instantiate EntitySpecification<T, TResult> directly (it's abstract!)
var spec = new EntitySpecification<Order, OrderSummaryDto>() // ❌ Compilation error
    .Where(o => o.Status == OrderStatus.Confirmed)
    .Select(o => new OrderSummaryDto { /* ... */ });

// ✅ GOOD: Create a derived class from EntitySpecification<T, TResult>
public class OrderSummarySpec : EntitySpecification<Order, OrderSummaryDto>
{
    public OrderSummarySpec()
    {
        Query
            .Where(o => o.Status == OrderStatus.Confirmed)
            .Select(o => new OrderSummaryDto { /* ... */ });
    }
}

// Usage
var spec = new OrderSummarySpec();
var summaries = await _orderRepository.ListAsync(spec);
// ✅ Automatically applies tenant filtering and soft delete filtering
```

---

## 🎨 Best Practices

### 1. **Create Reusable Specifications**

```csharp
// ✅ Good: Reusable specification class
public class PendingOrdersSpec : EntitySpecification<Order>
{
    public PendingOrdersSpec()
    {
        Query
            .Where(o => o.Status == OrderStatus.Pending)
            .Include(o => o.Customer)
            .OrderByDescending(o => o.CreatedOn);
    }
}

// Usage across multiple services
var spec = new PendingOrdersSpec();
var orders = await _orderRepository.ListAsync(spec);
```

### 2. **Use Post-Processing for In-Memory Operations**

```csharp
// ✅ Good: Create a derived class with post-processing
public class HighValueOrdersSpec : EntitySpecification<Order>
{
    public HighValueOrdersSpec()
    {
        Query
            .Where(o => o.Status == OrderStatus.Confirmed)
            .PostProcessing(orders => orders
                .Where(o => o.TotalAmount > 1000)
                .OrderByDescending(o => o.TotalAmount));
    }
}

// Usage
var spec = new HighValueOrdersSpec();
var orders = await _orderRepository.ListAsync(spec);
```

### 3. **Leverage AsNoTracking for Read-Only Operations**

```csharp
// ✅ Good: Create a derived class with AsNoTracking
public class ReadOnlyOrdersSpec : EntitySpecification<Order>
{
    public ReadOnlyOrdersSpec()
    {
        Query
            .Where(o => o.Status == OrderStatus.Confirmed)
            .AsNoTracking() // Performance optimization
            .Include(o => o.Items);
    }
}

// Usage
var spec = new ReadOnlyOrdersSpec();
var orders = await _orderRepository.ListAsync(spec);
```

### 4. **Handle Null Results Gracefully**

```csharp
// ✅ Good: Create a derived class for specific queries
public class OrderByIdSpec : EntitySpecification<Order>
{
    public OrderByIdSpec(int orderId)
    {
        Query
            .Where(o => o.Id == orderId)
            .Include(o => o.Items);
    }
}

// Usage
public async Task<OrderDto?> GetOrderAsync(int orderId)
{
    var spec = new OrderByIdSpec(orderId);
    var order = await _orderRepository.FirstOrDefaultAsync(spec);
    
    if (order == null)
        return null; // or throw NotFoundException
    
    return MapToDto(order);
}
```

### 5. **Use CancellationTokens**

```csharp
// ✅ Good: Create a derived class and use cancellation tokens
public class PendingOrdersSpec : EntitySpecification<Order>
{
    public PendingOrdersSpec()
    {
        Query.Where(o => o.Status == OrderStatus.Pending);
    }
}

// Usage
public async Task<List<Order>> GetOrdersAsync(CancellationToken cancellationToken = default)
{
    var spec = new PendingOrdersSpec();
    return await _orderRepository.ListAsync(spec, cancellationToken);
}
```

---

## 🔐 Multi-Tenant Considerations

### Automatic Tenant Filtering

```csharp
// ✅ Good: Create a derived class - EntitySpecification automatically filters by TenantId
public class PendingOrdersSpec : EntitySpecification<Order>
{
    public PendingOrdersSpec()
    {
        Query.Where(o => o.Status == OrderStatus.Pending);
        // TenantId filter is automatically applied based on ISystemUser
    }
}

// Usage
var spec = new PendingOrdersSpec();
var orders = await _orderRepository.ListAsync(spec);

// If you need to query across tenants (admin scenario)
public class AllTenantsPendingOrdersSpec : EntitySpecification<Order>
{
    public AllTenantsPendingOrdersSpec()
    {
        IgnoreCompanyFilter = true; // Disable tenant filtering
        Query.Where(o => o.Status == OrderStatus.Pending);
    }
}
```

---

## 🗑️ Soft Delete Considerations

### Automatic Soft Delete Filtering

```csharp
// ✅ Good: Create a derived class - EntitySpecification automatically excludes soft-deleted records
public class PendingOrdersSpec : EntitySpecification<Order>
{
    public PendingOrdersSpec()
    {
        Query.Where(o => o.Status == OrderStatus.Pending);
        // IsDeleted = true records are automatically excluded
    }
}

// Usage
var spec = new PendingOrdersSpec();
var orders = await _orderRepository.ListAsync(spec);

// If you need to include deleted records
public class AllOrdersIncludingDeletedSpec : EntitySpecification<Order>
{
    public AllOrdersIncludingDeletedSpec()
    {
        IncludeDeletedRecords = true; // Include soft-deleted records
        Query.Where(o => o.Status == OrderStatus.Pending);
    }
}
```

---

## ⚡ Performance Tips

### 1. **Use Projections for Large Datasets**

```csharp
// ✅ Good: Create a derived class from EntitySpecification<T, TResult> for projections
public class OrderSummarySpec : EntitySpecification<Order, OrderSummaryDto>
{
    public OrderSummarySpec()
    {
        Query
            .Where(o => o.Status == OrderStatus.Confirmed)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                TotalAmount = o.TotalAmount
            })
            .OrderByDescending(o => o.CreatedOn)
            .Take(1000);
    }
}

// Usage
var spec = new OrderSummarySpec();
var summaries = await _orderRepository.ListAsync(spec);
// ✅ Benefits: 
// - Automatic tenant filtering
// - Automatic soft delete filtering
// - Reduced data transfer (only selected fields)
```

### 2. **Limit Includes to What You Need**

```csharp
// ❌ BAD: Over-including
public class OverInclusiveOrderSpec : EntitySpecification<Order>
{
    public OverInclusiveOrderSpec()
    {
        Query
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Include(o => o.ShippingAddress)
            .Include(o => o.BillingAddress)
            .Include(o => o.PaymentDetails)
            .Include(o => o.Notes)
            .Include(o => o.Attachments);
        // Only include what you actually use!
    }
}

// ✅ GOOD: Include only what's needed
public class MinimalOrderSpec : EntitySpecification<Order>
{
    public MinimalOrderSpec()
    {
        Query
            .Include(o => o.Customer)
            .Include(o => o.Items);
    }
}
```

### 3. **Use Take() to Limit Results**

```csharp
// ✅ Good: Create a derived class that limits results
public class RecentPendingOrdersSpec : EntitySpecification<Order>
{
    public RecentPendingOrdersSpec(int limit = 100)
    {
        Query
            .Where(o => o.Status == OrderStatus.Pending)
            .OrderByDescending(o => o.CreatedOn)
            .Take(limit); // Prevent loading too many records
    }
}
```

---

## 📝 Code Generation Guidelines

When generating code for read operations:

1. **Always use repository abstractions** - Never suggest DbContext or DbSet
2. **Always use specifications** - Never suggest raw LINQ
3. **EntitySpecification is abstract** - Always create derived classes from `EntitySpecification<T>` or `EntitySpecification<T, TResult>`. Never try to instantiate them directly.
4. **Prefer EntitySpecification** - For entities inheriting from `Entity`, create derived classes from `EntitySpecification<T>`
5. **Use EntitySpecification<T, TResult> for Entity projections** - When projecting from entities to DTOs, create derived classes from `EntitySpecification<T, TResult>` instead of `Specification<T, TResult>` to get automatic tenant/soft delete filtering
6. **Include cancellation tokens** - In all async methods
7. **Handle null results** - Always check for null after FirstOrDefault/SingleOrDefault
8. **Use appropriate repository type** - Based on whether it's an aggregate, entity, or generic query
9. **Add proper includes** - But only what's needed
10. **Consider pagination** - For potentially large result sets

---

## 🎯 Summary Checklist

Before implementing a read operation, ensure:

- [ ] Using appropriate repository interface (IGenericReadRepository, IEntityReadRepository, or IAggregateReadRepository)
- [ ] Using Specification or EntitySpecification (not raw LINQ)
- [ ] **Creating derived classes from EntitySpecification** (it's abstract - cannot be instantiated directly)
- [ ] Using EntitySpecification<T> derived classes for entity queries (not Specification<T>)
- [ ] Using EntitySpecification<T, TResult> derived classes for entity projections (not Specification<T, TResult>)
- [ ] Respecting aggregate boundaries (query through root)
- [ ] Including only necessary navigation properties
- [ ] Handling null results appropriately
- [ ] Using cancellation tokens
- [ ] Considering pagination for large datasets
- [ ] Using projections when appropriate
- [ ] Not accessing DbContext directly

