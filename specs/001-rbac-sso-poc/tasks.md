# Tasks: RBAC-SSO Multi-Tenant E-Commerce POC

**Input**: Design documents from `/specs/001-rbac-sso-poc/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Required per Constitution (TDD/BDD - NON-NEGOTIABLE, 80%+ coverage target)

**Organization**: Tasks grouped by user story (US1-US6) for independent implementation and testing.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1, US2, US3, US4, US5, US6 (maps to spec.md user stories)
- All paths relative to `rbac-sso-poc-dotnet/` root

## Path Conventions

```text
rbac-sso-poc-dotnet/
├── src/Services/RbacSso.*/          # Service projects
├── src/Shared/RbacSso.*/            # Shared libraries
├── tests/RbacSso.*.UnitTests/       # Unit tests
├── tests/RbacSso.*.IntegrationTests/ # Integration tests
├── tests/RbacSso.ScenarioTests/     # BDD tests
└── tests/RbacSso.ArchitectureTests/ # Architecture tests
```

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create solution structure and configure shared dependencies

- [X] T001 Create solution file RbacSso.sln and Directory.Build.props with common settings
- [X] T002 [P] Create src/Shared/RbacSso.Common/ project with Exceptions/, Responses/, Extensions/ folders
- [X] T003 [P] Create src/Shared/RbacSso.Security/ project with Authentication/, Authorization/ folders
- [X] T004 [P] Create src/Shared/RbacSso.Tenant/ project with ITenantContext.cs and TenantMiddleware.cs stubs
- [X] T005 [P] Create src/Shared/RbacSso.Audit/ project with Domain/ folder and IAuditLogRepository.cs interface
- [X] T006 [P] Create src/Services/RbacSso.Gateway/ project with YARP 2.x dependency
- [X] T007 [P] Create src/Services/RbacSso.ProductService/ project with Hexagonal layers (Domain/Application/Infrastructure/Api)
- [X] T008 [P] Create src/Services/RbacSso.UserService/ project with Hexagonal layers
- [X] T009 [P] Create src/Services/RbacSso.AuditService/ project with Hexagonal layers
- [X] T010 [P] Create tests/RbacSso.ArchitectureTests/ project with xUnit and NetArchTest dependencies
- [X] T011 [P] Create deploy/docker/docker-compose.yml with PostgreSQL, Keycloak, OpenLDAP services
- [X] T012 Configure project references: Services → Shared libraries

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Domain Common Infrastructure

- [ ] T013 [P] Implement IDomainEvent interface in src/Shared/RbacSso.Common/Domain/IDomainEvent.cs
- [ ] T014 [P] Implement DomainEventBase record in src/Shared/RbacSso.Common/Domain/DomainEventBase.cs
- [ ] T015 [P] Implement AggregateRoot<TId> base class in src/Shared/RbacSso.Common/Domain/AggregateRoot.cs
- [ ] T016 [P] Implement DomainException base class in src/Shared/RbacSso.Common/Exceptions/DomainException.cs
- [ ] T017 [P] Implement ApiResponse and ErrorResponse DTOs in src/Shared/RbacSso.Common/Responses/

### Security & Multi-Tenancy

- [ ] T018 [P] Implement ITenantContext interface in src/Shared/RbacSso.Tenant/ITenantContext.cs
- [ ] T019 [P] Implement TenantMiddleware in src/Shared/RbacSso.Tenant/TenantMiddleware.cs
- [ ] T020 [P] Implement JwtClaimsPrincipalParser in src/Shared/RbacSso.Security/Authentication/JwtClaimsPrincipalParser.cs
- [ ] T021 [P] Implement Roles constants class in src/Shared/RbacSso.Security/Authorization/Roles.cs
- [ ] T022 [P] Implement RbacAuthorizationHandler in src/Shared/RbacSso.Security/Authorization/RbacAuthorizationHandler.cs
- [ ] T023 Implement CorrelationIdMiddleware in src/Shared/RbacSso.Common/Middleware/CorrelationIdMiddleware.cs

### Gateway Setup

- [ ] T024 Configure YARP routes in src/Services/RbacSso.Gateway/yarp.json
- [ ] T025 Configure JWT Bearer authentication in src/Services/RbacSso.Gateway/Program.cs
- [ ] T026 Add rate limiting middleware in src/Services/RbacSso.Gateway/Program.cs

### Architecture Tests

- [ ] T027 [P] Implement Domain_Should_Not_Depend_On_Infrastructure test in tests/RbacSso.ArchitectureTests/LayerDependencyTests.cs
- [ ] T028 [P] Implement DomainEvents_Should_Implement_IDomainEvent test in tests/RbacSso.ArchitectureTests/DomainEventTests.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Single Sign-On Authentication (Priority: P0) 🎯 MVP

**Goal**: Enable users to log in via Keycloak SSO and receive valid JWT tokens

**Independent Test**: Verify login flow redirects to Keycloak, authenticates, and returns valid JWT with claims

### Tests for User Story 1 (TDD - Write First)

- [ ] T029 [P] [US1] Create BDD feature file tests/RbacSso.ScenarioTests/Features/Authentication.feature with login scenarios
- [ ] T030 [P] [US1] Create integration test for OAuth2 authorization code flow in tests/RbacSso.Gateway.IntegrationTests/AuthenticationTests.cs
- [ ] T031 [P] [US1] Create unit test for JWT claims parsing in tests/RbacSso.Security.UnitTests/JwtClaimsParserTests.cs

### Implementation for User Story 1

- [ ] T032 [US1] Implement AuthenticationController in src/Services/RbacSso.Gateway/Controllers/AuthenticationController.cs
- [ ] T033 [US1] Implement OAuth2 callback handler in src/Services/RbacSso.Gateway/Controllers/AuthenticationController.cs
- [ ] T034 [US1] Implement token refresh endpoint in src/Services/RbacSso.Gateway/Controllers/AuthenticationController.cs
- [ ] T035 [US1] Implement ICurrentUser interface and implementation in src/Shared/RbacSso.Security/Authentication/
- [ ] T036 [US1] Configure Keycloak realm and client in deploy/docker/keycloak/realm-export.json
- [ ] T037 [US1] Create step definitions for Authentication.feature in tests/RbacSso.ScenarioTests/Steps/AuthenticationSteps.cs

### LDAP Federation (FR-005)

- [ ] T037a [US1] Configure OpenLDAP user federation in Keycloak realm in deploy/docker/keycloak/realm-export.json
- [ ] T037b [US1] Create LDAP user mapper for tenant_id claim in deploy/docker/keycloak/realm-export.json
- [ ] T037c [US1] Create integration test for LDAP user sync in tests/RbacSso.Gateway.IntegrationTests/LdapFederationTests.cs

**Checkpoint**: Users can log in via SSO and receive valid JWT tokens

---

## Phase 4: User Story 2 - Role-Based Product Management (Priority: P0)

**Goal**: Enable CRUD operations on products with RBAC enforcement

**Independent Test**: Verify each role (ADMIN, TENANT_ADMIN, USER, VIEWER) has correct permissions per matrix

### Tests for User Story 2 (TDD - Write First)

- [ ] T038 [P] [US2] Create BDD feature file tests/RbacSso.ScenarioTests/Features/ProductManagement.feature
- [ ] T039 [P] [US2] Create BDD feature file tests/RbacSso.ScenarioTests/Features/Rbac.feature with permission scenarios
- [ ] T040 [P] [US2] Create unit tests for Product aggregate in tests/RbacSso.ProductService.UnitTests/Domain/ProductTests.cs
- [ ] T041 [P] [US2] Create unit tests for value objects in tests/RbacSso.ProductService.UnitTests/Domain/ValueObjectTests.cs
- [ ] T042 [P] [US2] Create contract tests for Product API in tests/RbacSso.ProductService.IntegrationTests/ProductApiContractTests.cs

### Domain Layer for User Story 2

- [ ] T043 [P] [US2] Implement ProductId value object in src/Services/RbacSso.ProductService/Domain/Products/ProductId.cs
- [ ] T044 [P] [US2] Implement ProductCode value object in src/Services/RbacSso.ProductService/Domain/Products/ProductCode.cs
- [ ] T045 [P] [US2] Implement Money value object in src/Services/RbacSso.ProductService/Domain/Common/Money.cs
- [ ] T046 [P] [US2] Implement ProductStatus enum in src/Services/RbacSso.ProductService/Domain/Products/ProductStatus.cs
- [ ] T047 [US2] Implement Product aggregate root in src/Services/RbacSso.ProductService/Domain/Products/Product.cs
- [ ] T048 [P] [US2] Implement ProductCreated domain event in src/Services/RbacSso.ProductService/Domain/Products/Events/ProductCreated.cs
- [ ] T049 [P] [US2] Implement ProductUpdated domain event in src/Services/RbacSso.ProductService/Domain/Products/Events/ProductUpdated.cs
- [ ] T050 [P] [US2] Implement ProductPriceChanged domain event in src/Services/RbacSso.ProductService/Domain/Products/Events/ProductPriceChanged.cs
- [ ] T051 [P] [US2] Implement ProductDeleted domain event in src/Services/RbacSso.ProductService/Domain/Products/Events/ProductDeleted.cs

### Application Layer for User Story 2

- [ ] T052 [P] [US2] Implement IProductRepository interface in src/Services/RbacSso.ProductService/Application/Common/Interfaces/IProductRepository.cs
- [ ] T053 [P] [US2] Implement CreateProductCommand and handler in src/Services/RbacSso.ProductService/Application/Products/Commands/
- [ ] T054 [P] [US2] Implement UpdateProductCommand and handler in src/Services/RbacSso.ProductService/Application/Products/Commands/
- [ ] T055 [P] [US2] Implement DeleteProductCommand and handler in src/Services/RbacSso.ProductService/Application/Products/Commands/
- [ ] T056 [P] [US2] Implement GetProductByIdQuery and handler in src/Services/RbacSso.ProductService/Application/Products/Queries/
- [ ] T057 [P] [US2] Implement ListProductsQuery and handler in src/Services/RbacSso.ProductService/Application/Products/Queries/

### Infrastructure Layer for User Story 2

- [ ] T058 [US2] Implement ProductDbContext in src/Services/RbacSso.ProductService/Infrastructure/Persistence/ProductDbContext.cs
- [ ] T059 [US2] Implement ProductConfiguration EF mapping in src/Services/RbacSso.ProductService/Infrastructure/Persistence/Configurations/ProductConfiguration.cs
- [ ] T060 [US2] Implement EfProductRepository in src/Services/RbacSso.ProductService/Infrastructure/Persistence/EfProductRepository.cs
- [ ] T061 [US2] Create initial database migration in src/Services/RbacSso.ProductService/Infrastructure/Persistence/Migrations/

### API Layer for User Story 2

- [ ] T062 [US2] Implement ProductCommandController in src/Services/RbacSso.ProductService/Api/Controllers/ProductCommandController.cs
- [ ] T063 [US2] Implement ProductQueryController in src/Services/RbacSso.ProductService/Api/Controllers/ProductQueryController.cs
- [ ] T064 [US2] Configure MediatR and DI in src/Services/RbacSso.ProductService/Api/Program.cs
- [ ] T065 [US2] Implement RBAC authorization policies for product endpoints in src/Services/RbacSso.ProductService/Api/Program.cs
- [ ] T066 [US2] Create step definitions for ProductManagement.feature in tests/RbacSso.ScenarioTests/Steps/ProductManagementSteps.cs
- [ ] T067 [US2] Create step definitions for Rbac.feature in tests/RbacSso.ScenarioTests/Steps/RbacSteps.cs

**Checkpoint**: Product CRUD works with proper RBAC enforcement

---

## Phase 5: User Story 3 - Multi-Tenant Data Isolation (Priority: P0)

**Goal**: Ensure tenant A cannot access tenant B's data

**Independent Test**: Create products in tenant A and B, verify cross-tenant access blocked

### Tests for User Story 3 (TDD - Write First)

- [ ] T068 [P] [US3] Create BDD feature file tests/RbacSso.ScenarioTests/Features/MultiTenant.feature
- [ ] T069 [P] [US3] Create integration tests for tenant isolation in tests/RbacSso.ProductService.IntegrationTests/TenantIsolationTests.cs
- [ ] T070 [P] [US3] Create unit tests for tenant context in tests/RbacSso.Tenant.UnitTests/TenantContextTests.cs

### Implementation for User Story 3

- [ ] T071 [US3] Implement ITenantEntity interface in src/Shared/RbacSso.Tenant/ITenantEntity.cs
- [ ] T072 [US3] Configure Finbuckle.MultiTenant in src/Services/RbacSso.ProductService/Api/Program.cs
- [ ] T073 [US3] Implement tenant claim resolution strategy in src/Shared/RbacSso.Tenant/ClaimTenantResolver.cs
- [ ] T074 [US3] Add tenant-aware query filter to ProductDbContext in src/Services/RbacSso.ProductService/Infrastructure/Persistence/ProductDbContext.cs
- [ ] T075 [US3] Implement admin tenant bypass logic in src/Shared/RbacSso.Tenant/TenantMiddleware.cs
- [ ] T076 [US3] Create step definitions for MultiTenant.feature in tests/RbacSso.ScenarioTests/Steps/MultiTenantSteps.cs
- [ ] T077 [US3] Configure test tenants in deploy/docker/keycloak/realm-export.json

**Checkpoint**: Complete tenant isolation verified

---

## Phase 6: User Story 4 - Complete Audit Trail (Priority: P1)

**Goal**: All significant operations generate audit log entries via domain events

**Independent Test**: Perform CRUD operations and verify audit logs contain complete context

### Tests for User Story 4 (TDD - Write First)

- [ ] T078 [P] [US4] Create BDD feature file tests/RbacSso.ScenarioTests/Features/AuditLogging.feature
- [ ] T079 [P] [US4] Create unit tests for AuditEventHandler in tests/RbacSso.AuditService.UnitTests/AuditEventHandlerTests.cs
- [ ] T080 [P] [US4] Create integration tests for audit API in tests/RbacSso.AuditService.IntegrationTests/AuditApiTests.cs

### Domain Layer for User Story 4

- [ ] T081 [P] [US4] Implement AuditLog entity in src/Shared/RbacSso.Audit/Domain/AuditLog.cs
- [ ] T082 [P] [US4] Implement AuditResult enum in src/Shared/RbacSso.Audit/Domain/AuditResult.cs

### Application Layer for User Story 4

- [ ] T083 [P] [US4] Implement ListAuditLogsQuery and handler in src/Services/RbacSso.AuditService/Application/Queries/
- [ ] T084 [P] [US4] Implement GetAuditLogByIdQuery and handler in src/Services/RbacSso.AuditService/Application/Queries/

### Infrastructure Layer for User Story 4

- [ ] T085 [US4] Implement AuditEventHandler (subscribes to all domain events) in src/Services/RbacSso.ProductService/Infrastructure/Audit/AuditEventHandler.cs
- [ ] T086 [US4] Implement AuditDbContext in src/Services/RbacSso.AuditService/Infrastructure/Persistence/AuditDbContext.cs
- [ ] T087 [US4] Implement EfAuditLogRepository in src/Services/RbacSso.AuditService/Infrastructure/Persistence/EfAuditLogRepository.cs
- [ ] T088 [US4] Create audit_logs table migration in src/Services/RbacSso.AuditService/Infrastructure/Persistence/Migrations/
- [ ] T089 [US4] Implement PermissionDeniedEventHandler for logging denied access in src/Shared/RbacSso.Security/Authorization/PermissionDeniedEventHandler.cs
- [ ] T089a [US4] Implement AuthEventHandler for login/logout audit events in src/Services/RbacSso.Gateway/Infrastructure/Audit/AuthEventHandler.cs
- [ ] T089b [US4] Publish LoginSucceeded/LoginFailed/LogoutCompleted events from AuthenticationController

### API Layer for User Story 4

- [ ] T090 [US4] Implement AuditController in src/Services/RbacSso.AuditService/Api/Controllers/AuditController.cs
- [ ] T091 [US4] Configure MediatR event publishing in ProductService to include audit handler
- [ ] T092 [US4] Create step definitions for AuditLogging.feature in tests/RbacSso.ScenarioTests/Steps/AuditLoggingSteps.cs

### Audit Log Retention (FR-020)

- [ ] T092a [US4] Create PostgreSQL scheduled job for 90-day audit log retention in deploy/scripts/audit-retention.sql
- [ ] T092b [US4] Configure pg_cron or application-level scheduler for retention job execution

**Checkpoint**: All product and auth operations generate complete audit logs with retention

---

## Phase 7: User Story 5 - Product Catalog Search (Priority: P1)

**Goal**: Enable pagination, filtering, and sorting of product listings

**Independent Test**: Create products, verify pagination/filter/sort work correctly

### Tests for User Story 5 (TDD - Write First)

- [ ] T093 [P] [US5] Create integration tests for search/filter in tests/RbacSso.ProductService.IntegrationTests/ProductSearchTests.cs
- [ ] T094 [P] [US5] Create unit tests for query handlers in tests/RbacSso.ProductService.UnitTests/Application/ListProductsQueryTests.cs

### Implementation for User Story 5

- [ ] T095 [US5] Enhance ListProductsQuery with pagination parameters in src/Services/RbacSso.ProductService/Application/Products/Queries/ListProductsQuery.cs
- [ ] T096 [US5] Implement category filter in ListProductsQueryHandler
- [ ] T097 [US5] Implement sorting (name, price, createdAt) in ListProductsQueryHandler
- [ ] T098 [US5] Add database indexes for search performance in src/Services/RbacSso.ProductService/Infrastructure/Persistence/Configurations/ProductConfiguration.cs
- [ ] T099 [US5] Update ProductQueryController with filter/sort parameters

**Checkpoint**: Product search with pagination, filtering, sorting works

---

## Phase 8: User Story 6 - Secure Service Communication (Priority: P2)

**Goal**: Services communicate via mTLS with mutual certificate verification

**Independent Test**: Verify services reject connections without valid client certificates

### Tests for User Story 6 (TDD - Write First)

- [ ] T100 [P] [US6] Create integration tests for mTLS in tests/RbacSso.Gateway.IntegrationTests/MtlsTests.cs

### Implementation for User Story 6

- [ ] T101 [US6] Configure Kestrel mTLS in src/Services/RbacSso.ProductService/Api/Program.cs
- [ ] T102 [US6] Configure Kestrel mTLS in src/Services/RbacSso.UserService/Api/Program.cs
- [ ] T103 [US6] Configure Kestrel mTLS in src/Services/RbacSso.AuditService/Api/Program.cs
- [ ] T104 [US6] Configure HttpClient with client certificate in src/Services/RbacSso.Gateway/Program.cs
- [ ] T105 [US6] Create certificate generation scripts in deploy/scripts/generate-certs.sh
- [ ] T106 [US6] Add cert-manager configuration in deploy/k8s/base/cert-manager.yaml

**Checkpoint**: All service-to-service communication uses mTLS

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements across all stories

- [ ] T107 [P] Create UserController for /users/me endpoint in src/Services/RbacSso.UserService/Api/Controllers/UserController.cs
- [ ] T108 [P] Implement GlobalExceptionHandler middleware in src/Shared/RbacSso.Common/Middleware/GlobalExceptionHandler.cs
- [ ] T109 [P] Configure Swagger/OpenAPI in all service Program.cs files
- [ ] T110 [P] Create Kubernetes deployment manifests in deploy/k8s/base/
- [ ] T111 Run all BDD scenarios and verify 100% pass rate
- [ ] T112 Run coverage report and verify ≥80% line coverage
- [ ] T113 Run architecture tests and verify all pass
- [ ] T114 Validate quickstart.md steps work end-to-end

### Performance & Security Validation

- [ ] T115 [SC-006] Create load test script with k6 for 100 concurrent users in tests/load/k6-load-test.js
- [ ] T116 [SC-006] Run load test and verify no degradation at 100 concurrent users
- [ ] T117 [SC-009] Run OWASP ZAP security scan against all API endpoints
- [ ] T118 [SC-009] Address any high/critical findings from security scan

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup) → Phase 2 (Foundational) → [User Stories in parallel] → Phase 9 (Polish)
                                          ├─ Phase 3 (US1: SSO) ────────┐
                                          ├─ Phase 4 (US2: Products) ──┼─┐
                                          ├─ Phase 5 (US3: Tenancy) ───┘ │
                                          ├─ Phase 6 (US4: Audit) ───────┘
                                          ├─ Phase 7 (US5: Search) ──────
                                          └─ Phase 8 (US6: mTLS) ────────
```

### User Story Dependencies

| Story | Depends On | Reason |
|-------|------------|--------|
| US1 (SSO) | Foundational | JWT infrastructure needed |
| US2 (Products) | US1 | Requires authentication to test RBAC |
| US3 (Tenancy) | US2 | Requires products to test isolation |
| US4 (Audit) | US2 | Requires product operations to audit |
| US5 (Search) | US2 | Extends product listing functionality |
| US6 (mTLS) | Foundational | Independent infrastructure |

### Parallel Opportunities

**Phase 1 (Setup)**: T002-T011 can run in parallel (different project folders)

**Phase 2 (Foundational)**: T013-T017, T018-T022, T027-T028 can run in parallel

**Phase 4 (US2)**: Domain layer tasks T043-T051 can run in parallel

**Phase 4 (US2)**: Application layer tasks T053-T057 can run in parallel

---

## Parallel Example: User Story 2

```bash
# Launch all domain layer tasks in parallel:
Task: T043 "Implement ProductId value object"
Task: T044 "Implement ProductCode value object"
Task: T045 "Implement Money value object"
Task: T046 "Implement ProductStatus enum"
Task: T048-T051 "Implement domain events"

# After domain layer complete, launch application layer in parallel:
Task: T053 "CreateProductCommand and handler"
Task: T054 "UpdateProductCommand and handler"
Task: T055 "DeleteProductCommand and handler"
Task: T056 "GetProductByIdQuery and handler"
Task: T057 "ListProductsQuery and handler"
```

---

## Implementation Strategy

### MVP First (US1 + US2 + US3)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: US1 (SSO) - Can now authenticate
4. Complete Phase 4: US2 (Products) - Can now CRUD with RBAC
5. Complete Phase 5: US3 (Tenancy) - Data isolation verified
6. **STOP and VALIDATE**: Core platform functional with auth, RBAC, multi-tenancy
7. Deploy/demo MVP

### Incremental Delivery

| Increment | Stories Included | Business Value |
|-----------|------------------|----------------|
| MVP | US1 + US2 + US3 | Secure multi-tenant product management |
| +Audit | + US4 | Compliance and investigation capability |
| +Search | + US5 | Enhanced user productivity |
| +Security | + US6 | Production-ready security |

### Parallel Team Strategy

With 3 developers after Foundational phase:
- **Dev A**: US1 (SSO) → US4 (Audit)
- **Dev B**: US2 (Products) → US5 (Search)
- **Dev C**: US3 (Tenancy) → US6 (mTLS)

---

## Notes

- [P] tasks = different files, no dependencies
- [USn] label maps task to specific user story
- Each user story should be independently completable and testable
- TDD: Write tests first, verify they fail, then implement
- Commit after each task or logical group
- BDD scenarios in zh-TW with English step annotations per constitution
- Error codes follow format: `{SVC}-{CAT}{SEQ}` (e.g., `PRD-B00001`)
