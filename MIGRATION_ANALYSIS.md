# MIGRATION_ANALYSIS.md - .NET Core 遷移可行性分析

## RBAC-SSO-POC 遷移評估

**版本**: 1.0  
**日期**: 2025-01-11  
**原始專案**: Java 17 + Spring Boot 3.3.x  
**目標平台**: .NET 8.0 LTS

---

## 1. 執行摘要

### 1.1 可行性結論

| 評估項目 | 結論 |
|----------|------|
| **技術可行性** | ✅ 高度可行 |
| **架構移植性** | ✅ 完全相容 |
| **Domain Events 稽核** | ✅ MediatR 完整支援 |
| **Keycloak 整合** | ✅ 標準 OIDC，無需更換 |
| **風險等級** | 🟡 中低風險 |

### 1.2 關鍵對照

| 功能 | Java/Spring Boot | .NET Core | 對應難度 |
|------|------------------|-----------|----------|
| Web Framework | Spring Boot 3.3 | ASP.NET Core 8.0 | ⭐ 低 |
| ORM | Spring Data JPA | Entity Framework Core 8.0 | ⭐⭐ 中 |
| **Domain Events** | ApplicationEventPublisher | **MediatR INotification** | ⭐ 低 |
| CQRS | 自訂實作 | MediatR IRequest | ⭐ 低 |
| API Gateway | Spring Cloud Gateway | YARP | ⭐⭐ 中 |
| 多租戶 | 自訂 TenantContext | Finbuckle.MultiTenant | ⭐ 低 |
| BDD 測試 | Cucumber-JVM | Reqnroll | ⭐⭐ 中 |

---

## 2. 原始專案分析

### 2.1 專案狀態（Java 版）

| 指標 | 狀態 |
|------|------|
| 建置狀態 | BUILD SUCCESSFUL |
| 測試數量 | 344 tests |
| 測試通過率 | 100% |
| 覆蓋率 | 92-96% |
| Cucumber 場景 | 18 scenarios |

### 2.2 核心功能

- ✅ 多租戶架構 (Multi-tenancy)
- ✅ RBAC 權限控制
- ✅ SSO 單一登入 (Keycloak OAuth2/OIDC)
- ✅ Hexagonal Architecture
- ✅ DDD 領域驅動設計
- ✅ CQRS 模式
- ✅ **Domain Events 稽核日誌**
- ✅ BDD 測試 (Cucumber)
- ✅ mTLS 東西向安全

### 2.3 稽核機制說明

原專案使用 **Domain Events** 進行稽核（`domain-event-for-audit` 分支）：

```java
// Java Spring Boot - Domain Event 稽核
public UUID handle(CreateProductCommand cmd) {
    Product product = Product.create(...);
    
    // 發布 Domain Events
    eventPublisher.publish(product.pullDomainEvents());
    // ProductCreated 事件由 AuditDomainEventListener 捕獲並記錄
    
    return product.getId().getValue();
}
```

---

## 3. .NET Core 遷移方案

### 3.1 Domain Events 稽核實作

**.NET Core 使用 MediatR：**

```csharp
// Domain Event
public record ProductCreated(
    Guid ProductId,
    string Name,
    decimal Price,
    string CreatedBy,
    DateTimeOffset OccurredAt
) : IDomainEvent;

// Command Handler - 發布 Domain Events
public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
{
    var product = Product.Create(...);
    
    await _repository.AddAsync(product, ct);
    await _repository.SaveChangesAsync(ct);
    
    // 發布 Domain Events (與 Java 版相同模式)
    foreach (var domainEvent in product.PullDomainEvents())
    {
        await _mediator.Publish(domainEvent, ct);
    }
    
    return product.Id.Value;
}

// Audit Handler - 訂閱 Domain Events
public class AuditEventHandler : INotificationHandler<ProductCreated>
{
    public async Task Handle(ProductCreated notification, CancellationToken ct)
    {
        var auditLog = AuditLog.Create(
            eventType: notification.EventType,
            aggregateType: "Product",
            aggregateId: notification.ProductId.ToString(),
            username: notification.CreatedBy,
            // ...
        );
        
        await _auditRepository.AddAsync(auditLog, ct);
    }
}
```

### 3.2 技術選型

| 類別 | 推薦方案 | 理由 |
|------|----------|------|
| Runtime | .NET 8.0 LTS | 長期支援、效能優異 |
| Domain Events | MediatR | 社群廣泛採用、輕量 |
| ORM | EF Core 8.0 | 官方支援、成熟穩定 |
| API Gateway | YARP | 微軟官方、效能好 |
| 多租戶 | Finbuckle.MultiTenant | 成熟方案、彈性高 |
| BDD 測試 | Reqnroll | SpecFlow 後繼者、開源 |

---

## 4. 工項規劃

### 4.1 Phase 分解

| Phase | 工項 | 人天 |
|-------|------|------|
| **Phase 1: 基礎架構** | | **18** |
| | 建立 Solution 結構 (Clean Architecture) | 2 |
| | 設定 EF Core + PostgreSQL | 3 |
| | 建立 Domain Layer (Entities, Value Objects) | 5 |
| | 實作 Domain Events 機制 (MediatR) | 3 |
| | 建立共用函式庫 | 5 |
| **Phase 2: 核心服務** | | **26** |
| | Product Service - Domain Layer | 3 |
| | Product Service - Application Layer (CQRS) | 4 |
| | Product Service - Infrastructure Layer | 3 |
| | Product Service - API Layer | 2 |
| | User Service 遷移 | 5 |
| | **Audit Service (Domain Events)** | 5 |
| | API Gateway (YARP) | 4 |
| **Phase 3: 安全整合** | | **14** |
| | Keycloak OIDC 整合 | 3 |
| | JWT 驗證與 RBAC | 3 |
| | 多租戶實作 (Finbuckle) | 3 |
| | mTLS 東西向安全 | 3 |
| | API 授權策略 | 2 |
| **Phase 4: 測試部署** | | **19** |
| | 單元測試 (xUnit + Moq) | 5 |
| | BDD 測試 (Reqnroll) | 5 |
| | 架構測試 (NetArchTest) | 2 |
| | Docker 容器化 | 2 |
| | Kubernetes 部署設定 | 3 |
| | CI/CD Pipeline | 2 |
| **總計** | | **77 人天** |

### 4.2 時程估算

| 資源配置 | 預估時程 |
|----------|----------|
| 1 人全職 | 約 3.5 個月 |
| 2 人全職 | 約 2 個月 |
| 3 人全職 | 約 1.5 個月 |

---

## 5. 風險評估

### 5.1 風險矩陣

| 風險 | 機率 | 影響 | 緩解措施 |
|------|------|------|----------|
| 團隊 .NET 經驗不足 | 中 | 中 | 培訓、建立 Coding Guidelines |
| Cucumber → Reqnroll 轉換 | 低 | 低 | Gherkin 語法相容 |
| Spring Cloud Gateway → YARP | 中 | 中 | 設定語法不同，需重新設計 |
| 效能差異 | 低 | 中 | 基準測試、效能調校 |

### 5.2 建議

1. **先執行 Pilot** - 先遷移 Product Service 驗證架構
2. **保留 Keycloak** - 標準 OIDC，無需更換認證系統
3. **重視測試覆蓋** - 確保 BDD 測試完整轉換
4. **建立 Coding Guidelines** - 統一 .NET 開發規範

---

## 6. 交付文件清單

| 文件 | 說明 |
|------|------|
| ✅ PRD.md | 產品需求文件 (.NET Core 版) |
| ✅ TECH.md | 技術架構文件 (含 Domain Events 詳細設計) |
| ✅ INFRA.md | 基礎設施文件 (Docker/K8s/mTLS) |
| ✅ MIGRATION_ANALYSIS.md | 遷移可行性分析 (本文件) |

---

## 附錄：NuGet 套件清單

```xml
<!-- Domain Events / CQRS -->
<PackageReference Include="MediatR" Version="12.2.0" />

<!-- Infrastructure -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
<PackageReference Include="Finbuckle.MultiTenant" Version="7.0.0" />

<!-- API -->
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
