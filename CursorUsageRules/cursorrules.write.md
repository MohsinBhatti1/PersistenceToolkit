# Cursor Rules: PersistenceToolkit - Write Operations

This file contains rules and best practices for implementing **write operations** (Create, Update, Delete) using PersistenceToolkit in your solution.

---

## 🎯 Core Principles

### 1. **Always Use IAggregateRepository for Write Operations**
- ✅ **DO**: Use `IAggregateRepository<T>` for all write operations on aggregate roots
- ✅ **DO**: Always work with aggregate roots - never save child entities directly
- ❌ **DON'T**: Never use `IEntityReadRepository<T>` or `IGenericReadRepository<T>` for write operations
- ❌ **DON'T**: Never inject `DbContext` or use `DbSet<T>.Add()` directly in Application layer
- ❌ **DON'T**: Never call `SaveChanges()` directly

### 2. **Respect Aggregate Boundaries**
- ✅ **DO**: Always modify aggregates through their root entity
- ✅ **DO**: Save entire aggregate graphs atomically
- ❌ **DON'T**: Never save child entities in isolation
- ❌ **DON'T**: Never bypass aggregate root when modifying children

### 3. **Let PersistenceToolkit Handle State Management**
- ✅ **DO**: Let `EntityStateProcessor` automatically determine Added/Modified state
- ✅ **DO**: Rely on snapshot-based change detection (`HasChange()`)
- ❌ **DON'T**: Never manually set entity state (Added, Modified, Deleted)
- ❌ **DON'T**: Never manually attach/detach entities

---

## 📚 Repository Usage for Write Operations

### IAggregateRepository<T> - The Only Way to Write

```csharp
// ✅ Good: Using IAggregateRepository for write operations
public class OrderService
{
    private readonly IAggregateRepository<Order> _orderRepository;
    
    public OrderService(IAggregateRepository<Order> orderRepository)
    {
        _orderRepository = orderRepository;
    }
    
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order
        {
            CustomerId = request.CustomerId,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        };
        
        await _orderRepository.Save(order);
        return order;
    }
}
```

---

## ✍️ Create Operations

### Creating a New Aggregate

```csharp
// ✅ Good: Create new aggregate with children
public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
{
    var order = new Order
    {
        CustomerId = dto.CustomerId,
        OrderNumber = GenerateOrderNumber(),
        Items = dto.Items.Select(i => new OrderItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList(),
        ShippingAddress = new Address
        {
            Street = dto.ShippingAddress.Street,
            City = dto.ShippingAddress.City,
            ZipCode = dto.ShippingAddress.ZipCode
        }
    };
    
    await _orderRepository.Save(order);
    // ✅ Entire aggregate (Order + OrderItems + Address) saved atomically
    // ✅ CreatedBy, CreatedOn, TenantId set automatically
    // ✅ All entities detached after save
    
    return order;
}
```

### Creating Multiple Aggregates

```csharp
// ✅ Good: Batch create multiple aggregates
public async Task<bool> CreateOrdersAsync(List<CreateOrderDto> orders)
{
    var aggregates = orders.Select(dto => new Order
    {
        CustomerId = dto.CustomerId,
        Items = dto.Items.Select(i => new OrderItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity
        }).ToList()
    }).ToList();
    
    return await _orderRepository.SaveRange(aggregates);
    // ✅ All aggregates saved in single transaction
    // ✅ Audit fields set for all entities
}
```

### Anti-Pattern: Creating Child Entities Directly

```csharp
// ❌ BAD: Trying to save child entity directly
public class OrderItemService
{
    private readonly IAggregateRepository<OrderItem> _itemRepository; // ❌ OrderItem is not an aggregate root
    
    public async Task CreateItemAsync(int orderId, OrderItemDto dto)
    {
        var item = new OrderItem
        {
            OrderId = orderId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity
        };
        
        await _itemRepository.Save(item); // ❌ This won't compile - OrderItem doesn't implement IAggregateRoot
    }
}

// ✅ GOOD: Create child through aggregate root
public class OrderService
{
    private readonly IAggregateRepository<Order> _orderRepository;
    
    public async Task AddItemToOrderAsync(int orderId, OrderItemDto dto)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null) throw new NotFoundException();
        
        order.AddItem(new OrderItem
        {
            ProductId = dto.ProductId,
            Quantity = dto.Quantity
        });
        
        await _orderRepository.Save(order); // ✅ Save through aggregate root
    }
}
```

---

## 🔄 Update Operations

### Updating an Existing Aggregate

```csharp
// ✅ Good: Load, modify, save pattern
public async Task<Order> UpdateOrderAsync(int orderId, UpdateOrderDto dto)
{
    // 1. Load the aggregate
    var order = await _orderRepository.GetByIdAsync(orderId);
    if (order == null) throw new NotFoundException();
    
    // 2. Modify through domain methods (preferred) or properties
    order.UpdateStatus(dto.Status);
    order.UpdateShippingAddress(dto.ShippingAddress);
    
    // 3. Save - PersistenceToolkit handles state detection
    await _orderRepository.Save(order);
    // ✅ Only changed entities marked as Modified (via snapshot comparison)
    // ✅ UpdatedBy and UpdatedOn set automatically
    // ✅ All tracked entities detached after save
    
    return order;
}
```

### Modifying Child Entities in Aggregate

```csharp
// ✅ Good: Modify children through aggregate root
public async Task UpdateOrderItemAsync(int orderId, int itemId, UpdateItemDto dto)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    if (order == null) throw new NotFoundException();
    
    var item = order.Items.FirstOrDefault(i => i.Id == itemId);
    if (item == null) throw new NotFoundException();
    
    // Modify through aggregate root
    order.UpdateItem(itemId, dto.Quantity, dto.UnitPrice);
    
    await _orderRepository.Save(order);
    // ✅ Only the modified OrderItem is marked as Modified
    // ✅ Order itself is also marked Modified if it has changes
}
```

### Adding Children to Existing Aggregate

```csharp
// ✅ Good: Add children to existing aggregate
public async Task AddItemToOrderAsync(int orderId, AddItemDto dto)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    if (order == null) throw new NotFoundException();
    
    order.AddItem(new OrderItem
    {
        ProductId = dto.ProductId,
        Quantity = dto.Quantity,
        UnitPrice = dto.UnitPrice
    });
    
    await _orderRepository.Save(order);
    // ✅ New OrderItem automatically marked as Added
    // ✅ Order marked as Modified
}
```

### Removing Children from Aggregate

```csharp
// ✅ Good: Remove children through aggregate root
public async Task RemoveItemFromOrderAsync(int orderId, int itemId)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    if (order == null) throw new NotFoundException();
    
    order.RemoveItem(itemId);
    
    await _orderRepository.Save(order);
    // ✅ Removed OrderItem automatically marked as Deleted
    // ✅ Order marked as Modified
}
```

### Anti-Pattern: Manual State Management

```csharp
// ❌ BAD: Manually setting entity state
public async Task UpdateOrderAsync(int orderId, UpdateOrderDto dto)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    order.Status = dto.Status;
    
    // ❌ Don't do this - PersistenceToolkit handles it
    _context.Entry(order).State = EntityState.Modified;
    await _context.SaveChangesAsync();
}

// ✅ GOOD: Let PersistenceToolkit handle state
public async Task UpdateOrderAsync(int orderId, UpdateOrderDto dto)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    order.Status = dto.Status;
    
    await _orderRepository.Save(order); // ✅ State handled automatically
}
```

---

## 🗑️ Delete Operations

### Soft Delete (Recommended)

```csharp
// ✅ Good: Soft delete through aggregate root
public async Task<bool> DeleteOrderAsync(int orderId)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    if (order == null) return false;
    
    // Use domain method if available
    order.MarkAsDeleted(_systemUser.UserId, DateTime.UtcNow);
    
    await _orderRepository.Save(order);
    // ✅ IsDeleted, DeletedBy, DeletedOn set automatically
    // ✅ Entity remains in database but filtered from queries
}
```

### Hard Delete (Use with Caution)

```csharp
// ✅ Good: Hard delete when necessary
public async Task<bool> HardDeleteOrderAsync(int orderId)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    if (order == null) return false;
    
    return await _orderRepository.DeleteAsync(order);
    // ✅ Entity permanently removed from database
    // ⚠️ Use only when soft delete is not appropriate
}
```

### Delete Multiple Aggregates

```csharp
// ✅ Good: Batch delete
public async Task<bool> DeleteOrdersAsync(List<int> orderIds)
{
    var orders = new List<Order>();
    foreach (var id in orderIds)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order != null)
        {
            order.MarkAsDeleted(_systemUser.UserId, DateTime.UtcNow);
            orders.Add(order);
        }
    }
    
    return await _orderRepository.SaveRange(orders);
    // ✅ All orders soft-deleted in single transaction
}
```

### Delete by Specification

```csharp
// ✅ Good: Delete using specification
public async Task<bool> DeleteOldOrdersAsync(DateTime cutoffDate)
{
    var spec = new EntitySpecification<Order>()
        .Where(o => o.CreatedOn < cutoffDate)
        .Where(o => o.Status == OrderStatus.Cancelled);
    
    return await _orderRepository.DeleteRangeAsync(spec);
    // ✅ All matching orders deleted
    // ⚠️ Use with extreme caution - verify specification is correct
}
```

---

## 🎨 Best Practices

### 1. **Use Domain Methods for Modifications**

```csharp
// ✅ Good: Use domain methods
public class Order : Entity, IAggregateRoot
{
    public void AddItem(OrderItem item)
    {
        // Domain logic here
        ValidateItem(item);
        Items.Add(item);
        RecalculateTotal();
    }
    
    public void UpdateStatus(OrderStatus status)
    {
        // Domain validation
        if (!CanTransitionTo(status))
            throw new InvalidOperationException();
        
        Status = status;
        StatusChangedOn = DateTime.UtcNow;
    }
}

// Usage
order.AddItem(newItem); // ✅ Domain method
order.UpdateStatus(OrderStatus.Confirmed); // ✅ Domain method
await _orderRepository.Save(order);
```

### 2. **Always Load Before Modifying**

```csharp
// ✅ Good: Load, modify, save
public async Task UpdateOrderAsync(int orderId, UpdateOrderDto dto)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    if (order == null) throw new NotFoundException();
    
    // Modify
    order.UpdateStatus(dto.Status);
    
    // Save
    await _orderRepository.Save(order);
}

// ❌ BAD: Creating new instance instead of loading
public async Task UpdateOrderAsync(int orderId, UpdateOrderDto dto)
{
    var order = new Order { Id = orderId, Status = dto.Status }; // ❌ Missing other properties
    await _orderRepository.Save(order); // ❌ Will overwrite with incomplete data
}
```

### 3. **Handle Concurrency**

```csharp
// ✅ Good: Handle concurrency exceptions
public async Task<Order> UpdateOrderAsync(int orderId, UpdateOrderDto dto)
{
    try
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null) throw new NotFoundException();
        
        order.UpdateStatus(dto.Status);
        await _orderRepository.Save(order);
        
        return order;
    }
    catch (DbUpdateConcurrencyException)
    {
        // Handle concurrency conflict
        throw new ConcurrencyException("Order was modified by another user");
    }
}
```

### 4. **Validate Before Saving**

```csharp
// ✅ Good: Validate in domain or application layer
public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
{
    // Application-level validation
    if (dto.Items == null || !dto.Items.Any())
        throw new ValidationException("Order must have at least one item");
    
    var order = new Order
    {
        CustomerId = dto.CustomerId,
        Items = dto.Items.Select(i => new OrderItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity
        }).ToList()
    };
    
    // Domain validation (if implemented)
    order.Validate(); // Throws if invalid
    
    await _orderRepository.Save(order);
    return order;
}
```

### 5. **Use Transactions for Multiple Operations**

```csharp
// ✅ Good: Use Unit of Work pattern or explicit transactions
public async Task<bool> ProcessOrderAsync(int orderId)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    if (order == null) return false;
    
    // Multiple modifications
    order.Confirm();
    order.AssignShipping();
    order.CalculateShippingCost();
    
    // Single save - all changes in one transaction
    return await _orderRepository.Save(order);
}
```

### 6. **Always Use CancellationTokens**

```csharp
// ✅ Good: Pass cancellation tokens
public async Task<Order> CreateOrderAsync(
    CreateOrderDto dto, 
    CancellationToken cancellationToken = default)
{
    var order = new Order { /* ... */ };
    await _orderRepository.Save(order, cancellationToken);
    return order;
}
```

---

## 🚫 Anti-Patterns to Avoid

### ❌ Saving Child Entities Directly

```csharp
// ❌ BAD: Trying to save child entity
public async Task AddItemAsync(int orderId, OrderItemDto dto)
{
    var item = new OrderItem
    {
        OrderId = orderId,
        ProductId = dto.ProductId,
        Quantity = dto.Quantity
    };
    
    // ❌ This won't work - OrderItem is not an aggregate root
    await _someRepository.Save(item);
}

// ✅ GOOD: Save through aggregate root
public async Task AddItemAsync(int orderId, OrderItemDto dto)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    order.AddItem(new OrderItem { /* ... */ });
    await _orderRepository.Save(order);
}
```

### ❌ Direct DbContext Usage

```csharp
// ❌ BAD: Using DbContext directly
public class OrderService
{
    private readonly DbContext _context;
    
    public async Task CreateOrderAsync(Order order)
    {
        _context.Set<Order>().Add(order);
        await _context.SaveChangesAsync();
    }
}

// ✅ GOOD: Use repository
public class OrderService
{
    private readonly IAggregateRepository<Order> _orderRepository;
    
    public async Task CreateOrderAsync(Order order)
    {
        await _orderRepository.Save(order);
    }
}
```

### ❌ Manual Entity State Management

```csharp
// ❌ BAD: Manually managing entity state
public async Task UpdateOrderAsync(Order order)
{
    _context.Entry(order).State = EntityState.Modified;
    foreach (var item in order.Items)
    {
        if (item.Id == 0)
            _context.Entry(item).State = EntityState.Added;
        else
            _context.Entry(item).State = EntityState.Modified;
    }
    await _context.SaveChangesAsync();
}

// ✅ GOOD: Let PersistenceToolkit handle it
public async Task UpdateOrderAsync(Order order)
{
    await _orderRepository.Save(order);
    // ✅ State automatically determined via snapshot comparison
}
```

### ❌ Not Loading Before Updating

```csharp
// ❌ BAD: Creating new instance for update
public async Task UpdateOrderAsync(int orderId, string newStatus)
{
    var order = new Order { Id = orderId, Status = newStatus };
    await _orderRepository.Save(order);
    // ❌ This will overwrite other properties with default values
}

// ✅ GOOD: Load first, then modify
public async Task UpdateOrderAsync(int orderId, string newStatus)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    order.Status = newStatus;
    await _orderRepository.Save(order);
}
```

### ❌ Bypassing Aggregate Root

```csharp
// ❌ BAD: Modifying child collection directly without going through root
public async Task UpdateOrderItemsAsync(int orderId, List<OrderItem> items)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    order.Items.Clear(); // ❌ Direct manipulation
    order.Items.AddRange(items); // ❌ Bypassing domain logic
    await _orderRepository.Save(order);
}

// ✅ GOOD: Use aggregate root methods
public async Task UpdateOrderItemsAsync(int orderId, List<OrderItemDto> items)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    order.ReplaceItems(items.Select(dto => new OrderItem { /* ... */ })); // ✅ Domain method
    await _orderRepository.Save(order);
}
```

---

## 🔐 Audit and Multi-Tenant Considerations

### Automatic Audit Fields

```csharp
// ✅ Good: Audit fields set automatically
public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
{
    var order = new Order { /* ... */ };
    
    await _orderRepository.Save(order);
    // ✅ CreatedBy, CreatedOn, TenantId set automatically from ISystemUser
    // ✅ No manual setting required
}
```

### Tenant Isolation

```csharp
// ✅ Good: TenantId automatically set for new entities
public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
{
    var order = new Order { /* ... */ };
    
    await _orderRepository.Save(order);
    // ✅ TenantId automatically set from ISystemUser.TenantId
    // ✅ Queries automatically filtered by TenantId
}
```

---

## ⚡ Performance Tips

### 1. **Batch Operations When Possible**

```csharp
// ✅ Good: Batch save multiple aggregates
public async Task<bool> CreateOrdersAsync(List<CreateOrderDto> orders)
{
    var aggregates = orders.Select(dto => new Order { /* ... */ }).ToList();
    return await _orderRepository.SaveRange(aggregates);
    // ✅ More efficient than saving one by one
}
```

### 2. **Load Only What You Need**

```csharp
// ✅ Good: Load with only necessary includes
public async Task UpdateOrderStatusAsync(int orderId, OrderStatus status)
{
    // Only load what's needed - don't include everything
    var order = await _orderRepository.GetByIdAsync(orderId);
    // Don't use: .Include(o => o.Items).Include(o => o.Customer) if not needed
    
    order.UpdateStatus(status);
    await _orderRepository.Save(order);
}
```

### 3. **Use Projections for Read-Then-Write Scenarios**

```csharp
// ✅ Good: Use projection when you only need specific fields
public async Task UpdateOrderStatusAsync(int orderId, OrderStatus status)
{
    // If you only need to check status, use projection
    var spec = new Specification<Order, OrderStatusDto>()
        .Where(o => o.Id == orderId)
        .Select(o => new OrderStatusDto { Id = o.Id, Status = o.Status });
    
    var statusDto = await _orderRepository.FirstOrDefaultAsync(spec);
    if (statusDto == null) throw new NotFoundException();
    
    // Then load full aggregate only if needed
    var order = await _orderRepository.GetByIdAsync(orderId);
    order.UpdateStatus(status);
    await _orderRepository.Save(order);
}
```

---

## 🎯 Change Detection

### How PersistenceToolkit Detects Changes

```csharp
// ✅ Good: PersistenceToolkit uses snapshot-based change detection
public async Task UpdateOrderAsync(int orderId, UpdateOrderDto dto)
{
    // 1. Load entity - snapshot automatically captured
    var order = await _orderRepository.GetByIdAsync(orderId);
    // ✅ LoadTimeSnapshot captured automatically
    
    // 2. Modify entity
    order.Status = dto.Status;
    // ✅ HasChange() will return true when compared to snapshot
    
    // 3. Save - only changed entities are marked Modified
    await _orderRepository.Save(order);
    // ✅ EntityStateProcessor uses HasChange() to determine state
    // ✅ Only entities with changes are included in SaveChanges
}
```

### Manual Change Detection (If Needed)

```csharp
// ✅ Good: Check for changes before saving (optional)
public async Task<bool> UpdateOrderIfChangedAsync(int orderId, UpdateOrderDto dto)
{
    var order = await _orderRepository.GetByIdAsync(orderId);
    if (order == null) return false;
    
    var originalStatus = order.Status;
    order.Status = dto.Status;
    
    // Check if order itself changed
    if (order.HasChange())
    {
        await _orderRepository.Save(order);
        return true;
    }
    
    return false; // No changes, skip save
}
```

---

## 📝 Code Generation Guidelines

When generating code for write operations:

1. **Always use IAggregateRepository** - Never suggest other repository types for writes
2. **Always work with aggregate roots** - Never suggest saving child entities directly
3. **Load before modifying** - Always load entity first, then modify, then save
4. **Use domain methods** - Prefer domain methods over direct property assignment
5. **Handle null results** - Always check for null after GetByIdAsync
6. **Use cancellation tokens** - Include in all async methods
7. **Let PersistenceToolkit handle state** - Never suggest manual state management
8. **Respect aggregate boundaries** - Never bypass aggregate root
9. **Use SaveRange for batches** - When saving multiple aggregates
10. **Consider soft delete** - Prefer soft delete over hard delete when possible

---

## 🎯 Summary Checklist

Before implementing a write operation, ensure:

- [ ] Using `IAggregateRepository<T>` (not read repositories)
- [ ] Working with aggregate roots (not child entities)
- [ ] Loading entity before modifying (not creating new instance)
- [ ] Using domain methods when available (not direct property manipulation)
- [ ] Handling null results from GetByIdAsync
- [ ] Using cancellation tokens
- [ ] Letting PersistenceToolkit handle entity state (not manual state management)
- [ ] Respecting aggregate boundaries (modify through root)
- [ ] Using SaveRange for batch operations
- [ ] Considering soft delete vs hard delete
- [ ] Not accessing DbContext directly
- [ ] Not calling SaveChanges() directly

---

## 🔄 Complete Write Operation Pattern

```csharp
// ✅ Complete pattern for write operations
public class OrderService
{
    private readonly IAggregateRepository<Order> _orderRepository;
    
    public async Task<Order> UpdateOrderAsync(
        int orderId, 
        UpdateOrderDto dto,
        CancellationToken cancellationToken = default)
    {
        // 1. Load aggregate
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw new NotFoundException($"Order {orderId} not found");
        
        // 2. Validate (if needed)
        if (!order.CanBeModified())
            throw new InvalidOperationException("Order cannot be modified");
        
        // 3. Modify through domain methods
        order.UpdateStatus(dto.Status);
        if (dto.Items != null)
        {
            order.ReplaceItems(dto.Items.Select(i => new OrderItem { /* ... */ }));
        }
        
        // 4. Save - PersistenceToolkit handles the rest
        var saved = await _orderRepository.Save(order, cancellationToken);
        if (!saved)
            throw new PersistenceException("Failed to save order");
        
        // 5. Return result
        return order;
    }
}
```

