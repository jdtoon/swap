# NetMX Audit Module

**Comprehensive audit logging and compliance tracking for NetMX applications.**

This module provides automatic entity change tracking, action audit logging, compliance reporting, and retention policies.

## Overview

The Audit Module captures:
- **Entity Changes**: Track all CRUD operations
- **Property Changes**: Before/after values for every property
- **Action Logs**: HTTP requests, commands, background jobs
- **User Context**: Who made changes and when
- **Compliance**: Query audit trail for compliance requirements

Perfect for applications requiring audit trails, compliance, and change tracking.

## Features

### 1. Entity Change Tracking

Automatically captures:
- Entity type and ID
- Operation (Created, Updated, Deleted)
- Changed properties (before/after values)
- User who made change
- Timestamp
- IP address and user agent

### 2. Action Audit Logging

Tracks:
- HTTP requests (method, path, query string)
- Commands and use cases
- Background jobs
- Duration and status
- Exceptions and errors

### 3. Compliance Reporting

Provides:
- Who accessed what and when
- Full audit trail for entities
- Change history with user context
- Export to CSV/JSON for auditors

### 4. Retention Policies

Configure:
- Automatic deletion of old audit logs
- Archive to cold storage
- Compliance with data retention laws

## Structure

- **Audit.Core** - Domain entities (AuditLog, AuditEntry, EntityChange, PropertyChange)
- **Audit.Contracts** - DTOs and service interfaces
- **Audit.Application** - Service implementations and event handlers
- **Audit.Web** - Controllers, views, and HTMX UI

## Installation

```bash
# Add module to your project
cd src/YourApp.Web
netmx add module Audit
```

**What it does**:
- Adds project references
- Registers services in `Program.cs` (commented code for review)
- Creates database migrations
- Ready to use!

## Configuration

### Program.cs Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Audit services
builder.Services.AddAudit(options =>
{
    options.EnableEntityTracking = true;
    options.EnableActionLogging = true;
    options.RetentionDays = 90;  // Keep 90 days
});

var app = builder.Build();

// Use Audit middleware
app.UseAudit();

app.Run();
```

### Audit Options

```csharp
public class AuditOptions
{
    public bool EnableEntityTracking { get; set; } = true;
    public bool EnableActionLogging { get; set; } = true;
    public int RetentionDays { get; set; } = 90;
    public bool CaptureRequestBody { get; set; } = false;
    public bool CaptureResponseBody { get; set; } = false;
}
```

## Usage

### Automatic Entity Tracking

No code needed! Just inherit from `AuditedEntity`:

```csharp
public class Product : AuditedEntity<Guid>
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// Changes automatically tracked
var product = new Product { Name = "Widget", Price = 10.00m };
await _repository.InsertAsync(product);
// AuditLog entry created automatically
```

### Query Audit Trail

```csharp
public class AuditService
{
    private readonly IAuditLogRepository _auditRepo;
    
    public async Task<List<AuditLogDto>> GetEntityHistoryAsync(
        string entityType,
        Guid entityId)
    {
        return await _auditRepo.AsQueryable()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
}
```

### Custom Audit Logging

```csharp
public class OrderService
{
    private readonly IAuditLogger _auditLogger;
    
    public async Task SubmitOrderAsync(Guid orderId)
    {
        var order = await _repository.GetAsync(orderId);
        order.Submit();
        
        // Custom audit entry
        await _auditLogger.LogAsync(new AuditLogEntry
        {
            Action = "OrderSubmitted",
            EntityType = "Order",
            EntityId = orderId,
            Message = $"Order {orderId} submitted by {_currentUser.UserName}"
        });
    }
}
```

## UI Features

### Audit Log Viewer

Navigate to `/Audit/AuditLogs` to see:
- List of all audit entries
- Search by entity type, user, date range
- Filter by operation (Create, Update, Delete)
- View detailed change history

### Entity History

View complete history for any entity:
- Who created it and when
- All updates with before/after values
- Who deleted it (if soft deleted)

## Event Integration

The Audit module listens to domain events:

```csharp
// Events automatically trigger audit logging
this.HxTrigger(Events.Product.Created, new { productId });
// Audit module captures this event
```

**Supported Events**:
- `Product.Created/Updated/Deleted`
- `Order.Created/Updated/Deleted`
- `User.Created/Updated/Deleted`
- Custom domain events

## Compliance Features

### GDPR Compliance

```csharp
// Export user's audit trail
var auditData = await _auditService.ExportUserDataAsync(userId);
// Returns JSON with all actions by user

// Delete user's audit data (right to be forgotten)
await _auditService.DeleteUserAuditDataAsync(userId);
```

### SOX Compliance

- Immutable audit logs (cannot be modified after creation)
- Full trail of financial transactions
- User authentication logged
- Administrative actions tracked

## Database Schema

**AuditLogs Table**:
- Id (Guid)
- EntityType (string)
- EntityId (string)
- Operation (enum: Created, Updated, Deleted)
- UserId (Guid)
- UserName (string)
- IpAddress (string)
- CreatedAt (DateTime)

**AuditEntries Table** (Property changes):
- Id (Guid)
- AuditLogId (Guid)
- PropertyName (string)
- OldValue (string)
- NewValue (string)

## Generating Custom Features

```bash
cd modules/Audit/Audit.Web
netmx generate feature ComplianceReport -m Audit
```

## API Endpoints

**GET** `/api/audit/logs` - List audit logs (paginated)  
**GET** `/api/audit/logs/{id}` - Get audit log details  
**GET** `/api/audit/entity/{type}/{id}` - Get entity history  
**GET** `/api/audit/export` - Export audit data (CSV/JSON)

## Testing

```bash
cd modules/Audit
dotnet test
```

## Documentation

- [Event Registry Architecture](../../docs/EVENT-REGISTRY-ARCHITECTURE.md)
- [HTMX Patterns](../../docs/HTMX-PATTERNS.md)
- [Module Development](../../docs/TERMINOLOGY.md#-module)

## License

MIT License - See [LICENSE](../../LICENSE) file for details.