# Research: RBAC-SSO Multi-Tenant E-Commerce POC

**Date**: 2026-01-17
**Feature**: 001-rbac-sso-poc

## Technology Decisions

### 1. Authentication & SSO

**Decision**: Keycloak 24.x with OAuth2/OIDC Authorization Code Flow

**Rationale**:
- Open-source, enterprise-grade identity provider
- Native LDAP federation support for user directory synchronization
- Built-in multi-tenancy via Realms or custom claims
- JWT tokens with customizable claims (roles, tenant_id)
- Automatic token refresh via refresh_token grant

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| Azure AD B2C | Vendor lock-in, higher cost for POC |
| Auth0 | SaaS dependency, cost at scale |
| IdentityServer | More development effort, less out-of-box features |
| Custom JWT implementation | Security risk, reinventing the wheel |

**Implementation Notes**:
- Configure Keycloak Realm: `ecommerce`
- Client: `gateway` (confidential client)
- Custom claim mapper for `tenant_id` from user attributes
- LDAP User Federation for directory sync
- JWT signature: RS256

### 2. Multi-Tenancy Strategy

**Decision**: Finbuckle.MultiTenant 7.x with shared database, tenant-discriminator column

**Rationale**:
- Proven library with EF Core integration
- Automatic query filtering via `IMultiTenantDbContext`
- Multiple tenant resolution strategies (header, claim, route)
- Minimal infrastructure complexity (single database)

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| Database-per-tenant | Operational complexity, cost for POC |
| Schema-per-tenant | Migration complexity, PostgreSQL schema limits |
| Custom tenant filter | Error-prone, reinventing proven patterns |

**Implementation Notes**:
- Tenant resolution from JWT claim `tenant_id`
- All entities inherit `ITenantEntity` with `TenantId` property
- Global query filter applied via EF Core

### 3. CQRS & Domain Events

**Decision**: MediatR 12.x for CQRS pattern and INotification for Domain Events

**Rationale**:
- Industry-standard library for .NET CQRS
- Clean separation of commands and queries
- Built-in pipeline behaviors for cross-cutting concerns
- INotification for pub/sub domain events

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| Direct method calls | Tight coupling, no audit trail integration point |
| Event Sourcing | Overkill for POC, adds significant complexity |
| Custom mediator | Unnecessary when MediatR is mature and well-supported |

**Implementation Notes**:
- Commands: `CreateProductCommand`, `UpdateProductCommand`, `DeleteProductCommand`
- Queries: `GetProductByIdQuery`, `ListProductsQuery`
- Domain Events: Published after aggregate save, handled by `AuditEventHandler`
- Pipeline behaviors: Logging, Validation, Transaction

### 4. API Gateway

**Decision**: YARP (Yet Another Reverse Proxy) 2.x

**Rationale**:
- Native .NET implementation, high performance
- Configuration-based routing (no code changes for route updates)
- Middleware pipeline integration for JWT validation
- Built-in load balancing, health checks

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| Ocelot | Less actively maintained, YARP is Microsoft-backed |
| Kong/Nginx | Additional infrastructure, not native .NET |
| No gateway | Missing centralized auth, rate limiting, observability |

**Implementation Notes**:
- Routes: `/api/products/*` → ProductService, `/api/audit/*` → AuditService
- JWT validation at gateway level
- Rate limiting: 1000 req/min per client
- Health check endpoints for all downstream services

### 5. Audit Logging Architecture

**Decision**: Domain Events with in-process handler, async persistence

**Rationale**:
- Non-blocking: business operations complete regardless of audit status
- Correlation: events carry correlation ID from HTTP request
- Decoupled: domain aggregates don't know about audit implementation
- Queryable: audit logs stored in PostgreSQL with indexed fields

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| Synchronous logging | Blocks business operations on audit failures |
| Message queue (RabbitMQ) | Added infrastructure for POC, eventual consistency complexity |
| Separate audit database | Operational overhead, cross-database transactions |

**Implementation Notes**:
- `AuditEventHandler` subscribes to all domain events
- Audit log includes: eventType, aggregateType, aggregateId, username, action, payload (JSONB), correlationId
- Payload truncation for large events (>10KB)
- 90-day retention via scheduled PostgreSQL job

### 6. mTLS for Service-to-Service Communication

**Decision**: cert-manager 1.14+ with self-signed CA for POC

**Rationale**:
- Automated certificate provisioning and renewal
- Kubernetes-native, integrates with ingress
- Self-signed CA acceptable for POC (production would use PKI)

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| Manual certificates | Operational burden, renewal complexity |
| Istio service mesh | Overkill for 4-service POC |
| No mTLS | Security requirement from spec (P2 priority) |

**Implementation Notes**:
- Certificate Secret mounted to pods
- Kestrel configured for client certificate validation
- 30-day renewal threshold

## Best Practices Applied

### Hexagonal Architecture in .NET

- **Domain Layer**: Pure C# classes, no attributes, no framework dependencies
- **Application Layer**: MediatR handlers, port interfaces (IProductRepository)
- **Infrastructure Layer**: EF Core implementations, Keycloak client, HTTP clients
- **API Layer**: ASP.NET Core controllers, minimal APIs for simple endpoints

### Testing Strategy

| Layer | Test Type | Framework | Coverage Target |
|-------|-----------|-----------|-----------------|
| Domain | Unit | xUnit + Moq | 90%+ |
| Application | Unit + Integration | xUnit + WebApplicationFactory | 80%+ |
| Infrastructure | Integration | Testcontainers | 70%+ |
| E2E | BDD | Reqnroll | All acceptance scenarios |
| Architecture | Fitness | NetArchTest | Layer dependency rules |

### Error Handling

- Domain exceptions extend `DomainException` with error codes
- Error code format: `{SVC}-{CAT}{SEQ}` (e.g., `PRD-B00001`)
- Global exception handler returns consistent `ErrorResponse` JSON
- Correlation ID in all error responses for debugging

## Open Questions Resolved

| Question | Resolution |
|----------|------------|
| Tenant ID source | JWT claim `tenant_id` from Keycloak |
| Product code uniqueness | Global (not per-tenant) to simplify system-wide queries |
| Audit log storage | Same PostgreSQL database, `audit_logs` table |
| Token refresh strategy | Gateway handles refresh via Keycloak refresh_token grant |
| Cross-tenant admin queries | ADMIN role bypasses tenant filter via special middleware flag |
