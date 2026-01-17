# Data Model: RBAC-SSO Multi-Tenant E-Commerce POC

**Date**: 2026-01-17
**Feature**: 001-rbac-sso-poc

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              products                                    │
├─────────────────────────────────────────────────────────────────────────┤
│ id              UUID          PK                                        │
│ product_code    VARCHAR(10)   UK    "P" + 6 digits                      │
│ name            VARCHAR(255)  NOT NULL                                  │
│ price           DECIMAL(19,4) NOT NULL  > 0                             │
│ category        VARCHAR(100)  NOT NULL                                  │
│ description     TEXT                                                    │
│ status          VARCHAR(20)   NOT NULL  (ACTIVE, INACTIVE, DELETED)     │
│ tenant_id       VARCHAR(50)   NOT NULL  FK → implicit tenant filter     │
│ created_by      VARCHAR(100)  NOT NULL                                  │
│ created_at      TIMESTAMPTZ   NOT NULL                                  │
│ updated_by      VARCHAR(100)                                            │
│ updated_at      TIMESTAMPTZ                                             │
├─────────────────────────────────────────────────────────────────────────┤
│ INDEXES:                                                                 │
│   pk_products          (id) PRIMARY KEY                                 │
│   uk_products_code     (product_code) UNIQUE                            │
│   idx_products_tenant  (tenant_id)                                      │
│   idx_products_category (category)                                      │
│   idx_products_status  (status)                                         │
└─────────────────────────────────────────────────────────────────────────┘
                                   │
                                   │ generates Domain Events
                                   ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                             audit_logs                                   │
├─────────────────────────────────────────────────────────────────────────┤
│ id               UUID          PK                                       │
│ timestamp        TIMESTAMPTZ   NOT NULL                                 │
│ event_type       VARCHAR(50)   NOT NULL                                 │
│ aggregate_type   VARCHAR(50)   NOT NULL                                 │
│ aggregate_id     VARCHAR(100)  NOT NULL                                 │
│ username         VARCHAR(100)  NOT NULL                                 │
│ service_name     VARCHAR(50)   NOT NULL                                 │
│ action           VARCHAR(50)   NOT NULL  (CREATE, UPDATE, DELETE, etc.) │
│ payload          JSONB         NOT NULL                                 │
│ result           VARCHAR(20)   NOT NULL  (SUCCESS, FAILURE)             │
│ error_message    TEXT                                                   │
│ client_ip        VARCHAR(45)             IPv4/IPv6                      │
│ correlation_id   VARCHAR(100)  NOT NULL                                 │
│ payload_truncated BOOLEAN      DEFAULT FALSE                            │
├─────────────────────────────────────────────────────────────────────────┤
│ INDEXES:                                                                 │
│   pk_audit_logs         (id) PRIMARY KEY                                │
│   idx_audit_timestamp   (timestamp DESC)                                │
│   idx_audit_username    (username, timestamp DESC)                      │
│   idx_audit_aggregate   (aggregate_type, aggregate_id, timestamp DESC)  │
│   idx_audit_event_type  (event_type, timestamp DESC)                    │
│   idx_audit_correlation (correlation_id)                                │
│   idx_audit_result      (result, timestamp DESC)                        │
└─────────────────────────────────────────────────────────────────────────┘
```

## Domain Entities

### Product (Aggregate Root)

**Bounded Context**: Product Management

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Id | ProductId (Guid) | PK, auto-generated | Unique identifier |
| ProductCode | ProductCode | Unique, "P" + 6 digits | Human-readable code |
| Name | string | Required, max 255 chars | Product name |
| Price | Money | Required, > 0 | Product price |
| Category | string | Required, max 100 chars | Product category |
| Description | string? | Optional | Product description |
| Status | ProductStatus | Required | ACTIVE, INACTIVE, DELETED |
| TenantId | string | Required, max 50 chars | Tenant ownership |
| CreatedBy | string | Required | Username who created |
| CreatedAt | DateTimeOffset | Required | Creation timestamp |
| UpdatedBy | string? | Optional | Username who last updated |
| UpdatedAt | DateTimeOffset? | Optional | Last update timestamp |

**Invariants**:
- Price must be positive
- ProductCode must match format "P" + 6 digits
- Status transitions: ACTIVE ↔ INACTIVE, any → DELETED (one-way)
- TenantId immutable after creation

**Domain Events**:
- `ProductCreated`: On creation
- `ProductUpdated`: On any update
- `ProductPriceChanged`: On price change (in addition to ProductUpdated)
- `ProductDeleted`: On soft delete

### Value Objects

#### ProductId
```csharp
public record ProductId(Guid Value)
{
    public static ProductId Create() => new(Guid.NewGuid());
    public static ProductId Create(Guid value) => new(value);
}
```

#### ProductCode
```csharp
public record ProductCode(string Value)
{
    private static readonly Regex Pattern = new(@"^P\d{6}$");

    public static ProductCode Generate()
    {
        var random = new Random();
        return new ProductCode($"P{random.Next(0, 999999):D6}");
    }

    public static ProductCode Create(string value)
    {
        if (!Pattern.IsMatch(value))
            throw new DomainException("PRD-V00001", "Invalid product code format");
        return new ProductCode(value);
    }
}
```

#### Money
```csharp
public record Money(decimal Amount)
{
    public static Money Create(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("PRD-V00002", "Price must be positive");
        return new Money(amount);
    }
}
```

### AuditLog (Entity)

**Bounded Context**: Audit & Compliance

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Id | Guid | PK, auto-generated | Unique identifier |
| Timestamp | DateTimeOffset | Required | Event occurrence time |
| EventType | string | Required | Domain event type name |
| AggregateType | string | Required | Entity type (e.g., "Product") |
| AggregateId | string | Required | Entity identifier |
| Username | string | Required | Actor username |
| ServiceName | string | Required | Originating service |
| Action | string | Required | Operation type |
| Payload | string (JSON) | Required | Event data as JSON |
| Result | AuditResult | Required | SUCCESS or FAILURE |
| ErrorMessage | string? | Optional | Error details if failed |
| ClientIp | string? | Optional | Client IP address |
| CorrelationId | string | Required | Request correlation ID |
| PayloadTruncated | bool | Default false | If payload was truncated |

**Invariants**:
- Payload max size: 10KB (truncate if larger)
- CorrelationId required for traceability

## Enumerations

### ProductStatus
```csharp
public enum ProductStatus
{
    Active,
    Inactive,
    Deleted
}
```

### AuditResult
```csharp
public enum AuditResult
{
    Success,
    Failure
}
```

### Role (Reference Data from Keycloak)
```csharp
public static class Roles
{
    public const string Admin = "ADMIN";
    public const string TenantAdmin = "TENANT_ADMIN";
    public const string User = "USER";
    public const string Viewer = "VIEWER";
}
```

## Permission Matrix

| Endpoint | ADMIN | TENANT_ADMIN | USER | VIEWER |
|----------|-------|--------------|------|--------|
| GET /api/products | ✅ | ✅ (tenant-scoped) | ✅ (tenant-scoped) | ✅ (tenant-scoped) |
| GET /api/products/{id} | ✅ | ✅ (tenant-scoped) | ✅ (tenant-scoped) | ✅ (tenant-scoped) |
| POST /api/products | ✅ | ✅ | ❌ | ❌ |
| PUT /api/products/{id} | ✅ | ✅ (tenant-scoped) | ❌ | ❌ |
| DELETE /api/products/{id} | ✅ | ❌ | ❌ | ❌ |
| GET /api/audit/logs | ✅ | ✅ (tenant-scoped) | ❌ | ❌ |
| GET /api/users/me | ✅ | ✅ | ✅ | ✅ |
| GET /api/admin/users | ✅ | ❌ | ❌ | ❌ |

## State Transitions

### Product Status

```
         ┌──────────────────────────────────┐
         │                                  │
         ▼                                  │
    ┌─────────┐    deactivate()     ┌───────────┐
    │ ACTIVE  │ ─────────────────▶  │ INACTIVE  │
    └────┬────┘                     └─────┬─────┘
         │    ◀─────────────────────      │
         │       activate()               │
         │                                │
         │  delete()                      │  delete()
         │                                │
         ▼                                ▼
    ┌─────────────────────────────────────────┐
    │                DELETED                   │
    │         (terminal state)                 │
    └─────────────────────────────────────────┘
```

## Multi-Tenancy Strategy

- **Approach**: Shared database with discriminator column (`tenant_id`)
- **Resolution**: JWT claim extraction via Finbuckle.MultiTenant
- **Query Filter**: EF Core global query filter on all tenant entities
- **Admin Override**: ADMIN role bypasses tenant filter

```csharp
// Entity base class
public interface ITenantEntity
{
    string TenantId { get; }
}

// EF Core configuration
modelBuilder.Entity<Product>()
    .HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);
```
