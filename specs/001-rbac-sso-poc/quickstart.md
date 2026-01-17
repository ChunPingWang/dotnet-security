# Quickstart: RBAC-SSO Multi-Tenant E-Commerce POC

**Date**: 2026-01-17
**Feature**: 001-rbac-sso-poc

## Prerequisites

- .NET 8.0 SDK
- Docker Desktop (for PostgreSQL, Keycloak, OpenLDAP)
- IDE: Visual Studio 2022 / JetBrains Rider / VS Code with C# extension

## Quick Setup

### 1. Clone and Build

```bash
# Clone repository
git clone <repository-url>
cd rbac-sso-poc-dotnet

# Restore and build
dotnet restore
dotnet build
```

### 2. Start Infrastructure

```bash
# Start PostgreSQL, Keycloak, OpenLDAP
cd deploy/docker
docker-compose up -d

# Wait for services to be healthy
docker-compose ps
```

### 3. Run Migrations

```bash
# Apply database migrations
cd src/Services/RbacSso.ProductService
dotnet ef database update
```

### 4. Start Services

```bash
# Terminal 1: Gateway (port 8080)
cd src/Services/RbacSso.Gateway
dotnet run

# Terminal 2: Product Service (port 8081)
cd src/Services/RbacSso.ProductService
dotnet run

# Terminal 3: User Service (port 8082)
cd src/Services/RbacSso.UserService
dotnet run

# Terminal 4: Audit Service (port 8083)
cd src/Services/RbacSso.AuditService
dotnet run
```

## Verification Steps

### 1. Verify Infrastructure

```bash
# PostgreSQL
psql -h localhost -U postgres -d rbac_sso -c "SELECT 1"

# Keycloak Admin Console
open http://localhost:8180/admin
# Login: admin / admin

# OpenLDAP (optional)
ldapsearch -x -H ldap://localhost:389 -D "cn=admin,dc=example,dc=org" -w admin -b "dc=example,dc=org"
```

### 2. Get Access Token

```bash
# Get token for admin user
TOKEN=$(curl -s -X POST "http://localhost:8180/realms/ecommerce/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=gateway" \
  -d "client_secret=gateway-secret" \
  -d "username=admin" \
  -d "password=admin" | jq -r '.access_token')

echo $TOKEN
```

### 3. Test Product API

```bash
# Create product
curl -X POST "http://localhost:8080/api/products" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Product",
    "price": 99.99,
    "category": "Electronics",
    "description": "A test product"
  }'

# List products
curl -X GET "http://localhost:8080/api/products" \
  -H "Authorization: Bearer $TOKEN"

# Get product by ID (replace {id} with actual ID)
curl -X GET "http://localhost:8080/api/products/{id}" \
  -H "Authorization: Bearer $TOKEN"
```

### 4. Test Multi-Tenancy

```bash
# Get token for tenant-a user
TOKEN_A=$(curl -s -X POST "http://localhost:8180/realms/ecommerce/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=gateway" \
  -d "client_secret=gateway-secret" \
  -d "username=tenant-a-admin" \
  -d "password=password" | jq -r '.access_token')

# Get token for tenant-b user
TOKEN_B=$(curl -s -X POST "http://localhost:8180/realms/ecommerce/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=gateway" \
  -d "client_secret=gateway-secret" \
  -d "username=tenant-b-admin" \
  -d "password=password" | jq -r '.access_token')

# Create product as tenant-a
curl -X POST "http://localhost:8080/api/products" \
  -H "Authorization: Bearer $TOKEN_A" \
  -H "Content-Type: application/json" \
  -d '{"name": "Tenant A Product", "price": 100, "category": "Test"}'

# List products as tenant-b (should not see tenant-a products)
curl -X GET "http://localhost:8080/api/products" \
  -H "Authorization: Bearer $TOKEN_B"
```

### 5. Test RBAC

```bash
# Get token for viewer user
TOKEN_VIEWER=$(curl -s -X POST "http://localhost:8180/realms/ecommerce/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=gateway" \
  -d "client_secret=gateway-secret" \
  -d "username=viewer" \
  -d "password=password" | jq -r '.access_token')

# Try to create product as viewer (should fail with 403)
curl -X POST "http://localhost:8080/api/products" \
  -H "Authorization: Bearer $TOKEN_VIEWER" \
  -H "Content-Type: application/json" \
  -d '{"name": "Forbidden Product", "price": 50, "category": "Test"}'
# Expected: 403 Forbidden
```

### 6. Verify Audit Logs

```bash
# List audit logs (as admin)
curl -X GET "http://localhost:8080/api/audit/logs" \
  -H "Authorization: Bearer $TOKEN"

# Filter by event type
curl -X GET "http://localhost:8080/api/audit/logs?eventType=PRODUCT_CREATED" \
  -H "Authorization: Bearer $TOKEN"
```

## Running Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test --filter Category=Unit

# Integration tests only
dotnet test --filter Category=Integration

# BDD tests only
dotnet test --filter Category=BDD

# Architecture tests
dotnet test --filter Category=Architecture

# With coverage report
dotnet test --collect:"XPlat Code Coverage"
```

## Swagger UI

After starting services:
- Gateway: http://localhost:8080/swagger
- Product Service: http://localhost:8081/swagger
- User Service: http://localhost:8082/swagger
- Audit Service: http://localhost:8083/swagger

## Common Issues

### Token Expired
```bash
# Refresh token or request new one
TOKEN=$(curl -s -X POST "http://localhost:8180/realms/ecommerce/protocol/openid-connect/token" ...)
```

### Database Connection Failed
```bash
# Check PostgreSQL is running
docker-compose ps postgres
docker-compose logs postgres
```

### Keycloak Not Ready
```bash
# Check Keycloak health
curl http://localhost:8180/health/ready

# Wait and retry (Keycloak can take 30-60 seconds to start)
```

### Port Already in Use
```bash
# Find and kill process using port
lsof -i :8080
kill -9 <PID>
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| ASPNETCORE_ENVIRONMENT | Development | Environment name |
| ConnectionStrings__Default | (in appsettings) | PostgreSQL connection |
| Keycloak__Authority | http://localhost:8180/realms/ecommerce | Keycloak realm URL |
| Keycloak__ClientId | gateway | OIDC client ID |
| Keycloak__ClientSecret | gateway-secret | OIDC client secret |
