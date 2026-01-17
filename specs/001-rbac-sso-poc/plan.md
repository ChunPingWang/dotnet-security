# Implementation Plan: RBAC-SSO Multi-Tenant E-Commerce POC

**Branch**: `001-rbac-sso-poc` | **Date**: 2026-01-17 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-rbac-sso-poc/spec.md`

## Summary

Build a multi-tenant e-commerce platform POC demonstrating enterprise-grade RBAC permission control and SSO integration using .NET 8 with Hexagonal Architecture. The system features:
- OAuth2/OIDC SSO via Keycloak with LDAP federation
- Role-based access control (ADMIN, TENANT_ADMIN, USER, VIEWER)
- Complete tenant data isolation
- Domain Events-driven audit logging via MediatR
- CQRS pattern for command/query separation

## Technical Context

**Language/Version**: .NET 8.0 LTS (C# 12)
**Primary Dependencies**: ASP.NET Core 8.0, Entity Framework Core 8.0, MediatR 12.x, YARP 2.x, Finbuckle.MultiTenant 7.x
**Storage**: PostgreSQL 15+
**Testing**: xUnit 2.x, Moq 4.x, Reqnroll 2.x (BDD), NetArchTest 1.x, WebApplicationFactory
**Target Platform**: Linux containers (Docker/Kubernetes 1.28+)
**Project Type**: Microservices (API Gateway + 3 backend services)
**Performance Goals**: >1000 RPS, <200ms P95 latency
**Constraints**: <200ms P95, 100 concurrent users minimum, 99.9% availability target
**Scale/Scope**: POC scope - Product CRUD, User management, Audit logging across 2+ tenants

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Domain-Driven Design | ✅ PASS | Product Aggregate with Domain Events, Value Objects (ProductId, ProductCode, Money), clear bounded contexts (Product, User, Audit) |
| II. SOLID Principles | ✅ PASS | Interface segregation (IProductRepository, IAuditLogRepository), DI throughout, single-responsibility handlers |
| III. Hexagonal Architecture | ✅ PASS | Domain/Application/Infrastructure layers defined in TECH.md, port interfaces in Application, adapters in Infrastructure |
| IV. TDD/BDD | ✅ PASS | Reqnroll for BDD, xUnit for unit tests, 80%+ coverage target, NetArchTest for architecture validation |
| V. Simplicity (YAGNI) | ✅ PASS | POC scope limits complexity; microservices justified by multi-tenant isolation requirement |

**Complexity Justification**:

| Complexity | Why Needed | Simpler Alternative Rejected Because |
|------------|------------|--------------------------------------|
| 4 services (Gateway + 3) | Tenant isolation, independent scaling, CQRS | Monolith would couple audit writes with product operations |
| MediatR CQRS | Domain Events for audit trail, decoupled handlers | Direct method calls would tightly couple audit to business logic |
| Finbuckle.MultiTenant | Automatic tenant filtering, proven library | Manual tenant filtering error-prone and would require extensive custom code |

## Project Structure

### Documentation (this feature)

```text
specs/001-rbac-sso-poc/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
rbac-sso-poc-dotnet/
├── src/
│   ├── Services/
│   │   ├── RbacSso.ProductService/
│   │   │   ├── Domain/
│   │   │   │   ├── Products/
│   │   │   │   │   ├── Product.cs              # Aggregate Root
│   │   │   │   │   ├── ProductId.cs            # Value Object
│   │   │   │   │   ├── ProductCode.cs          # Value Object
│   │   │   │   │   ├── ProductStatus.cs        # Enum
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
│   │   │   ├── Domain/
│   │   │   ├── Application/
│   │   │   ├── Infrastructure/
│   │   │   └── Api/
│   │   │
│   │   ├── RbacSso.AuditService/
│   │   │   ├── Domain/
│   │   │   ├── Application/
│   │   │   ├── Infrastructure/
│   │   │   └── Api/
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
│   ├── RbacSso.ArchitectureTests/
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
├── RbacSso.sln
└── Directory.Build.props
```

**Structure Decision**: Microservices architecture with shared libraries. Each service follows Hexagonal Architecture with Domain/Application/Infrastructure/Api layers. Gateway handles JWT validation and routing. This structure aligns with TECH.md specifications and supports independent service deployment.
