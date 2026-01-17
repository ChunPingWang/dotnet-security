# TECH.md - 技術架構文件

## RBAC-SSO-POC (.NET Core 版)

**版本**: 1.0  
**日期**: 2025-01-11  
**稽核機制**: Domain Events

---

## 1. 技術棧

### 1.1 核心技術

| 類別 | 技術 | 版本 | 說明 |
|------|------|------|------|
| Runtime | .NET | 8.0 LTS | 長期支援版本 |
| Web Framework | ASP.NET Core | 8.0 | Web API |
| ORM | Entity Framework Core | 8.0 | 資料存取 |
| Mediator | MediatR | 12.x | CQRS + Domain Events |
| API Gateway | YARP | 2.x | 反向代理 |
| 多租戶 | Finbuckle.MultiTenant | 7.x | 多租戶支援 |
| 驗證 | JWT Bearer | 8.0 | Token 驗證 |
| API 文件 | Swashbuckle | 6.x | OpenAPI/Swagger |

### 1.2 測試框架

| 類別 | 技術 | 版本 |
|------|------|------|
| 單元測試 | xUnit | 2.x |
| Mocking | Moq / NSubstitute | 4.x |
| BDD | Reqnroll (SpecFlow) | 2.x |
| 架構測試 | NetArchTest | 1.x |
| 整合測試 | WebApplicationFactory | 8.0 |

### 1.3 基礎設施

| 類別 | 技術 | 版本 |
|------|------|------|
| 資料庫 | PostgreSQL | 15+ |
| 認證服務 | Keycloak | 24.x |
| 目錄服務 | OpenLDAP | 2.6 |
| 容器 | Docker | 24+ |
| 編排 | Kubernetes | 1.28+ |
| 憑證管理 | cert-manager | 1.14+ |

---

## 2. 系統架構

### 2.1 整體架構圖

```
┌─────────────────────────────────────────────────────────────────────────┐
│                            Client Layer                                  │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐                 │
│  │ Web Browser │    │ Mobile App  │    │  API Client │                 │
│  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘                 │
└─────────┼──────────────────┼──────────────────┼─────────────────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        API Gateway (YARP)                                │
│                           :8080                                          │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │  • JWT 驗證          • Rate Limiting      • Load Balancing         │ │
│  │  • 路由轉發          • Request Logging    • Health Checks          │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└────────────────────────────┬────────────────────────────────────────────┘
                             │
          ┌──────────────────┼──────────────────┐
          │                  │                  │
          ▼                  ▼                  ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ Product Service │ │  User Service   │ │  Audit Service  │
│     :8081       │ │     :8082       │ │     :8083       │
│                 │ │                 │ │                 │
│  ┌───────────┐  │ │  ┌───────────┐  │ │  ┌───────────┐  │
│  │  Domain   │  │ │  │  Domain   │  │ │  │  Domain   │  │
│  │  Events   │──┼─┼──│  Events   │──┼─┼──│  Handler  │  │
│  └───────────┘  │ │  └───────────┘  │ │  └───────────┘  │
└────────┬────────┘ └────────┬────────┘ └────────┬────────┘
         │                   │                   │
         ▼                   ▼                   ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         PostgreSQL Database                              │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐                 │
│  │  products   │    │    users    │    │ audit_logs  │                 │
│  └─────────────┘    └─────────────┘    └─────────────┘                 │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                      Authentication (Keycloak)                           │
│                           :8180                                          │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │  • OAuth2/OIDC       • LDAP Federation    • Realm: ecommerce       │ │
│  │  • JWT 簽發          • User Management    • Client: gateway        │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Hexagonal Architecture (Clean Architecture)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          PRESENTATION LAYER                              │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │  Controllers / Minimal APIs / Middleware                           │ │
│  │  • ProductController          • Request/Response DTOs              │ │
│  │  • UserController             • Exception Handling                 │ │
│  │  • AuditController            • Swagger Documentation              │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          APPLICATION LAYER                               │
│  ┌──────────────────────────┐    ┌──────────────────────────┐          │
│  │       Commands           │    │        Queries           │          │
│  │  • CreateProductCommand  │    │  • GetProductByIdQuery   │          │
│  │  • UpdateProductCommand  │    │  • ListProductsQuery     │          │
│  │  • DeleteProductCommand  │    │  • GetAuditLogsQuery     │          │
│  └──────────────────────────┘    └──────────────────────────┘          │
│                                                                         │
│  ┌──────────────────────────┐    ┌──────────────────────────┐          │
│  │    Command Handlers      │    │     Query Handlers       │          │
│  │  • CreateProductHandler  │    │  • GetProductHandler     │          │
│  │  • UpdateProductHandler  │    │  • ListProductsHandler   │          │
│  └──────────────────────────┘    └──────────────────────────┘          │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                      Domain Event Publisher                        │ │
│  │  • Publish events via MediatR INotification                        │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                            DOMAIN LAYER                                  │
│  ┌──────────────────────────┐    ┌──────────────────────────┐          │
│  │     Aggregates           │    │    Value Objects         │          │
│  │  • Product               │    │  • ProductId             │          │
│  │    - RegisterEvent()     │    │  • ProductCode           │          │
│  │    - PullDomainEvents()  │    │  • Money                 │          │
│  └──────────────────────────┘    └──────────────────────────┘          │
│                                                                         │
│  ┌──────────────────────────┐    ┌──────────────────────────┐          │
│  │    Domain Events         │    │    Repository Interfaces │          │
│  │  • ProductCreated        │    │  • IProductRepository    │          │
│  │  • ProductUpdated        │    │  • IAuditLogRepository   │          │
│  │  • ProductDeleted        │    │                          │          │
│  │  • ProductPriceChanged   │    │                          │          │
│  └──────────────────────────┘    └──────────────────────────┘          │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        INFRASTRUCTURE LAYER                              │
│  ┌──────────────────────────┐    ┌──────────────────────────┐          │
│  │   EF Core Repositories   │    │    External Services     │          │
│  │  • EfProductRepository   │    │  • KeycloakService       │          │
│  │  • EfAuditLogRepository  │    │  • LdapService           │          │
│  └──────────────────────────┘    └──────────────────────────┘          │
│                                                                         │
│  ┌──────────────────────────┐    ┌──────────────────────────┐          │
│  │   Domain Event Handlers  │    │     Persistence          │          │
│  │  • AuditEventHandler     │    │  • DbContext             │          │
│  │    (訂閱所有 Domain Events) │    │  • Migrations           │          │
│  └──────────────────────────┘    └──────────────────────────┘          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Domain Events 稽核機制

### 3.1 架構設計

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    Domain Events 稽核流程                                 │
└──────────────────────────────────────────────────────────────────────────┘

  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌──────────┐
  │  Controller │────▶│   Handler   │────▶│  Aggregate  │────▶│ Register │
  │             │     │  (MediatR)  │     │  (Product)  │     │  Event   │
  └─────────────┘     └─────────────┘     └─────────────┘     └────┬─────┘
                                                                   │
                                                                   ▼
  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌──────────┐
  │  AuditLog   │◀────│   Audit     │◀────│   MediatR   │◀────│  Pull &  │
  │  Database   │     │   Handler   │     │  Publish()  │     │  Publish │
  └─────────────┘     └─────────────┘     └─────────────┘     └──────────┘
```

### 3.2 程式碼實作

#### 3.2.1 Domain Event 介面

```csharp
// Domain/Common/IDomainEvent.cs
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
    string EventType { get; }
}

// Domain/Common/DomainEventBase.cs
public abstract record DomainEventBase : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    public abstract string EventType { get; }
}
```

#### 3.2.2 Product Domain Events

```csharp
// Domain/Products/Events/ProductCreated.cs
public record ProductCreated(
    Guid ProductId,
    string ProductCode,
    string Name,
    decimal Price,
    string Category,
    string TenantId,
    string CreatedBy
) : DomainEventBase
{
    public override string EventType => "PRODUCT_CREATED";
}

// Domain/Products/Events/ProductUpdated.cs
public record ProductUpdated(
    Guid ProductId,
    string Name,
    decimal Price,
    string Category,
    string UpdatedBy
) : DomainEventBase
{
    public override string EventType => "PRODUCT_UPDATED";
}

// Domain/Products/Events/ProductPriceChanged.cs
public record ProductPriceChanged(
    Guid ProductId,
    decimal OldPrice,
    decimal NewPrice,
    string ChangedBy
) : DomainEventBase
{
    public override string EventType => "PRODUCT_PRICE_CHANGED";
}

// Domain/Products/Events/ProductDeleted.cs
public record ProductDeleted(
    Guid ProductId,
    string DeletedBy
) : DomainEventBase
{
    public override string EventType => "PRODUCT_DELETED";
}
```

#### 3.2.3 Aggregate Root 基底類別

```csharp
// Domain/Common/AggregateRoot.cs
public abstract class AggregateRoot<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;
    
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void RegisterDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public IReadOnlyCollection<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }
}
```

#### 3.2.4 Product Aggregate

```csharp
// Domain/Products/Product.cs
public class Product : AggregateRoot<ProductId>
{
    public ProductCode ProductCode { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public string? Description { get; private set; }
    public ProductStatus Status { get; private set; }
    public string TenantId { get; private set; } = null!;
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    
    private Product() { } // For EF Core
    
    public static Product Create(
        ProductCode productCode,
        string name,
        Money price,
        string category,
        string? description,
        string tenantId,
        string createdBy)
    {
        var product = new Product
        {
            Id = ProductId.Create(),
            ProductCode = productCode,
            Name = name,
            Price = price,
            Category = category,
            Description = description,
            Status = ProductStatus.Active,
            TenantId = tenantId,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        // 註冊 Domain Event
        product.RegisterDomainEvent(new ProductCreated(
            product.Id.Value,
            product.ProductCode.Value,
            product.Name,
            product.Price.Amount,
            product.Category,
            product.TenantId,
            product.CreatedBy
        ));
        
        return product;
    }
    
    public void Update(string name, Money price, string category, string? description, string updatedBy)
    {
        var oldPrice = Price;
        
        Name = name;
        Price = price;
        Category = category;
        Description = description;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
        
        // 註冊更新事件
        RegisterDomainEvent(new ProductUpdated(
            Id.Value, Name, Price.Amount, Category, updatedBy
        ));
        
        // 如果價格變更，額外註冊價格變更事件
        if (oldPrice != price)
        {
            RegisterDomainEvent(new ProductPriceChanged(
                Id.Value, oldPrice.Amount, price.Amount, updatedBy
            ));
        }
    }
    
    public void Delete(string deletedBy)
    {
        Status = ProductStatus.Deleted;
        UpdatedBy = deletedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
        
        RegisterDomainEvent(new ProductDeleted(Id.Value, deletedBy));
    }
}
```

#### 3.2.5 Command Handler (發布 Domain Events)

```csharp
// Application/Products/Commands/CreateProductHandler.cs
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;
    
    public CreateProductHandler(
        IProductRepository repository,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IMediator mediator)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _mediator = mediator;
    }
    
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var productCode = string.IsNullOrEmpty(request.ProductCode)
            ? ProductCode.Generate()
            : ProductCode.Create(request.ProductCode);
            
        if (await _repository.ExistsByCodeAsync(productCode, ct))
            throw new DomainException($"Product code {productCode} already exists");
        
        var product = Product.Create(
            productCode,
            request.Name,
            Money.Create(request.Price),
            request.Category,
            request.Description,
            _tenantContext.TenantId,
            _currentUser.Username
        );
        
        await _repository.AddAsync(product, ct);
        await _repository.SaveChangesAsync(ct);
        
        // 發布 Domain Events
        foreach (var domainEvent in product.PullDomainEvents())
        {
            await _mediator.Publish(domainEvent, ct);
        }
        
        return product.Id.Value;
    }
}
```

#### 3.2.6 Audit Event Handler (訂閱 Domain Events)

```csharp
// Infrastructure/Audit/AuditEventHandler.cs
public class AuditEventHandler :
    INotificationHandler<ProductCreated>,
    INotificationHandler<ProductUpdated>,
    INotificationHandler<ProductDeleted>,
    INotificationHandler<ProductPriceChanged>
{
    private readonly IAuditLogRepository _auditRepository;
    private readonly ICorrelationIdAccessor _correlationId;
    private readonly IHttpContextAccessor _httpContext;
    
    public AuditEventHandler(
        IAuditLogRepository auditRepository,
        ICorrelationIdAccessor correlationId,
        IHttpContextAccessor httpContext)
    {
        _auditRepository = auditRepository;
        _correlationId = correlationId;
        _httpContext = httpContext;
    }
    
    public Task Handle(ProductCreated notification, CancellationToken ct)
        => CreateAuditLog(notification, "CREATE", "Product", notification.ProductId.ToString(), ct);
    
    public Task Handle(ProductUpdated notification, CancellationToken ct)
        => CreateAuditLog(notification, "UPDATE", "Product", notification.ProductId.ToString(), ct);
    
    public Task Handle(ProductDeleted notification, CancellationToken ct)
        => CreateAuditLog(notification, "DELETE", "Product", notification.ProductId.ToString(), ct);
    
    public Task Handle(ProductPriceChanged notification, CancellationToken ct)
        => CreateAuditLog(notification, "PRICE_CHANGE", "Product", notification.ProductId.ToString(), ct);
    
    private async Task CreateAuditLog(
        IDomainEvent domainEvent,
        string action,
        string aggregateType,
        string aggregateId,
        CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
        var clientIp = _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString();
        
        var auditLog = AuditLog.Create(
            eventType: domainEvent.EventType,
            aggregateType: aggregateType,
            aggregateId: aggregateId,
            username: GetUsernameFromEvent(domainEvent),
            serviceName: "ProductService",
            action: action,
            payload: payload,
            result: AuditResult.Success,
            clientIp: clientIp,
            correlationId: _correlationId.CorrelationId
        );
        
        await _auditRepository.AddAsync(auditLog, ct);
        await _auditRepository.SaveChangesAsync(ct);
    }
    
    private string GetUsernameFromEvent(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            ProductCreated e => e.CreatedBy,
            ProductUpdated e => e.UpdatedBy,
            ProductDeleted e => e.DeletedBy,
            ProductPriceChanged e => e.ChangedBy,
            _ => "system"
        };
    }
}
```

### 3.3 Domain Events 序列圖

```
┌────────┐     ┌───────────┐     ┌─────────┐     ┌─────────┐     ┌───────────┐     ┌──────────┐
│Client  │     │Controller │     │Handler  │     │Product  │     │MediatR    │     │AuditHandler│
└───┬────┘     └─────┬─────┘     └────┬────┘     └────┬────┘     └─────┬─────┘     └─────┬────┘
    │               │                 │               │                │                 │
    │ POST /products│                 │               │                │                 │
    │──────────────▶│                 │               │                │                 │
    │               │                 │               │                │                 │
    │               │  Send Command   │               │                │                 │
    │               │────────────────▶│               │                │                 │
    │               │                 │               │                │                 │
    │               │                 │ Create()      │                │                 │
    │               │                 │──────────────▶│                │                 │
    │               │                 │               │                │                 │
    │               │                 │               │ RegisterEvent  │                 │
    │               │                 │               │ (ProductCreated)                 │
    │               │                 │               │────────┐       │                 │
    │               │                 │               │        │       │                 │
    │               │                 │               │◀───────┘       │                 │
    │               │                 │               │                │                 │
    │               │                 │◀──────────────│                │                 │
    │               │                 │  product      │                │                 │
    │               │                 │               │                │                 │
    │               │                 │ Save to DB    │                │                 │
    │               │                 │───────────────│                │                 │
    │               │                 │               │                │                 │
    │               │                 │ PullDomainEvents()             │                 │
    │               │                 │──────────────▶│                │                 │
    │               │                 │               │                │                 │
    │               │                 │◀──────────────│                │                 │
    │               │                 │  events[]     │                │                 │
    │               │                 │               │                │                 │
    │               │                 │ Publish(ProductCreated)        │                 │
    │               │                 │───────────────────────────────▶│                 │
    │               │                 │               │                │                 │
    │               │                 │               │                │ Handle()        │
    │               │                 │               │                │────────────────▶│
    │               │                 │               │                │                 │
    │               │                 │               │                │                 │ Create
    │               │                 │               │                │                 │ AuditLog
    │               │                 │               │                │                 │────┐
    │               │                 │               │                │                 │    │
    │               │                 │               │                │                 │◀───┘
    │               │                 │               │                │                 │
    │               │                 │               │                │◀────────────────│
    │               │                 │               │                │                 │
    │               │                 │◀──────────────────────────────│                 │
    │               │                 │               │                │                 │
    │               │◀────────────────│               │                │                 │
    │               │  201 Created    │               │                │                 │
    │◀──────────────│                 │               │                │                 │
    │               │                 │               │                │                 │
```

---

## 4. 專案結構

### 4.1 Solution 結構

```
rbac-sso-poc-dotnet/
├── src/
│   ├── Services/
│   │   ├── RbacSso.ProductService/
│   │   │   ├── Domain/
│   │   │   │   ├── Products/
│   │   │   │   │   ├── Product.cs
│   │   │   │   │   ├── ProductId.cs
│   │   │   │   │   ├── ProductCode.cs
│   │   │   │   │   ├── ProductStatus.cs
│   │   │   │   │   └── Events/
│   │   │   │   │       ├── ProductCreated.cs
│   │   │   │   │       ├── ProductUpdated.cs
│   │   │   │   │       ├── ProductDeleted.cs
│   │   │   │   │       └── ProductPriceChanged.cs
│   │   │   │   └── Common/
│   │   │   │       ├── AggregateRoot.cs
│   │   │   │       ├── IDomainEvent.cs
│   │   │   │       ├── Money.cs
│   │   │   │       └── IRepository.cs
│   │   │   ├── Application/
│   │   │   │   ├── Products/
│   │   │   │   │   ├── Commands/
│   │   │   │   │   │   ├── CreateProductCommand.cs
│   │   │   │   │   │   ├── CreateProductHandler.cs
│   │   │   │   │   │   ├── UpdateProductCommand.cs
│   │   │   │   │   │   └── DeleteProductCommand.cs
│   │   │   │   │   └── Queries/
│   │   │   │   │       ├── GetProductByIdQuery.cs
│   │   │   │   │       └── ListProductsQuery.cs
│   │   │   │   └── Common/
│   │   │   │       └── Interfaces/
│   │   │   ├── Infrastructure/
│   │   │   │   ├── Persistence/
│   │   │   │   │   ├── ProductDbContext.cs
│   │   │   │   │   ├── EfProductRepository.cs
│   │   │   │   │   └── Configurations/
│   │   │   │   └── Audit/
│   │   │   │       └── AuditEventHandler.cs
│   │   │   └── Api/
│   │   │       ├── Controllers/
│   │   │       │   ├── ProductCommandController.cs
│   │   │       │   └── ProductQueryController.cs
│   │   │       ├── Program.cs
│   │   │       └── appsettings.json
│   │   │
│   │   ├── RbacSso.UserService/
│   │   │   └── ...
│   │   │
│   │   ├── RbacSso.AuditService/
│   │   │   └── ...
│   │   │
│   │   └── RbacSso.Gateway/
│   │       ├── Program.cs
│   │       └── yarp.json
│   │
│   └── Shared/
│       ├── RbacSso.Common/
│       │   ├── Exceptions/
│       │   ├── Responses/
│       │   └── Extensions/
│       ├── RbacSso.Security/
│       │   ├── Authentication/
│       │   └── Authorization/
│       ├── RbacSso.Tenant/
│       │   ├── ITenantContext.cs
│       │   └── TenantMiddleware.cs
│       └── RbacSso.Audit/
│           ├── Domain/
│           │   ├── AuditLog.cs
│           │   └── AuditResult.cs
│           └── IAuditLogRepository.cs
│
├── tests/
│   ├── RbacSso.ProductService.UnitTests/
│   ├── RbacSso.ProductService.IntegrationTests/
│   └── RbacSso.ScenarioTests/
│       └── Features/
│           ├── ProductManagement.feature
│           ├── Rbac.feature
│           └── MultiTenant.feature
│
├── deploy/
│   ├── docker/
│   │   └── docker-compose.yml
│   ├── k8s/
│   │   ├── base/
│   │   └── overlays/
│   └── scripts/
│
├── docs/
│   ├── PRD.md
│   ├── TECH.md
│   └── INFRA.md
│
├── RbacSso.sln
└── Directory.Build.props
```

### 4.2 專案相依性

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Solution                                       │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
         ┌──────────────────────────┼──────────────────────────┐
         │                          │                          │
         ▼                          ▼                          ▼
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│ ProductService  │      │  UserService    │      │   Gateway       │
│                 │      │                 │      │                 │
│  Domain ◀───────│──────│──────┐          │      │   YARP         │
│  Application    │      │      │          │      │                 │
│  Infrastructure │      │      │          │      │                 │
│  Api            │      │      │          │      │                 │
└────────┬────────┘      └──────┼──────────┘      └─────────────────┘
         │                      │
         │                      │
         ▼                      ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        Shared Libraries                                  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │
│  │   Common    │  │  Security   │  │   Tenant    │  │    Audit    │    │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 5. 安全架構

### 5.1 認證流程 (OAuth2/OIDC)

```
┌────────┐     ┌─────────┐     ┌──────────┐     ┌─────────────┐
│ Client │     │ Gateway │     │ Keycloak │     │ Microservice│
└───┬────┘     └────┬────┘     └─────┬────┘     └──────┬──────┘
    │               │                │                  │
    │ 1. Access Resource            │                  │
    │──────────────▶│                │                  │
    │               │                │                  │
    │ 2. 302 Redirect to Keycloak   │                  │
    │◀──────────────│                │                  │
    │               │                │                  │
    │ 3. Login Page │                │                  │
    │───────────────────────────────▶│                  │
    │               │                │                  │
    │               │ 4. LDAP Bind   │                  │
    │               │                │──────▶          │
    │               │                │◀──────          │
    │               │                │                  │
    │ 5. Authorization Code         │                  │
    │◀──────────────────────────────│                  │
    │               │                │                  │
    │ 6. Code      │                │                  │
    │──────────────▶│                │                  │
    │               │                │                  │
    │               │ 7. Exchange Token                │
    │               │───────────────▶│                  │
    │               │                │                  │
    │               │ 8. JWT Token   │                  │
    │               │◀───────────────│                  │
    │               │                │                  │
    │ 9. Access with JWT            │                  │
    │──────────────▶│                │                  │
    │               │                │                  │
    │               │ 10. Forward Request (JWT Header) │
    │               │─────────────────────────────────▶│
    │               │                │                  │
    │               │                │     11. Validate │
    │               │                │         JWT     │
    │               │                │                  │
    │               │ 12. Response   │                  │
    │               │◀─────────────────────────────────│
    │               │                │                  │
    │ 13. Response  │                │                  │
    │◀──────────────│                │                  │
```

### 5.2 JWT Token 結構

```json
{
  "header": {
    "alg": "RS256",
    "typ": "JWT",
    "kid": "keycloak-key-id"
  },
  "payload": {
    "iss": "http://keycloak:8180/realms/ecommerce",
    "sub": "user-uuid",
    "aud": "gateway",
    "exp": 1704067200,
    "iat": 1704063600,
    "preferred_username": "admin",
    "email": "admin@example.com",
    "realm_access": {
      "roles": ["ADMIN", "TENANT_ADMIN"]
    },
    "tenant_id": "tenant-a",
    "groups": ["/admins", "/tenant-a-users"]
  }
}
```

### 5.3 mTLS 配置

```csharp
// Program.cs - Kestrel mTLS 配置
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(https =>
    {
        https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        https.ServerCertificate = X509Certificate2.CreateFromPemFile(
            "/etc/ssl/certs/tls.crt",
            "/etc/ssl/certs/tls.key"
        );
        https.ClientCertificateValidation = (cert, chain, errors) =>
        {
            // 驗證客戶端憑證是否由 CA 簽發
            return errors == SslPolicyErrors.None;
        };
    });
});
```

---

## 6. 資料庫設計

### 6.1 ER Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              products                                    │
├─────────────────────────────────────────────────────────────────────────┤
│ id              UUID          PK                                        │
│ product_code    VARCHAR(10)   UK    "P" + 6 digits                      │
│ name            VARCHAR(255)  NOT NULL                                  │
│ price           DECIMAL(19,4) NOT NULL                                  │
│ category        VARCHAR(100)  NOT NULL                                  │
│ description     TEXT                                                    │
│ status          VARCHAR(20)   NOT NULL  (ACTIVE, INACTIVE, DELETED)     │
│ tenant_id       VARCHAR(50)   NOT NULL                                  │
│ created_by      VARCHAR(100)  NOT NULL                                  │
│ created_at      TIMESTAMPTZ   NOT NULL                                  │
│ updated_by      VARCHAR(100)                                            │
│ updated_at      TIMESTAMPTZ                                             │
├─────────────────────────────────────────────────────────────────────────┤
│ INDEXES:                                                                 │
│   idx_products_tenant    (tenant_id)                                    │
│   idx_products_category  (category)                                     │
│   idx_products_status    (status)                                       │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ generates
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
│ action           VARCHAR(50)   NOT NULL                                 │
│ payload          JSONB         NOT NULL                                 │
│ result           VARCHAR(20)   NOT NULL  (SUCCESS, FAILURE)             │
│ error_message    TEXT                                                   │
│ client_ip        VARCHAR(45)                                            │
│ correlation_id   VARCHAR(100)  NOT NULL                                 │
│ payload_truncated BOOLEAN      DEFAULT FALSE                            │
├─────────────────────────────────────────────────────────────────────────┤
│ INDEXES:                                                                 │
│   idx_audit_timestamp    (timestamp DESC)                               │
│   idx_audit_username     (username, timestamp DESC)                     │
│   idx_audit_aggregate    (aggregate_type, aggregate_id, timestamp DESC) │
│   idx_audit_event_type   (event_type, timestamp DESC)                   │
│   idx_audit_correlation  (correlation_id)                               │
│   idx_audit_result       (result, timestamp DESC)                       │
└─────────────────────────────────────────────────────────────────────────┘
```

### 6.2 EF Core Configuration

```csharp
// Infrastructure/Persistence/Configurations/ProductConfiguration.cs
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id)
            .HasConversion(
                id => id.Value,
                value => ProductId.Create(value))
            .HasColumnName("id");
        
        builder.Property(p => p.ProductCode)
            .HasConversion(
                code => code.Value,
                value => ProductCode.Create(value))
            .HasColumnName("product_code")
            .HasMaxLength(10);
        
        builder.Property(p => p.Price)
            .HasConversion(
                money => money.Amount,
                value => Money.Create(value))
            .HasColumnName("price")
            .HasPrecision(19, 4);
        
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20);
        
        builder.HasIndex(p => p.ProductCode).IsUnique();
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => p.Category);
        
        // Ignore Domain Events
        builder.Ignore(p => p.DomainEvents);
    }
}
```

---

## 7. 測試策略

### 7.1 測試金字塔

```
                    ┌─────────────┐
                    │    E2E      │  ← Reqnroll BDD
                    │   Tests     │    (18 scenarios)
                    └──────┬──────┘
                           │
                ┌──────────┴──────────┐
                │   Integration       │  ← WebApplicationFactory
                │      Tests          │    (API + DB)
                └──────────┬──────────┘
                           │
        ┌──────────────────┴──────────────────┐
        │           Unit Tests                 │  ← xUnit + Moq
        │  • Domain (Aggregates, Value Objects)│    (80%+ coverage)
        │  • Application (Handlers)            │
        │  • Infrastructure (Repositories)     │
        └─────────────────────────────────────┘
```

### 7.2 架構測試 (NetArchTest)

```csharp
// Tests/ArchitectureTests.cs
public class ArchitectureTests
{
    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Product).Assembly)
            .That().ResideInNamespace("RbacSso.ProductService.Domain")
            .ShouldNot().HaveDependencyOn("RbacSso.ProductService.Infrastructure")
            .GetResult();
        
        Assert.True(result.IsSuccessful);
    }
    
    [Fact]
    public void Domain_Events_Should_Implement_IDomainEvent()
    {
        var result = Types.InAssembly(typeof(Product).Assembly)
            .That().ResideInNamespace("RbacSso.ProductService.Domain")
            .And().HaveNameEndingWith("Event")
            .Should().ImplementInterface(typeof(IDomainEvent))
            .GetResult();
        
        Assert.True(result.IsSuccessful);
    }
}
```

---

## 8. API 規格

### 8.1 Product API

#### POST /api/products

```yaml
Request:
  Headers:
    Authorization: Bearer {jwt_token}
    Content-Type: application/json
  Body:
    productCode: string (optional, auto-generated if empty)
    name: string (required)
    price: decimal (required, > 0)
    category: string (required)
    description: string (optional)

Response (201 Created):
  Body:
    success: true
    data:
      id: "uuid"
    timestamp: "2025-01-11T10:00:00Z"

Response (400 Bad Request):
  Body:
    success: false
    error:
      code: "VALIDATION_ERROR"
      message: "Price must be greater than zero"

Response (403 Forbidden):
  Body:
    success: false
    error:
      code: "ACCESS_DENIED"
      message: "Insufficient permissions"
```

---

## 附錄

### A. NuGet 套件清單

```xml
<!-- Domain/Application Layer -->
<PackageReference Include="MediatR" Version="12.2.0" />

<!-- Infrastructure Layer -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
<PackageReference Include="Finbuckle.MultiTenant" Version="7.0.0" />

<!-- API Layer -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />

<!-- Gateway -->
<PackageReference Include="Yarp.ReverseProxy" Version="2.1.0" />

<!-- Testing -->
<PackageReference Include="xunit" Version="2.6.0" />
<PackageReference Include="Moq" Version="4.20.0" />
<PackageReference Include="Reqnroll" Version="2.0.0" />
<PackageReference Include="NetArchTest.Rules" Version="1.3.0" />
```

### B. 參考文件

- PRD.md - 產品需求文件
- INFRA.md - 基礎設施文件
- Microsoft Docs - ASP.NET Core
- MediatR Documentation
