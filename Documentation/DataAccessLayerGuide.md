# Data Access Layer - Developer Guide

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Creating Entities](#creating-entities)
4. [Composition Patterns](#composition-patterns)
5. [Entity Configuration](#entity-configuration)
6. [Services](#services)
7. [DTOs and Mapping](#dtos-and-mapping)
8. [Specifications](#specifications)
9. [Creating a Vertical Slice](#creating-a-vertical-slice)
10. [Best Practices](#best-practices)

---

## Overview

The Data Access Layer provides a robust, feature-rich framework for building .NET Core APIs using Entity Framework Core. It implements several design patterns including:

- **Repository Pattern**: Abstract data access logic
- **Unit of Work Pattern**: Manage transactions and change persistence
- **Specification Pattern**: Encapsulate query logic
- **Composition Pattern**: Compose services with cross-cutting concerns (auditing, locking, soft delete, concurrency)

### Key Features

- **Entity Behaviors**: Automatic auditing, locking, soft delete, and concurrency control
- **Cascading Operations**: Lock and delete child entities with parent entities
- **Transaction Management**: Built-in support for database transactions
- **Type-Safe Queries**: Specification pattern for complex queries
- **Auto-Mapping**: AutoMapper integration for entity-DTO mapping

---

## Architecture

### Core Components

```
┌─────────────────────────────────────────────────────────┐
│                    Application Layer                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐               │
│  │ Customer │  │  Order   │  │  Product │               │
│  │  Service │  │  Service │  │  Service │               │
│  └──────────┘  └──────────┘  └──────────┘               │
│       │              │              │                   │
│       └──────────────┼──────────────┘                   │
│                      │                                  │
└──────────────────────┼──────────────────────────────────┘
                       │
┌──────────────────────┼──────────────────────────────────┐
│                    Infrastructure Layer                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │ CoreEntity   │  │ EntityLock   │  │ EntityAudit  │   │
│  │   Service    │  │   Service    │  │   Service    │   │
│  └──────────────┘  └──────────────┘  └──────────────┘   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │ EntitySoft   │  │ Concurrency  │  │ Collection   │   │
│  │   Delete     │  │   Service    │  │   Sync       │   │
│  └──────────────┘  └──────────────┘  └──────────────┘   │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐                     │
│  │  Repository  │  │ Unit of Work │                     │
│  └──────────────┘  └──────────────┘                     │
└─────────────────────────────────────────────────────────┘
                       │
┌──────────────────────┼──────────────────────────────────┐
│                    Entity Framework Core                │
│                    (Database Provider)                  │
└─────────────────────────────────────────────────────────┘
```

### Service Composition

Services are composed using dependency injection. The `CoreEntityService` orchestrates other services:

- **EntityAuditService**: Tracks create/update user and timestamps
- **EntityLockService**: Manages entity locking for concurrency
- **EntitySoftDeleteService**: Handles soft deletion
- **ConcurrencyService**: Manages optimistic concurrency control
- **CollectionSyncService**: Syncs child entity collections

---

## Creating Entities

### Basic Entity Structure

An entity must implement `IEntity` and define a `Guid Id` property:

```csharp
using Infrastructure.Entity.Interfaces;

namespace Application.Customer.Entities;

public class Customer : IEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

### Adding Behavior Interfaces

Entities can implement behavior interfaces to add cross-cutting concerns:

#### 1. Auditing (`IAuditableEntity`)

Tracks who created/updated the entity and when:

```csharp
using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;

public class Customer : IEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    // ... other properties
    
    public AuditingBehavior Auditing { get; private set; } = new();
}
```

**What it provides:**
- `Auditing.AuditInfo.CreatedByUserId`: User who created the entity
- `Auditing.AuditInfo.CreatedAtUtc`: Creation timestamp
- `Auditing.AuditInfo.ModifiedByUserId`: User who last modified the entity
- `Auditing.AuditInfo.ModifiedAtUtc`: Last modification timestamp

**Automatic behavior:**
- `StampForCreate(userId)`: Called automatically on entity creation
- `StampForUpdate(userId)`: Called automatically on entity updates

#### 2. Locking (`ILockableEntity<TEntity>`)

Prevents concurrent modifications:

```csharp
using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;

public class Customer : IEntity, ILockableEntity<Customer>
{
    public Guid Id { get; set; }
    // ... other properties
    
    public LockingBehavior<Customer> Locking { get; private set; } = new();
}
```

**What it provides:**
- `Locking.LockInfo.LockedByUserId`: User who locked the entity
- `Locking.LockInfo.LockedAtUtc`: When the lock was acquired
- `Locking.LockInfo.LockTimeoutMinutes`: Lock timeout duration

**Usage:**
```csharp
// Lock an entity
entityLockService.LockIfSupported(entity, userId);

// Validate lock before update
entityLockService.ValidateLockForUpdate(entity, userId);

// Unlock after update
entityLockService.UnlockIfSupported(entity, userId);
```

#### 3. Soft Delete (`ISoftDeletableEntity`)

Marks entities as deleted without removing them from the database:

```csharp
using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;

public class Customer : IEntity, ISoftDeletableEntity
{
    public Guid Id { get; set; }
    // ... other properties
    
    public SoftDeleteBehavior Deleted { get; private set; } = new();
}
```

**What it provides:**
- `Deleted.SoftDeleteInfo.IsDeleted`: Whether the entity is soft deleted
- `Deleted.SoftDeleteInfo.DeletedByUserId`: User who deleted the entity
- `Deleted.SoftDeleteInfo.DeletedAtUtc`: Deletion timestamp

**Automatic behavior:**
- Soft-deleted entities are automatically filtered out of queries
- `DeleteAsync()` performs soft delete by default
- `RestoreAsync()` restores soft-deleted entities

#### 4. Concurrency (`IConcurrencyEntity`)

Implements optimistic concurrency control using row versioning:

```csharp
using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;

public class Customer : IEntity, IConcurrencyEntity
{
    public Guid Id { get; set; }
    // ... other properties
    
    public ConcurrencyBehavior Concurrency { get; private set; } = new();
}
```

**What it provides:**
- `Concurrency.RowVersion`: Byte array used for concurrency checking

**Automatic behavior:**
- Row version is automatically updated on each save
- Concurrency conflicts are detected and handled automatically
- Throws `ConcurrencyException` when conflicts occur

### Complete Entity Example

Here's a complete entity with all behaviors:

```csharp
using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;

namespace Application.Customer.Entities;

public class Customer : 
    IEntity, 
    ILockableEntity<Customer>, 
    IAuditableEntity,
    IConcurrencyEntity,
    ISoftDeletableEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    
    // Behavior properties
    public LockingBehavior<Customer> Locking { get; private set; } = new();
    public AuditingBehavior Auditing { get; private set; } = new();
    public ConcurrencyBehavior Concurrency { get; private set; } = new();
    public SoftDeleteBehavior Deleted { get; private set; } = new();
    
    // Navigation properties
    public ICollection<Order.Entities.Order> Orders { get; set; } = new List<Order.Entities.Order>();
}
```

**Important Notes:**
- Behavior properties should be `private set` to prevent direct modification
- Initialize behavior properties with `new()` to ensure they're never null
- Behavior properties are automatically configured by `BaseAppDbContext`

---

## Composition Patterns

### Cascade Locking

Use the `[CascadeLock]` attribute to lock child entities when locking a parent:

```csharp
using Infrastructure.Entity.Attributes;

public class Order : IEntity, ILockableEntity<Order>
{
    public Guid Id { get; set; }
    
    [CascadeLock]
    public ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();
}
```

**Behavior:**
- When `Order` is locked, all `OrderProducts` are also locked
- Lock depth is controlled by `LockOptions.MaxDepth`
- Default depth is 1 (direct children only)

**Usage:**
```csharp
var lockOptions = new LockOptions 
{ 
    IncludeChildren = true, 
    MaxDepth = 1 
};

await coreEntityService.UpdateAsync(
    orderId, 
    order => { /* update logic */ },
    lockOptions,
    cancellationToken);
```

### Cascade Soft Delete

Use the `[CascadeDelete]` attribute to soft delete child entities when soft deleting a parent:

```csharp
using Infrastructure.Entity.Attributes;

public class Order : IEntity, ISoftDeletableEntity
{
    public Guid Id { get; set; }
    
    [CascadeDelete]
    public ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();
}
```

**Behavior:**
- When `Order` is soft deleted, all `OrderProducts` are also soft deleted
- Restoring an `Order` also restores its `OrderProducts`

**Usage:**
```csharp
// Soft delete with children
await coreEntityService.DeleteAsync(orderId, cancellationToken);

// Restore with children
await coreEntityService.RestoreAsync(orderId, includeChildren: true, cancellationToken);
```

---

## Entity Configuration

### EF Core Configuration Files

Create configuration classes that implement `IEntityTypeConfiguration<T>`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Customer.Entities.Configuration;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        // Property configuration
        builder
            .Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(64);
        
        builder
            .Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(64);

        builder
            .Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(128);

        // Indexes
        builder
            .HasIndex(x => x.Email)
            .IsUnique();

        builder
            .HasIndex(x => x.LastName);
    }
}
```

### Relationship Configuration

Configure relationships between entities:

```csharp
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // One-to-Many relationship
        builder
            .HasOne(x => x.Customer)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // One-to-Many with cascade delete
        builder
            .HasMany(o => o.OrderProducts)
            .WithOne(op => op.Order)
            .HasForeignKey(op => op.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder
            .HasIndex(o => o.CustomerId);
        
        builder
            .HasIndex(o => o.Date);
    }
}
```

### Configuration Discovery

Configuration files are automatically discovered by `BaseAppDbContext`:

```csharp
public class AppDbContext : BaseAppDbContext
{
    public AppDbContext(DbContextOptions<BaseAppDbContext> options) 
        : base(options, typeof(AppDbContext).Assembly)
    {
    }
    
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
}
```

The `BaseAppDbContext` automatically:
1. Applies configurations from the specified assembly
2. Configures entity behaviors (auditing, locking, soft delete, concurrency)
3. Applies soft delete query filters

---

## Services

### CoreEntityService

The `CoreEntityService<TEntity>` provides comprehensive CRUD operations:

```csharp
public class CustomerService(
    ICoreEntityService<Entities.Customer> coreEntityService,
    IMapper mapper) : ICustomerService
{
    // Get all entities
    public async Task<IEnumerable<CustomerDto>> GetAllAsync(
        CancellationToken cancellationToken)
        => mapper.Map<IEnumerable<CustomerDto>>(
            await coreEntityService.GetAllAsync(cancellationToken));

    // Get by ID
    public async Task<CustomerDto> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
        => mapper.Map<CustomerDto>(
            await coreEntityService.GetByIdAsync(id, cancellationToken));
    
    // Get by ID with lock
    public async Task<CustomerDto> GetByIdWithLockAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
        => mapper.Map<CustomerDto>(
            await coreEntityService.GetByIdWithLockAsync(id, cancellationToken));

    // Create entity
    public async Task<CustomerDto> CreateAsync(
        CustomerDto customer, 
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        
        return mapper.Map<CustomerDto>(
            await coreEntityService.CreateAsync(
                mapper.Map<Entities.Customer>(customer), 
                cancellationToken));
    }

    // Update entity
    public async Task<CustomerDto> UpdateAsync(
        CustomerDto customer, 
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        
        if (customer.Id == Guid.Empty)
        {
            throw new ArgumentException("Customer ID cannot be empty.", nameof(customer));
        }
        
        var updated = await coreEntityService.UpdateAsync(
            customer.Id,
            existing => mapper.Map(customer, existing),
            cancellationToken);
    
        return mapper.Map<CustomerDto>(updated);
    }

    // Delete entity (soft delete if ISoftDeletableEntity)
    public async Task DeleteAsync(
        Guid id, 
        CancellationToken cancellationToken)
        => await coreEntityService.DeleteAsync(id, cancellationToken);
}
```

### Service Methods

#### Query Methods

- `GetAllAsync(cancellationToken)`: Get all entities
- `GetByIdAsync(id, cancellationToken)`: Get entity by ID
- `GetByIdOrThrowAsync(id, cancellationToken)`: Get entity by ID or throw `EntityNotFoundException`
- `GetByIdWithLockAsync(id, lockOptions?, cancellationToken)`: Get entity by ID and lock it
- `FindAsync(specification, queryOptions?, cancellationToken)`: Find entities using specification
- `FindPagedAsync(specification, queryOptions?, cancellationToken)`: Find entities with pagination
- `FindOneAsync(specification, queryOptions?, cancellationToken)`: Find single entity using specification

#### Modification Methods

- `CreateAsync(entity, cancellationToken)`: Create new entity
- `UpdateAsync(id, updateAction, lockOptions?, specification?, cancellationToken)`: Update entity
- `DeleteAsync(id, lockOptions?, cancellationToken)`: Delete entity (soft delete if supported)
- `HardDeleteAsync(id, lockOptions?, cancellationToken)`: Hard delete entity
- `RestoreAsync(id, includeChildren, cancellationToken)`: Restore soft-deleted entity

### Collection Sync Service

Use `CollectionSyncService` to sync child entity collections:

```csharp
public class OrderService(
    ICoreEntityService<Entities.Order> coreEntityService,
    ICollectionSyncService collectionSyncService,
    IEntitySoftDeleteService<Entities.OrderProduct> orderProductSoftDeleteService,
    IRepository<Entities.OrderProduct> orderProductRepository,
    IMapper mapper) : IOrderService
{
    public async Task<OrderDto> UpdateAsync(
        OrderDto order, 
        CancellationToken cancellationToken)
    {
        var updated = await coreEntityService.UpdateAsync(
            order.Id,
            existingOrder =>
            {
                mapper.Map(order, existingOrder);
                
                // Sync OrderProducts collection
                collectionSyncService.SyncChildCollection(
                    existingChildren: existingOrder.OrderProducts,
                    dtoChildren: order.OrderProducts,
                    getDtoKey: dto => dto.Id,
                    hasChanges: (entity, dto) => 
                        entity.ProductId != dto.ProductId ||
                        entity.Quantity != dto.Quantity ||
                        entity.Price != dto.Price ||
                        !entity.Concurrency.RowVersion.SequenceEqual(dto.RowVersion),
                    updateExisting: (entity, dto) =>
                    {
                        entity.ProductId = dto.ProductId;
                        entity.Quantity = dto.Quantity;
                        entity.Price = dto.Price;
                        entity.Concurrency.RowVersion = dto.RowVersion;
                    },
                    createNew: dto => new Entities.OrderProduct
                    {
                        OrderId = existingOrder.Id,
                        ProductId = dto.ProductId,
                        Quantity = dto.Quantity,
                        Price = dto.Price
                    },
                    childSoftDeleteService: orderProductSoftDeleteService,
                    childRepository: orderProductRepository,
                    userId: currentUserService.UserId);
            },
            lockOptions,
            specification,
            cancellationToken);
        
        return mapper.Map<OrderDto>(updated);
    }
}
```

**CollectionSyncService behavior:**
- Adds new entities from DTO that don't exist in entity collection
- Updates existing entities that have changes
- Soft deletes entities that exist in collection but not in DTO
- Restores entities that were soft deleted but are back in DTO

---

## DTOs and Mapping

### Creating DTOs

DTOs should include all relevant data for the API:

```csharp
namespace Application.Customer.Dtos;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    
    // Locking information
    public bool Locked { get; set; }
    public string? LockedBy { get; set; }
    public Guid? LockedByUserId { get; set; }
    
    // Auditing information
    public string CreatedBy { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
    public Guid ModifiedByUserId { get; set; }
    public DateTime ModifiedAtUtc { get; set; }
    
    // Concurrency information
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
```

### AutoMapper Profiles

Create mapping profiles to map between entities and DTOs:

```csharp
using Application.Customer.Dtos;
using AutoMapper;
using Infrastructure.Services.Interfaces;

namespace Application.Customer.Mapping;

public class CustomerMappingProfile : Profile
{
    public CustomerMappingProfile(ICurrentUserService currentUserService)
    {
        // Entity -> DTO
        CreateMap<Entities.Customer, CustomerDto>()
            .ForMember(
                dest => dest.Locked,
                opt => opt.MapFrom(src => src.Locking.LockInfo.LockedByUserId != null))
            .ForMember(
                dest => dest.LockedBy,
                opt => opt.MapFrom(src =>
                    src.Locking.LockInfo.LockedByUserId.HasValue
                        ? currentUserService.GetUserName(src.Locking.LockInfo.LockedByUserId.Value)
                        : null))
            .ForMember(
                dest => dest.LockedByUserId, 
                opt => opt.MapFrom(src => src.Locking.LockInfo.LockedByUserId))
            
            .ForMember(
                dest => dest.CreatedAtUtc, 
                opt => opt.MapFrom(src => src.Auditing.AuditInfo.CreatedAtUtc))
            .ForMember(
                dest => dest.CreatedBy, 
                opt => opt.MapFrom(src => 
                    currentUserService.GetUserName(src.Auditing.AuditInfo.CreatedByUserId)))
            .ForMember(
                dest => dest.CreatedByUserId, 
                opt => opt.MapFrom(src => src.Auditing.AuditInfo.CreatedByUserId))
            
            .ForMember(
                dest => dest.ModifiedAtUtc, 
                opt => opt.MapFrom(src => src.Auditing.AuditInfo.ModifiedAtUtc))
            .ForMember(
                dest => dest.ModifiedBy, 
                opt => opt.MapFrom(src => 
                    currentUserService.GetUserName(src.Auditing.AuditInfo.ModifiedByUserId)))
            .ForMember(
                dest => dest.ModifiedByUserId, 
                opt => opt.MapFrom(src => src.Auditing.AuditInfo.ModifiedByUserId))
            
            .ForMember(
                dest => dest.RowVersion, 
                opt => opt.MapFrom(src => src.Concurrency.RowVersion));

        // DTO -> Entity
        CreateMap<CustomerDto, Entities.Customer>()
            .ForMember(dest => dest.Auditing, opt => opt.Ignore())
            .ForMember(dest => dest.Locking, opt => opt.Ignore())
            .ForMember(
                dest => dest.Concurrency.RowVersion, 
                opt => opt.MapFrom(src => src.RowVersion));
    }
}
```

**Important mapping rules:**
- Always ignore behavior properties (`Auditing`, `Locking`, `Deleted`) when mapping DTO -> Entity
- Map `RowVersion` for concurrency control
- Use `ICurrentUserService` to resolve user names from IDs
- Map nested navigation properties explicitly

---

## Specifications

### Creating Specifications

Use the Specification pattern to encapsulate query logic:

```csharp
using Infrastructure.Repository.Specification;

namespace Application.Customer.Specifications;

public class FindCustomerSpecification : BaseSpecification<Entities.Customer>
{
    public FindCustomerSpecification(
        string? lastName = null, 
        string? emailAddress = null, 
        string? phoneNumber = null,
        int page = 1,
        int pageSize = 10)
    {
        // Add criteria
        if (lastName != null)
            AddCriteria(x => x.LastName.StartsWith(lastName));
        
        if (emailAddress != null)
            AddCriteria(x => x.Email.StartsWith(emailAddress));
        
        if (phoneNumber != null)
            AddCriteria(x => x.Phone.StartsWith(phoneNumber));
        
        // Add includes
        AddInclude(x => x.Orders);
        
        // Add ordering
        ApplyOrderBy(x => x.LastName);
        
        // Add pagination
        ApplyPaging((page - 1) * pageSize, pageSize);
    }
}
```

### Specification Methods

- `AddCriteria(expression)`: Add WHERE clause
- `AddInclude(expression)`: Include related entities
- `AddIncludeString(path)`: Include using string path
- `ApplyOrderBy(expression)`: Order by ascending
- `ApplyOrderByDescending(expression)`: Order by descending
- `ApplyPaging(skip, take)`: Add pagination
- `ApplyTracking()`: Enable change tracking

### Using Specifications

```csharp
public async Task<IEnumerable<CustomerDto>> FindCustomersAsync(
    string? lastName = null,
    string? emailAddress = null,
    string? phoneNumber = null,
    int page = 1,
    int pageSize = 10,
    CancellationToken cancellationToken = default)
{
    var specification = new FindCustomerSpecification(
        lastName, 
        emailAddress, 
        phoneNumber, 
        page, 
        pageSize);
    
    return mapper.Map<IEnumerable<CustomerDto>>(
        await coreEntityService.FindAsync(
            specification, 
            QueryOptions.Default, 
            cancellationToken));
}
```

### Composite Specifications

Combine specifications using AND/OR logic:

```csharp
var activeCustomers = new ActiveCustomerSpecification();
var premiumCustomers = new PremiumCustomerSpecification();

var activeAndPremium = activeCustomers.And(premiumCustomers);
var activeOrPremium = activeCustomers.Or(premiumCustomers);
```

---

## Creating a Vertical Slice

A vertical slice is a complete feature implementation. Here's how to create one:

### Step 1: Create Entity

```csharp
// Application/Product/Entities/Product.cs
using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;

namespace Application.Product.Entities;

public class Product : 
    IEntity, 
    ILockableEntity<Product>, 
    IAuditableEntity,
    IConcurrencyEntity,
    ISoftDeletableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    public LockingBehavior<Product> Locking { get; private set; } = new();
    public AuditingBehavior Auditing { get; private set; } = new();
    public ConcurrencyBehavior Concurrency { get; private set; } = new();
    public SoftDeleteBehavior Deleted { get; private set; } = new();
}
```

### Step 2: Create Entity Configuration

```csharp
// Application/Product/Entities/Configuration/ProductConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Product.Entities.Configuration;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder
            .Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(128);
        
        builder
            .Property(x => x.Description)
            .HasMaxLength(512);
        
        builder
            .Property(x => x.Price)
            .HasPrecision(18, 2);
        
        builder
            .HasIndex(x => x.Name);
    }
}
```

### Step 3: Create DTO

```csharp
// Application/Product/Dtos/ProductDto.cs
namespace Application.Product.Dtos;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    public bool Locked { get; set; }
    public string? LockedBy { get; set; }
    public Guid? LockedByUserId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
    public Guid ModifiedByUserId { get; set; }
    public DateTime ModifiedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
```

### Step 4: Create Mapping Profile

```csharp
// Application/Product/Mapping/ProductMappingProfile.cs
using Application.Product.Dtos;
using AutoMapper;
using Infrastructure.Services.Interfaces;

namespace Application.Product.Mapping;

public class ProductMappingProfile : Profile
{
    public ProductMappingProfile(ICurrentUserService currentUserService)
    {
        CreateMap<Entities.Product, ProductDto>()
            .ForMember(
                dest => dest.Locked,
                opt => opt.MapFrom(src => src.Locking.LockInfo.LockedByUserId != null))
            .ForMember(
                dest => dest.LockedBy,
                opt => opt.MapFrom(src =>
                    src.Locking.LockInfo.LockedByUserId.HasValue
                        ? currentUserService.GetUserName(src.Locking.LockInfo.LockedByUserId.Value)
                        : null))
            .ForMember(
                dest => dest.LockedByUserId, 
                opt => opt.MapFrom(src => src.Locking.LockInfo.LockedByUserId))
            .ForMember(
                dest => dest.CreatedAtUtc, 
                opt => opt.MapFrom(src => src.Auditing.AuditInfo.CreatedAtUtc))
            .ForMember(
                dest => dest.CreatedBy, 
                opt => opt.MapFrom(src => 
                    currentUserService.GetUserName(src.Auditing.AuditInfo.CreatedByUserId)))
            .ForMember(
                dest => dest.CreatedByUserId, 
                opt => opt.MapFrom(src => src.Auditing.AuditInfo.CreatedByUserId))
            .ForMember(
                dest => dest.ModifiedAtUtc, 
                opt => opt.MapFrom(src => src.Auditing.AuditInfo.ModifiedAtUtc))
            .ForMember(
                dest => dest.ModifiedBy, 
                opt => opt.MapFrom(src => 
                    currentUserService.GetUserName(src.Auditing.AuditInfo.ModifiedByUserId)))
            .ForMember(
                dest => dest.ModifiedByUserId, 
                opt => opt.MapFrom(src => src.Auditing.AuditInfo.ModifiedByUserId))
            .ForMember(
                dest => dest.RowVersion, 
                opt => opt.MapFrom(src => src.Concurrency.RowVersion));

        CreateMap<ProductDto, Entities.Product>()
            .ForMember(dest => dest.Auditing, opt => opt.Ignore())
            .ForMember(dest => dest.Locking, opt => opt.Ignore())
            .ForMember(
                dest => dest.Concurrency.RowVersion, 
                opt => opt.MapFrom(src => src.RowVersion));
    }
}
```

### Step 5: Create Service Interface

```csharp
// Application/Product/Interfaces/IProductService.cs
using Application.Product.Dtos;

namespace Application.Product.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDto> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(ProductDto product, CancellationToken cancellationToken);
    Task<ProductDto> UpdateAsync(ProductDto product, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
```

### Step 6: Create Service Implementation

```csharp
// Application/Product/Services/ProductService.cs
using Application.Product.Dtos;
using Application.Product.Interfaces;
using AutoMapper;
using Infrastructure.Repository.Models;
using Infrastructure.Services.Interfaces;

namespace Application.Product.Services;

public class ProductService(
    ICoreEntityService<Entities.Product> coreEntityService,
    IMapper mapper) : IProductService
{
    public async Task<IEnumerable<ProductDto>> GetAllAsync(
        CancellationToken cancellationToken)
        => mapper.Map<IEnumerable<ProductDto>>(
            await coreEntityService.GetAllAsync(cancellationToken));

    public async Task<ProductDto> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
        => mapper.Map<ProductDto>(
            await coreEntityService.GetByIdAsync(id, cancellationToken));
    
    public async Task<ProductDto> GetByIdWithLockAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
        => mapper.Map<ProductDto>(
            await coreEntityService.GetByIdWithLockAsync(id, cancellationToken));

    public async Task<ProductDto> CreateAsync(
        ProductDto product, 
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product, nameof(product));
        
        return mapper.Map<ProductDto>(
            await coreEntityService.CreateAsync(
                mapper.Map<Entities.Product>(product), 
                cancellationToken));
    }

    public async Task<ProductDto> UpdateAsync(
        ProductDto product, 
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product, nameof(product));
        
        if (product.Id == Guid.Empty)
        {
            throw new ArgumentException("Product ID cannot be empty.", nameof(product));
        }
        
        var updated = await coreEntityService.UpdateAsync(
            product.Id,
            existing => mapper.Map(product, existing),
            cancellationToken);
    
        return mapper.Map<ProductDto>(updated);
    }

    public async Task DeleteAsync(
        Guid id, 
        CancellationToken cancellationToken)
        => await coreEntityService.DeleteAsync(id, cancellationToken);
}
```

### Step 7: Register Service

```csharp
// Application/ServiceComposition.cs
public static class ServiceComposition
{
    public static void RegisterApplication(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IProductService, ProductService>(); // Add this
    }
}
```

### Step 8: Add DbSet to AppDbContext

```csharp
// Application/AppDbContext.cs
public class AppDbContext : BaseAppDbContext
{
    public AppDbContext(DbContextOptions<BaseAppDbContext> options) 
        : base(options, typeof(AppDbContext).Assembly)
    {
    }
    
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; } // Add this
}
```

---

## Best Practices

### Entity Design

1. **Always use `Guid` for IDs**: Provides better distributed system support
2. **Initialize behavior properties**: Use `= new()` to ensure they're never null
3. **Use `private set` for behavior properties**: Prevents direct modification
4. **Implement interfaces explicitly**: Makes behavior composition clear

### Service Design

1. **Use DTOs for API boundaries**: Never expose entities directly
2. **Validate input**: Always validate DTOs before mapping to entities
3. **Handle exceptions**: Let infrastructure exceptions bubble up, but wrap with meaningful messages
4. **Use cancellation tokens**: Always accept `CancellationToken` parameters

### Mapping

1. **Ignore behavior properties**: Never map behavior properties from DTO to Entity
2. **Map RowVersion**: Always map `RowVersion` for concurrency control
3. **Resolve user names**: Use `ICurrentUserService` to resolve user names from IDs
4. **Handle nulls**: Use null-conditional operators when mapping nested properties

### Specifications

1. **Single responsibility**: Each specification should have a single purpose
2. **Composability**: Design specifications to be composable
3. **Documentation**: Add XML comments to complex specifications
4. **Pagination**: Always include pagination for list queries

### Performance

1. **Use AsNoTracking**: Use `QueryOptions.Default` for read-only queries
2. **Selective includes**: Only include related entities when needed
3. **Pagination**: Always paginate large result sets
4. **Indexes**: Create indexes for frequently queried properties

---

## Summary

This guide covers the complete Data Access Layer implementation. The key concepts are:

- **Entities**: Implement behavior interfaces to add cross-cutting concerns
- **Configuration**: Use EF Core configuration classes for entity setup
- **Services**: Use `CoreEntityService` for CRUD operations
- **DTOs**: Use AutoMapper to map between entities and DTOs
- **Specifications**: Encapsulate query logic in reusable specifications
- **Composition**: Use cascade attributes for child entity operations

For complete examples, refer to the `Customer` and `Order` implementations in the `Application` project.

