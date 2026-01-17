# INFRA.md - 基礎設施文件

## RBAC-SSO-POC (.NET Core 版)

**版本**: 1.0  
**日期**: 2025-01-11  
**稽核機制**: Domain Events

---

## 1. 基礎設施概覽

### 1.1 部署架構

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              Kubernetes Cluster                                  │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                         Namespace: rbac-sso                              │   │
│  │                                                                          │   │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐       │   │
│  │  │  Gateway (YARP)  │  │ Product Service  │  │  User Service    │       │   │
│  │  │  Replicas: 2     │  │  Replicas: 2     │  │  Replicas: 2     │       │   │
│  │  │  Port: 8080      │  │  Port: 8081      │  │  Port: 8082      │       │   │
│  │  └────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘       │   │
│  │           │                     │                     │                  │   │
│  │           │                     │                     │                  │   │
│  │           └─────────────────────┼─────────────────────┘                  │   │
│  │                                 │                                        │   │
│  │                                 ▼                                        │   │
│  │  ┌──────────────────────────────────────────────────────────────────┐   │   │
│  │  │                     PostgreSQL StatefulSet                        │   │   │
│  │  │                     Replicas: 1                                   │   │   │
│  │  │                     Port: 5432                                    │   │   │
│  │  │                     PVC: 10Gi                                     │   │   │
│  │  └──────────────────────────────────────────────────────────────────┘   │   │
│  │                                                                          │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                         Namespace: keycloak                              │   │
│  │                                                                          │   │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐       │   │
│  │  │    Keycloak      │  │   PostgreSQL     │  │    OpenLDAP      │       │   │
│  │  │  Replicas: 1     │  │  (for Keycloak)  │  │  Replicas: 1     │       │   │
│  │  │  Port: 8180      │  │  Port: 5432      │  │  Port: 389/636   │       │   │
│  │  └──────────────────┘  └──────────────────┘  └──────────────────┘       │   │
│  │                                                                          │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                         Namespace: cert-manager                          │   │
│  │                                                                          │   │
│  │  ┌──────────────────┐  ┌──────────────────┐                             │   │
│  │  │  cert-manager    │  │   CA Issuer      │                             │   │
│  │  │  Controller      │  │   (Self-Signed)  │                             │   │
│  │  └──────────────────┘  └──────────────────┘                             │   │
│  │                                                                          │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 服務清單

| 服務 | 端口 | 說明 | Replicas |
|------|------|------|----------|
| Gateway (YARP) | 8080 | API 閘道 | 2 |
| Product Service | 8081 | 商品管理服務 | 2 |
| User Service | 8082 | 使用者服務 | 2 |
| Audit Service | 8083 | 稽核服務 | 2 |
| Keycloak | 8180 | SSO/OAuth2 認證 | 1 |
| PostgreSQL (App) | 5432 | 應用資料庫 | 1 |
| PostgreSQL (KC) | 5433 | Keycloak 資料庫 | 1 |
| OpenLDAP | 389/636 | 使用者目錄 | 1 |

---

## 2. Docker 部署

### 2.1 Docker Compose

```yaml
# deploy/docker/docker-compose.yml
version: '3.8'

services:
  # ===== API Gateway =====
  gateway:
    build:
      context: ../../
      dockerfile: src/Services/RbacSso.Gateway/Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - Keycloak__Authority=http://keycloak:8080/realms/ecommerce
      - ReverseProxy__Clusters__productCluster__Destinations__destination1__Address=http://product-service:8081
      - ReverseProxy__Clusters__userCluster__Destinations__destination1__Address=http://user-service:8082
    depends_on:
      - keycloak
      - product-service
      - user-service
    networks:
      - rbac-network

  # ===== Product Service =====
  product-service:
    build:
      context: ../../
      dockerfile: src/Services/RbacSso.ProductService/Dockerfile
    ports:
      - "8081:8081"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8081
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=rbac_sso;Username=postgres;Password=postgres
      - Keycloak__Authority=http://keycloak:8080/realms/ecommerce
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - rbac-network

  # ===== User Service =====
  user-service:
    build:
      context: ../../
      dockerfile: src/Services/RbacSso.UserService/Dockerfile
    ports:
      - "8082:8082"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8082
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=rbac_sso;Username=postgres;Password=postgres
      - Keycloak__Authority=http://keycloak:8080/realms/ecommerce
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - rbac-network

  # ===== PostgreSQL =====
  postgres:
    image: postgres:15-alpine
    ports:
      - "5432:5432"
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres
      - POSTGRES_DB=rbac_sso
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./init-db.sql:/docker-entrypoint-initdb.d/init-db.sql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5
    networks:
      - rbac-network

  # ===== Keycloak =====
  keycloak:
    image: quay.io/keycloak/keycloak:24.0
    command: start-dev --import-realm
    ports:
      - "8180:8080"
    environment:
      - KEYCLOAK_ADMIN=admin
      - KEYCLOAK_ADMIN_PASSWORD=admin
      - KC_DB=postgres
      - KC_DB_URL=jdbc:postgresql://postgres-keycloak:5432/keycloak
      - KC_DB_USERNAME=keycloak
      - KC_DB_PASSWORD=keycloak
    volumes:
      - ./keycloak/realm-export.json:/opt/keycloak/data/import/realm-export.json
    depends_on:
      postgres-keycloak:
        condition: service_healthy
    networks:
      - rbac-network

  # ===== PostgreSQL for Keycloak =====
  postgres-keycloak:
    image: postgres:15-alpine
    ports:
      - "5433:5432"
    environment:
      - POSTGRES_USER=keycloak
      - POSTGRES_PASSWORD=keycloak
      - POSTGRES_DB=keycloak
    volumes:
      - postgres_keycloak_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U keycloak"]
      interval: 5s
      timeout: 5s
      retries: 5
    networks:
      - rbac-network

  # ===== OpenLDAP =====
  openldap:
    image: osixia/openldap:1.5.0
    ports:
      - "389:389"
      - "636:636"
    environment:
      - LDAP_ORGANISATION=Example Inc
      - LDAP_DOMAIN=example.com
      - LDAP_ADMIN_PASSWORD=admin
    volumes:
      - ldap_data:/var/lib/ldap
      - ldap_config:/etc/ldap/slapd.d
      - ./ldap/bootstrap.ldif:/container/service/slapd/assets/config/bootstrap/ldif/custom/bootstrap.ldif
    networks:
      - rbac-network

  # ===== phpLDAPadmin =====
  phpldapadmin:
    image: osixia/phpldapadmin:0.9.0
    ports:
      - "8181:80"
    environment:
      - PHPLDAPADMIN_LDAP_HOSTS=openldap
    depends_on:
      - openldap
    networks:
      - rbac-network

volumes:
  postgres_data:
  postgres_keycloak_data:
  ldap_data:
  ldap_config:

networks:
  rbac-network:
    driver: bridge
```

### 2.2 Dockerfile (.NET Core)

```dockerfile
# src/Services/RbacSso.ProductService/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["RbacSso.sln", "./"]
COPY ["src/Services/RbacSso.ProductService/RbacSso.ProductService.csproj", "src/Services/RbacSso.ProductService/"]
COPY ["src/Shared/RbacSso.Common/RbacSso.Common.csproj", "src/Shared/RbacSso.Common/"]
COPY ["src/Shared/RbacSso.Security/RbacSso.Security.csproj", "src/Shared/RbacSso.Security/"]
COPY ["src/Shared/RbacSso.Tenant/RbacSso.Tenant.csproj", "src/Shared/RbacSso.Tenant/"]
COPY ["src/Shared/RbacSso.Audit/RbacSso.Audit.csproj", "src/Shared/RbacSso.Audit/"]

# Restore
RUN dotnet restore "src/Services/RbacSso.ProductService/RbacSso.ProductService.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/src/Services/RbacSso.ProductService"
RUN dotnet build "RbacSso.ProductService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RbacSso.ProductService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "RbacSso.ProductService.dll"]
```

### 2.3 Docker 部署腳本

```bash
#!/bin/bash
# deploy/scripts/docker-deploy.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOCKER_DIR="$SCRIPT_DIR/../docker"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}=== RBAC-SSO-POC Docker Deployment ===${NC}"

# Build option
if [[ "$1" == "--build" ]]; then
    echo -e "${YELLOW}Building Docker images...${NC}"
    docker compose -f "$DOCKER_DIR/docker-compose.yml" build
fi

# Start services
echo -e "${YELLOW}Starting services...${NC}"
docker compose -f "$DOCKER_DIR/docker-compose.yml" up -d

# Wait for services
echo -e "${YELLOW}Waiting for services to be healthy...${NC}"
sleep 30

# Health check
echo -e "${YELLOW}Running health checks...${NC}"

services=("gateway:8080" "product-service:8081" "user-service:8082" "keycloak:8180")
for service in "${services[@]}"; do
    name="${service%%:*}"
    port="${service##*:}"
    
    if curl -s "http://localhost:$port/health" > /dev/null 2>&1; then
        echo -e "${GREEN}✓ $name is healthy${NC}"
    else
        echo -e "${YELLOW}⚠ $name health check pending${NC}"
    fi
done

echo -e "${GREEN}=== Deployment Complete ===${NC}"
echo ""
echo "Services:"
echo "  Gateway:         http://localhost:8080"
echo "  Product Service: http://localhost:8081"
echo "  User Service:    http://localhost:8082"
echo "  Keycloak:        http://localhost:8180"
echo "  phpLDAPadmin:    http://localhost:8181"
```

---

## 3. Kubernetes 部署

### 3.1 Namespace

```yaml
# deploy/k8s/base/namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: rbac-sso
  labels:
    app.kubernetes.io/name: rbac-sso
    app.kubernetes.io/component: namespace
```

### 3.2 ConfigMap

```yaml
# deploy/k8s/base/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: rbac-sso-config
  namespace: rbac-sso
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  Keycloak__Authority: "http://keycloak.keycloak.svc.cluster.local:8080/realms/ecommerce"
  Keycloak__Audience: "gateway"
  Logging__LogLevel__Default: "Information"
```

### 3.3 Secret

```yaml
# deploy/k8s/base/secret.yaml
apiVersion: v1
kind: Secret
metadata:
  name: rbac-sso-secret
  namespace: rbac-sso
type: Opaque
stringData:
  ConnectionStrings__DefaultConnection: "Host=postgres.rbac-sso.svc.cluster.local;Database=rbac_sso;Username=postgres;Password=postgres"
  POSTGRES_PASSWORD: "postgres"
```

### 3.4 Product Service Deployment

```yaml
# deploy/k8s/base/product-service.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: product-service
  namespace: rbac-sso
  labels:
    app: product-service
spec:
  replicas: 2
  selector:
    matchLabels:
      app: product-service
  template:
    metadata:
      labels:
        app: product-service
    spec:
      containers:
      - name: product-service
        image: rbac-sso/product-service:latest
        ports:
        - containerPort: 8081
        envFrom:
        - configMapRef:
            name: rbac-sso-config
        - secretRef:
            name: rbac-sso-secret
        env:
        - name: ASPNETCORE_URLS
          value: "http://+:8081"
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8081
          initialDelaySeconds: 10
          periodSeconds: 5
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8081
          initialDelaySeconds: 15
          periodSeconds: 10
---
apiVersion: v1
kind: Service
metadata:
  name: product-service
  namespace: rbac-sso
spec:
  selector:
    app: product-service
  ports:
  - port: 8081
    targetPort: 8081
  type: ClusterIP
```

### 3.5 Gateway Deployment (YARP)

```yaml
# deploy/k8s/base/gateway.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: gateway
  namespace: rbac-sso
  labels:
    app: gateway
spec:
  replicas: 2
  selector:
    matchLabels:
      app: gateway
  template:
    metadata:
      labels:
        app: gateway
    spec:
      containers:
      - name: gateway
        image: rbac-sso/gateway:latest
        ports:
        - containerPort: 8080
        envFrom:
        - configMapRef:
            name: rbac-sso-config
        env:
        - name: ASPNETCORE_URLS
          value: "http://+:8080"
        - name: ReverseProxy__Clusters__productCluster__Destinations__destination1__Address
          value: "http://product-service.rbac-sso.svc.cluster.local:8081"
        - name: ReverseProxy__Clusters__userCluster__Destinations__destination1__Address
          value: "http://user-service.rbac-sso.svc.cluster.local:8082"
        resources:
          requests:
            memory: "128Mi"
            cpu: "50m"
          limits:
            memory: "256Mi"
            cpu: "250m"
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 10
---
apiVersion: v1
kind: Service
metadata:
  name: gateway
  namespace: rbac-sso
spec:
  selector:
    app: gateway
  ports:
  - port: 8080
    targetPort: 8080
    nodePort: 30080
  type: NodePort
```

### 3.6 PostgreSQL StatefulSet

```yaml
# deploy/k8s/base/postgres.yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres
  namespace: rbac-sso
spec:
  serviceName: postgres
  replicas: 1
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
      - name: postgres
        image: postgres:15-alpine
        ports:
        - containerPort: 5432
        env:
        - name: POSTGRES_USER
          value: "postgres"
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: rbac-sso-secret
              key: POSTGRES_PASSWORD
        - name: POSTGRES_DB
          value: "rbac_sso"
        volumeMounts:
        - name: postgres-data
          mountPath: /var/lib/postgresql/data
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
  volumeClaimTemplates:
  - metadata:
      name: postgres-data
    spec:
      accessModes: ["ReadWriteOnce"]
      resources:
        requests:
          storage: 10Gi
---
apiVersion: v1
kind: Service
metadata:
  name: postgres
  namespace: rbac-sso
spec:
  selector:
    app: postgres
  ports:
  - port: 5432
    targetPort: 5432
  clusterIP: None
```

---

## 4. mTLS 配置 (cert-manager)

### 4.1 安裝 cert-manager

```bash
#!/bin/bash
# deploy/scripts/install-cert-manager.sh

# Install cert-manager
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.14.0/cert-manager.yaml

# Wait for cert-manager to be ready
kubectl wait --for=condition=Available deployment/cert-manager -n cert-manager --timeout=300s
kubectl wait --for=condition=Available deployment/cert-manager-webhook -n cert-manager --timeout=300s
```

### 4.2 CA Issuer

```yaml
# deploy/k8s/security/cert-manager/ca-issuer.yaml
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: rbac-sso-selfsigned-issuer
spec:
  selfSigned: {}
---
apiVersion: cert-manager.io/v1
kind: Certificate
metadata:
  name: rbac-sso-ca
  namespace: rbac-sso
spec:
  isCA: true
  commonName: rbac-sso-ca
  secretName: rbac-sso-ca-secret
  privateKey:
    algorithm: ECDSA
    size: 256
  issuerRef:
    name: rbac-sso-selfsigned-issuer
    kind: ClusterIssuer
    group: cert-manager.io
---
apiVersion: cert-manager.io/v1
kind: Issuer
metadata:
  name: rbac-sso-ca-issuer
  namespace: rbac-sso
spec:
  ca:
    secretName: rbac-sso-ca-secret
```

### 4.3 Service Certificates

```yaml
# deploy/k8s/security/cert-manager/service-certificates.yaml
apiVersion: cert-manager.io/v1
kind: Certificate
metadata:
  name: gateway-tls
  namespace: rbac-sso
spec:
  secretName: gateway-tls-secret
  duration: 8760h    # 1 year
  renewBefore: 720h  # 30 days
  commonName: gateway
  privateKey:
    algorithm: ECDSA
    size: 256
  usages:
    - server auth
    - client auth
  dnsNames:
    - gateway
    - gateway.rbac-sso.svc.cluster.local
  issuerRef:
    name: rbac-sso-ca-issuer
    kind: Issuer
---
apiVersion: cert-manager.io/v1
kind: Certificate
metadata:
  name: product-service-tls
  namespace: rbac-sso
spec:
  secretName: product-service-tls-secret
  duration: 8760h
  renewBefore: 720h
  commonName: product-service
  privateKey:
    algorithm: ECDSA
    size: 256
  usages:
    - server auth
    - client auth
  dnsNames:
    - product-service
    - product-service.rbac-sso.svc.cluster.local
  issuerRef:
    name: rbac-sso-ca-issuer
    kind: Issuer
---
apiVersion: cert-manager.io/v1
kind: Certificate
metadata:
  name: user-service-tls
  namespace: rbac-sso
spec:
  secretName: user-service-tls-secret
  duration: 8760h
  renewBefore: 720h
  commonName: user-service
  privateKey:
    algorithm: ECDSA
    size: 256
  usages:
    - server auth
    - client auth
  dnsNames:
    - user-service
    - user-service.rbac-sso.svc.cluster.local
  issuerRef:
    name: rbac-sso-ca-issuer
    kind: Issuer
```

### 4.4 mTLS Deployment

```yaml
# deploy/k8s/services-mtls/product-service-mtls.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: product-service
  namespace: rbac-sso
spec:
  replicas: 2
  selector:
    matchLabels:
      app: product-service
  template:
    metadata:
      labels:
        app: product-service
    spec:
      containers:
      - name: product-service
        image: rbac-sso/product-service:latest
        ports:
        - containerPort: 8081
        env:
        - name: ASPNETCORE_URLS
          value: "https://+:8081"
        - name: ASPNETCORE_Kestrel__Certificates__Default__Path
          value: "/etc/ssl/certs/tls.crt"
        - name: ASPNETCORE_Kestrel__Certificates__Default__KeyPath
          value: "/etc/ssl/certs/tls.key"
        - name: Kestrel__EndpointDefaults__ClientCertificateMode
          value: "RequireCertificate"
        volumeMounts:
        - name: tls-certs
          mountPath: /etc/ssl/certs
          readOnly: true
        - name: ca-certs
          mountPath: /etc/ssl/ca
          readOnly: true
      volumes:
      - name: tls-certs
        secret:
          secretName: product-service-tls-secret
      - name: ca-certs
        secret:
          secretName: rbac-sso-ca-secret
```

### 4.5 .NET Core mTLS 配置

```csharp
// Program.cs - mTLS 配置
builder.WebHost.ConfigureKestrel((context, options) =>
{
    var certPath = context.Configuration["Kestrel:Certificates:Default:Path"];
    var keyPath = context.Configuration["Kestrel:Certificates:Default:KeyPath"];
    var caPath = context.Configuration["Kestrel:Certificates:CA:Path"];
    
    if (!string.IsNullOrEmpty(certPath))
    {
        options.ConfigureHttpsDefaults(https =>
        {
            // Server certificate
            https.ServerCertificate = X509Certificate2.CreateFromPemFile(certPath, keyPath);
            
            // Require client certificate
            https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
            
            // Validate client certificate against CA
            https.ClientCertificateValidation = (cert, chain, errors) =>
            {
                if (errors == SslPolicyErrors.None)
                    return true;
                
                // Load CA certificate
                var caCert = X509Certificate2.CreateFromPemFile(caPath);
                
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(caCert);
                
                return chain.Build(cert);
            };
        });
    }
});
```

---

## 5. Keycloak 配置

### 5.1 Realm 配置

```json
// deploy/docker/keycloak/realm-export.json
{
  "realm": "ecommerce",
  "enabled": true,
  "sslRequired": "external",
  "registrationAllowed": false,
  "loginWithEmailAllowed": true,
  "duplicateEmailsAllowed": false,
  "resetPasswordAllowed": true,
  "editUsernameAllowed": false,
  "bruteForceProtected": true,
  "roles": {
    "realm": [
      { "name": "ADMIN", "description": "System Administrator" },
      { "name": "TENANT_ADMIN", "description": "Tenant Administrator" },
      { "name": "USER", "description": "Regular User" },
      { "name": "VIEWER", "description": "Read-only User" }
    ]
  },
  "groups": [
    { "name": "admins", "path": "/admins" },
    { "name": "tenant-a-admins", "path": "/tenant-a-admins" },
    { "name": "tenant-a-users", "path": "/tenant-a-users" },
    { "name": "tenant-b-admins", "path": "/tenant-b-admins" },
    { "name": "tenant-b-users", "path": "/tenant-b-users" }
  ],
  "clients": [
    {
      "clientId": "gateway",
      "enabled": true,
      "publicClient": false,
      "secret": "gateway-secret",
      "redirectUris": ["http://localhost:8080/*"],
      "webOrigins": ["http://localhost:8080"],
      "standardFlowEnabled": true,
      "directAccessGrantsEnabled": true,
      "serviceAccountsEnabled": true,
      "protocol": "openid-connect",
      "attributes": {
        "access.token.lifespan": "3600"
      },
      "protocolMappers": [
        {
          "name": "tenant_id",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-usermodel-attribute-mapper",
          "config": {
            "claim.name": "tenant_id",
            "user.attribute": "tenant_id",
            "id.token.claim": "true",
            "access.token.claim": "true",
            "jsonType.label": "String"
          }
        },
        {
          "name": "realm roles",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-usermodel-realm-role-mapper",
          "config": {
            "claim.name": "roles",
            "multivalued": "true",
            "id.token.claim": "true",
            "access.token.claim": "true"
          }
        }
      ]
    }
  ],
  "users": [
    {
      "username": "admin",
      "email": "admin@example.com",
      "enabled": true,
      "credentials": [{ "type": "password", "value": "admin" }],
      "realmRoles": ["ADMIN"],
      "attributes": { "tenant_id": ["system"] }
    },
    {
      "username": "tenant-a-admin",
      "email": "admin-a@example.com",
      "enabled": true,
      "credentials": [{ "type": "password", "value": "admin" }],
      "realmRoles": ["TENANT_ADMIN"],
      "attributes": { "tenant_id": ["tenant-a"] }
    },
    {
      "username": "user-a",
      "email": "user-a@example.com",
      "enabled": true,
      "credentials": [{ "type": "password", "value": "user" }],
      "realmRoles": ["USER"],
      "attributes": { "tenant_id": ["tenant-a"] }
    }
  ],
  "components": {
    "org.keycloak.storage.UserStorageProvider": [
      {
        "name": "ldap",
        "providerId": "ldap",
        "config": {
          "vendor": ["other"],
          "connectionUrl": ["ldap://openldap:389"],
          "bindDn": ["cn=admin,dc=example,dc=com"],
          "bindCredential": ["admin"],
          "usersDn": ["ou=users,dc=example,dc=com"],
          "usernameLDAPAttribute": ["uid"],
          "uuidLDAPAttribute": ["entryUUID"],
          "userObjectClasses": ["inetOrgPerson"],
          "editMode": ["READ_ONLY"],
          "syncRegistrations": ["false"]
        }
      }
    ]
  }
}
```

### 5.2 LDAP Bootstrap

```ldif
# deploy/docker/ldap/bootstrap.ldif
dn: ou=users,dc=example,dc=com
objectClass: organizationalUnit
ou: users

dn: ou=groups,dc=example,dc=com
objectClass: organizationalUnit
ou: groups

dn: uid=ldap-admin,ou=users,dc=example,dc=com
objectClass: inetOrgPerson
cn: LDAP Admin
sn: Admin
uid: ldap-admin
userPassword: admin
mail: ldap-admin@example.com

dn: uid=ldap-user,ou=users,dc=example,dc=com
objectClass: inetOrgPerson
cn: LDAP User
sn: User
uid: ldap-user
userPassword: user
mail: ldap-user@example.com

dn: cn=admins,ou=groups,dc=example,dc=com
objectClass: groupOfNames
cn: admins
member: uid=ldap-admin,ou=users,dc=example,dc=com

dn: cn=users,ou=groups,dc=example,dc=com
objectClass: groupOfNames
cn: users
member: uid=ldap-user,ou=users,dc=example,dc=com
```

---

## 6. CI/CD Pipeline

### 6.1 GitHub Actions

```yaml
# .github/workflows/ci.yml
name: CI/CD Pipeline

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

env:
  DOTNET_VERSION: '8.0.x'
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal --collect:"XPlat Code Coverage"
    
    - name: Upload coverage
      uses: codecov/codecov-action@v3
      with:
        files: '**/coverage.cobertura.xml'

  docker:
    needs: build
    runs-on: ubuntu-latest
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Login to Container Registry
      uses: docker/login-action@v3
      with:
        registry: ${{ env.REGISTRY }}
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}
    
    - name: Build and push Gateway
      uses: docker/build-push-action@v5
      with:
        context: .
        file: src/Services/RbacSso.Gateway/Dockerfile
        push: true
        tags: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/gateway:${{ github.sha }}
    
    - name: Build and push Product Service
      uses: docker/build-push-action@v5
      with:
        context: .
        file: src/Services/RbacSso.ProductService/Dockerfile
        push: true
        tags: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/product-service:${{ github.sha }}

  deploy:
    needs: docker
    runs-on: ubuntu-latest
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Deploy to Kubernetes
      uses: azure/k8s-deploy@v4
      with:
        manifests: |
          deploy/k8s/base/
        images: |
          ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/gateway:${{ github.sha }}
          ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/product-service:${{ github.sha }}
```

---

## 7. 監控與日誌

### 7.1 Health Checks

```csharp
// Program.cs - Health Checks 配置
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProductDbContext>("database")
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddUrlGroup(new Uri($"{keycloakUrl}/.well-known/openid-configuration"), "keycloak");

// Endpoints
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
```

### 7.2 Logging 配置

```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    },
    "Console": {
      "FormatterName": "json",
      "FormatterOptions": {
        "SingleLine": true,
        "IncludeScopes": true,
        "TimestampFormat": "yyyy-MM-dd HH:mm:ss "
      }
    }
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "Enrich": ["FromLogContext", "WithCorrelationId"],
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/rbac-sso-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

---

## 8. 運維腳本

### 8.1 K8s 部署腳本

```bash
#!/bin/bash
# deploy/scripts/k8s-deploy.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="$SCRIPT_DIR/../k8s"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

usage() {
    echo "Usage: $0 [--build] [--delete] [--mtls]"
    echo "  --build   Build Docker images before deployment"
    echo "  --delete  Delete the deployment"
    echo "  --mtls    Deploy with mTLS enabled"
    exit 1
}

BUILD=false
DELETE=false
MTLS=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --build) BUILD=true; shift ;;
        --delete) DELETE=true; shift ;;
        --mtls) MTLS=true; shift ;;
        *) usage ;;
    esac
done

if $DELETE; then
    echo -e "${YELLOW}Deleting deployment...${NC}"
    kubectl delete namespace rbac-sso --ignore-not-found
    echo -e "${GREEN}Deployment deleted${NC}"
    exit 0
fi

# Create Kind cluster if not exists
if ! kind get clusters | grep -q rbac-sso; then
    echo -e "${YELLOW}Creating Kind cluster...${NC}"
    kind create cluster --name rbac-sso --config "$K8S_DIR/kind-config.yaml"
fi

# Build images
if $BUILD; then
    echo -e "${YELLOW}Building Docker images...${NC}"
    docker build -t rbac-sso/gateway:latest -f src/Services/RbacSso.Gateway/Dockerfile .
    docker build -t rbac-sso/product-service:latest -f src/Services/RbacSso.ProductService/Dockerfile .
    docker build -t rbac-sso/user-service:latest -f src/Services/RbacSso.UserService/Dockerfile .
    
    echo -e "${YELLOW}Loading images to Kind...${NC}"
    kind load docker-image rbac-sso/gateway:latest --name rbac-sso
    kind load docker-image rbac-sso/product-service:latest --name rbac-sso
    kind load docker-image rbac-sso/user-service:latest --name rbac-sso
fi

# Apply base manifests
echo -e "${YELLOW}Applying Kubernetes manifests...${NC}"
kubectl apply -f "$K8S_DIR/base/"

# Apply mTLS if enabled
if $MTLS; then
    echo -e "${YELLOW}Applying mTLS configuration...${NC}"
    kubectl apply -f "$K8S_DIR/security/cert-manager/"
    kubectl apply -f "$K8S_DIR/services-mtls/"
fi

# Wait for deployments
echo -e "${YELLOW}Waiting for deployments...${NC}"
kubectl wait --for=condition=Available deployment/gateway -n rbac-sso --timeout=300s
kubectl wait --for=condition=Available deployment/product-service -n rbac-sso --timeout=300s

echo -e "${GREEN}=== Deployment Complete ===${NC}"
kubectl get pods -n rbac-sso
```

### 8.2 整合測試腳本

```bash
#!/bin/bash
# deploy/scripts/integration-test.sh

set -e

GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

GATEWAY_URL="http://localhost:8080"
PASSED=0
FAILED=0

test_endpoint() {
    local name="$1"
    local method="$2"
    local url="$3"
    local expected="$4"
    
    response=$(curl -s -o /dev/null -w "%{http_code}" -X "$method" "$url")
    
    if [ "$response" == "$expected" ]; then
        echo -e "${GREEN}✓${NC} $name (HTTP $response)"
        ((PASSED++))
    else
        echo -e "${RED}✗${NC} $name (Expected $expected, got $response)"
        ((FAILED++))
    fi
}

echo "=== Running Integration Tests ==="

# Health checks
test_endpoint "Gateway Health" "GET" "$GATEWAY_URL/health" "200"
test_endpoint "Product Service Health" "GET" "http://localhost:8081/health" "200"
test_endpoint "User Service Health" "GET" "http://localhost:8082/health" "200"

# Authentication tests
test_endpoint "Products (No Auth)" "GET" "$GATEWAY_URL/api/products" "401"
test_endpoint "Users (No Auth)" "GET" "$GATEWAY_URL/api/users/me" "401"

echo ""
echo "=== Test Results ==="
echo -e "Passed: ${GREEN}$PASSED${NC}"
echo -e "Failed: ${RED}$FAILED${NC}"

exit $FAILED
```

---

## 附錄

### A. 環境變數參考

| 變數名稱 | 說明 | 預設值 |
|----------|------|--------|
| `ASPNETCORE_ENVIRONMENT` | 環境 | Development |
| `ASPNETCORE_URLS` | 監聽 URL | http://+:8080 |
| `ConnectionStrings__DefaultConnection` | 資料庫連線字串 | - |
| `Keycloak__Authority` | Keycloak Realm URL | - |
| `Keycloak__Audience` | JWT Audience | gateway |

### B. 端口對照表

| 服務 | Docker Port | K8s Port | NodePort |
|------|-------------|----------|----------|
| Gateway | 8080 | 8080 | 30080 |
| Product Service | 8081 | 8081 | - |
| User Service | 8082 | 8082 | - |
| Keycloak | 8180 | 8080 | 30180 |
| PostgreSQL | 5432 | 5432 | - |
| OpenLDAP | 389 | 389 | - |

### C. 參考文件

- PRD.md - 產品需求文件
- TECH.md - 技術架構文件
- Kubernetes Documentation
- cert-manager Documentation
