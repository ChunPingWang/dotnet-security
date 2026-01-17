<!--
================================================================================
SYNC IMPACT REPORT
================================================================================
Source Document: /constitution.md (Java/Spring v2.0, 2026-01-04)
Adaptation: .NET 8.0 LTS for RBAC-SSO Microservices project

Version Change: 1.1.0 → 1.2.0
Bump Rationale: .NET-specific adaptation of original Java/Spring constitution

Principles Retained (adapted for .NET):
  - I. Domain-Driven Design (DDD) - unchanged
  - II. SOLID Principles - unchanged
  - III. Hexagonal Architecture - adapted: Spring → ASP.NET Core, JPA → EF Core
  - IV. TDD/BDD - adapted: JUnit → xUnit, Mockito → Moq, Cucumber → Reqnroll
  - V. Simplicity (YAGNI) - unchanged

Technical Standards Adapted:
  - Exception Handling: Same error code pattern, C# implementation
  - API Design: Same Richardson L2, C# response format
  - Logging: MDC → Serilog enrichers, ILogger<T>
  - CI/CD: Gradle → dotnet, JaCoCo → Coverlet, Cucumber → Reqnroll

Original Java Elements NOT Included:
  - Java package naming (com.bank.discount.*)
  - Spring annotations (@Service, @Component, @Repository)
  - JPA/Hibernate annotations (@Entity, @Table)
  - Gradle build commands
  - Java-specific code examples

Templates Requiring Updates:
  - .specify/templates/plan-template.md: ✅ No changes needed (generic structure)
  - .specify/templates/spec-template.md: ✅ No changes needed (generic structure)
  - .specify/templates/tasks-template.md: ✅ No changes needed (generic structure)

Follow-up TODOs: None
================================================================================
-->

# RBAC-SSO Microservices Constitution

## Core Principles

### I. Domain-Driven Design (DDD)

The project MUST follow Domain-Driven Design principles as the foundational approach.

- All features MUST start with domain modeling using ubiquitous language
- Entities, Value Objects, Domain Services, and Domain Events MUST be clearly distinguished
- Bounded contexts MUST be explicitly defined and respected
- Domain layer MUST remain free of infrastructure concerns
- Aggregates MUST enforce invariants and emit domain events for state changes

**Rationale**: DDD ensures business logic is accurately captured and maintained, preventing technology-driven designs that diverge from business needs.

### II. SOLID Principles

All object-oriented code MUST adhere to SOLID principles.

- **Single Responsibility**: Each class MUST have exactly one reason to change
- **Open-Closed**: Classes MUST be open for extension, closed for modification
- **Liskov Substitution**: Subtypes MUST be substitutable for their base types
- **Interface Segregation**: Clients MUST NOT depend on interfaces they don't use
- **Dependency Inversion**: High-level modules MUST NOT depend on low-level modules; both MUST depend on abstractions

**Rationale**: SOLID principles ensure maintainable, testable, and extensible code that can evolve with changing requirements.

### III. Hexagonal Architecture (Ports & Adapters)

The system MUST be structured using Hexagonal Architecture with strict layer separation.

- **Domain Layer**: Pure C# classes with NO framework dependencies
  - NO EF Core attributes (`[Key]`, `[Required]`, `[Table]`, etc.)
  - NO ASP.NET attributes (`[ApiController]`, `[HttpGet]`, etc.)
  - NO MediatR attributes or base classes in domain entities
  - Value Objects and Entities MUST be immutable or have controlled mutability
- **Application Layer**: Use case orchestration via MediatR handlers, port interface definitions
- **Infrastructure Layer**: EF Core DbContext, repositories, external service clients

**Dependency Rules**:
| Direction | Allowed |
|-----------|---------|
| Infrastructure → Application | ✅ |
| Application → Domain | ✅ |
| Application → Infrastructure | ❌ |
| Domain → Application | ❌ |
| Domain → Infrastructure | ❌ |

**Verification**: NetArchTest MUST enforce these rules in CI pipeline.

**Rationale**: Hexagonal architecture isolates business logic from technical concerns, enabling technology changes without business logic rewrites.

### IV. Test-Driven Development (TDD/BDD) - NON-NEGOTIABLE

All features MUST be developed using TDD/BDD methodology.

- **TDD Cycle**: Red → Green → Refactor (strictly enforced)
- **BDD Tests**: Feature files MUST use Gherkin syntax (zh-TW content permitted)
- **Step Definitions**: MUST use Reqnroll `[Given]`, `[When]`, `[Then]` attributes
- **Coverage Requirements**: Line coverage MUST be ≥ 80%

**Test Levels**:
| Type | Scope | Framework | Location |
|------|-------|-----------|----------|
| Unit Tests | Single class/method | xUnit + Moq | `tests/*.UnitTests/` |
| Integration Tests | Multi-component | WebApplicationFactory + Testcontainers | `tests/*.IntegrationTests/` |
| BDD Tests | Business scenarios | Reqnroll | `tests/*.ScenarioTests/` |
| Architecture Tests | Layer dependencies | NetArchTest | `tests/*.ArchitectureTests/` |

**Test Naming Convention**: `MethodName_StateUnderTest_ExpectedBehavior`

**Rationale**: TDD ensures correctness from the start and creates living documentation. Tests written after implementation miss edge cases and design feedback.

### V. Simplicity (YAGNI)

Code MUST be as simple as possible while meeting requirements.

- NO speculative features or "just in case" abstractions
- NO premature optimization
- Complexity MUST be justified in writing (Complexity Tracking table in plan.md)
- Prefer direct solutions over patterns unless patterns solve a real problem
- Microservices MUST be justified; default to modular monolith

**Rationale**: Unnecessary complexity increases maintenance burden, introduces bugs, and slows development without adding value.

## Technical Standards

### .NET Version & Dependencies

- **Target Framework**: .NET 8.0 LTS (C# 12)
- **Core Libraries**:
  - ASP.NET Core 8.0 for web APIs
  - Entity Framework Core 8.0 for data access
  - MediatR 12.x for CQRS and domain events
  - FluentValidation for input validation
  - Serilog for structured logging

### Exception Handling

Error codes MUST follow the pattern: `{COMPONENT}-{CATEGORY}{SEQUENCE}`

| Field | Length | Description |
|-------|--------|-------------|
| Component | 3 | Service code (cross-component: `STD`) |
| Separator | 1 | Fixed `-` |
| Category | 1 | B=Business, S=System, V=Validation |
| Sequence | 5 | Starting from 00001 |

Domain exceptions MUST extend a common `DomainException` base class:

```csharp
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    protected DomainException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
```

### API Design

APIs MUST follow Richardson Maturity Model Level 2:

- Correct HTTP methods (GET, POST, PUT, DELETE)
- Meaningful URL paths: `/api/{resource}/{id}/{sub-resource}`
- Appropriate HTTP status codes (200, 201, 400, 401, 403, 404, 500)

**Standard Response Format**:
```csharp
public record ApiResponse<T>(
    bool Success,
    string Code,
    string Message,
    T? Data,
    DateTime Timestamp,
    string RequestId
);
```

### Logging

- MUST use `ILogger<T>` with structured logging
- MUST use Serilog enrichers for request context (CorrelationId, UserId, TenantId)
- NEVER use string concatenation for log messages
- Log levels:
  - **Error**: Unexpected failures requiring investigation
  - **Warning**: Potential issues or degraded operations
  - **Information**: Business events and operational milestones
  - **Debug**: Diagnostic information for development
  - **Verbose**: Detailed tracing (disabled in production)

### Data Access

- Repository pattern MUST be used for all data access
- EF Core configurations MUST use Fluent API (not attributes)
- Global query filters MUST be applied for soft delete and multi-tenancy
- Database migrations MUST be code-first and idempotent

## Development Workflow

### Version Control

**Branch Strategy**:
```
main (production)
  └── release/X.Y.Z (UAT)
        └── develop (integration)
              ├── feature/XXX-description
              └── bugfix/XXX-description
```

**Commit Format**:
```
<type>(<scope>): <subject>

<body>

<footer>
```

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`

### CI/CD Quality Gates

All PRs MUST pass these quality gates:

| Check | Tool | Standard |
|-------|------|----------|
| Compilation | `dotnet build` | No errors, no warnings |
| Unit Tests | xUnit | 100% pass |
| Integration Tests | xUnit + Testcontainers | 100% pass |
| Coverage | Coverlet | ≥ 80% line coverage |
| Code Quality | `dotnet format` | No violations |
| Security | `dotnet list package --vulnerable` | No known vulnerabilities |
| BDD Tests | Reqnroll | 100% pass |
| Architecture | NetArchTest | 100% pass |

### Code Review Checklist

- [ ] Constitution principles adhered to
- [ ] Tests written before implementation (TDD)
- [ ] No framework dependencies in Domain layer
- [ ] Error codes follow standard format
- [ ] Structured logging used correctly
- [ ] No hardcoded secrets or connection strings

## Governance

This Constitution supersedes all other development practices and guidelines.

**Amendment Process**:
1. Proposed changes MUST be documented with rationale
2. Changes MUST be reviewed by technical leads
3. Breaking changes MUST include migration plan
4. All PRs MUST verify compliance with current Constitution

**Compliance Review**:
- All code reviews MUST verify Constitution compliance
- Violations MUST be resolved before merge
- Complexity deviations MUST be documented in plan.md Complexity Tracking table

**Version Policy**: This Constitution follows semantic versioning:
- MAJOR: Backward-incompatible principle changes
- MINOR: New principles or expanded guidance
- PATCH: Clarifications and non-semantic fixes

**Version**: 1.2.0 | **Ratified**: 2026-01-17 | **Last Amended**: 2026-01-17
