#!/bin/bash
# =============================================================================
# Certificate Generation Script for mTLS
# 用於 mTLS 的憑證生成腳本
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CERTS_DIR="${SCRIPT_DIR}/../certs"
VALIDITY_DAYS=365

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Create certs directory
mkdir -p "${CERTS_DIR}"
cd "${CERTS_DIR}"

log_info "Generating certificates in ${CERTS_DIR}"

# =============================================================================
# 1. Generate CA (Certificate Authority)
# =============================================================================
log_info "Generating CA certificate..."

# CA private key
openssl genrsa -out ca.key 4096

# CA certificate
openssl req -new -x509 -days ${VALIDITY_DAYS} -key ca.key -out ca.crt \
    -subj "/C=TW/ST=Taiwan/L=Taipei/O=RBAC-SSO-POC/OU=Security/CN=RBAC-SSO-CA"

log_info "CA certificate generated: ca.crt"

# =============================================================================
# 2. Generate Gateway Certificate
# =============================================================================
log_info "Generating Gateway certificate..."

# Gateway private key
openssl genrsa -out gateway.key 2048

# Gateway CSR
openssl req -new -key gateway.key -out gateway.csr \
    -subj "/C=TW/ST=Taiwan/L=Taipei/O=RBAC-SSO-POC/OU=Gateway/CN=gateway"

# Gateway certificate with SAN
cat > gateway.ext << EOF
authorityKeyIdentifier=keyid,issuer
basicConstraints=CA:FALSE
keyUsage = digitalSignature, nonRepudiation, keyEncipherment, dataEncipherment
extendedKeyUsage = serverAuth, clientAuth
subjectAltName = @alt_names

[alt_names]
DNS.1 = gateway
DNS.2 = localhost
DNS.3 = rbac-sso-gateway
IP.1 = 127.0.0.1
EOF

openssl x509 -req -in gateway.csr -CA ca.crt -CAkey ca.key -CAcreateserial \
    -out gateway.crt -days ${VALIDITY_DAYS} -extfile gateway.ext

# Create PFX for .NET
openssl pkcs12 -export -out gateway.pfx -inkey gateway.key -in gateway.crt \
    -certfile ca.crt -passout pass:changeit

log_info "Gateway certificate generated: gateway.crt, gateway.pfx"

# =============================================================================
# 3. Generate ProductService Certificate
# =============================================================================
log_info "Generating ProductService certificate..."

openssl genrsa -out product-service.key 2048

openssl req -new -key product-service.key -out product-service.csr \
    -subj "/C=TW/ST=Taiwan/L=Taipei/O=RBAC-SSO-POC/OU=ProductService/CN=product-service"

cat > product-service.ext << EOF
authorityKeyIdentifier=keyid,issuer
basicConstraints=CA:FALSE
keyUsage = digitalSignature, nonRepudiation, keyEncipherment, dataEncipherment
extendedKeyUsage = serverAuth, clientAuth
subjectAltName = @alt_names

[alt_names]
DNS.1 = product-service
DNS.2 = localhost
DNS.3 = rbac-sso-product-service
IP.1 = 127.0.0.1
EOF

openssl x509 -req -in product-service.csr -CA ca.crt -CAkey ca.key -CAcreateserial \
    -out product-service.crt -days ${VALIDITY_DAYS} -extfile product-service.ext

openssl pkcs12 -export -out product-service.pfx -inkey product-service.key \
    -in product-service.crt -certfile ca.crt -passout pass:changeit

log_info "ProductService certificate generated: product-service.crt, product-service.pfx"

# =============================================================================
# 4. Generate AuditService Certificate
# =============================================================================
log_info "Generating AuditService certificate..."

openssl genrsa -out audit-service.key 2048

openssl req -new -key audit-service.key -out audit-service.csr \
    -subj "/C=TW/ST=Taiwan/L=Taipei/O=RBAC-SSO-POC/OU=AuditService/CN=audit-service"

cat > audit-service.ext << EOF
authorityKeyIdentifier=keyid,issuer
basicConstraints=CA:FALSE
keyUsage = digitalSignature, nonRepudiation, keyEncipherment, dataEncipherment
extendedKeyUsage = serverAuth, clientAuth
subjectAltName = @alt_names

[alt_names]
DNS.1 = audit-service
DNS.2 = localhost
DNS.3 = rbac-sso-audit-service
IP.1 = 127.0.0.1
EOF

openssl x509 -req -in audit-service.csr -CA ca.crt -CAkey ca.key -CAcreateserial \
    -out audit-service.crt -days ${VALIDITY_DAYS} -extfile audit-service.ext

openssl pkcs12 -export -out audit-service.pfx -inkey audit-service.key \
    -in audit-service.crt -certfile ca.crt -passout pass:changeit

log_info "AuditService certificate generated: audit-service.crt, audit-service.pfx"

# =============================================================================
# 5. Generate UserService Certificate
# =============================================================================
log_info "Generating UserService certificate..."

openssl genrsa -out user-service.key 2048

openssl req -new -key user-service.key -out user-service.csr \
    -subj "/C=TW/ST=Taiwan/L=Taipei/O=RBAC-SSO-POC/OU=UserService/CN=user-service"

cat > user-service.ext << EOF
authorityKeyIdentifier=keyid,issuer
basicConstraints=CA:FALSE
keyUsage = digitalSignature, nonRepudiation, keyEncipherment, dataEncipherment
extendedKeyUsage = serverAuth, clientAuth
subjectAltName = @alt_names

[alt_names]
DNS.1 = user-service
DNS.2 = localhost
DNS.3 = rbac-sso-user-service
IP.1 = 127.0.0.1
EOF

openssl x509 -req -in user-service.csr -CA ca.crt -CAkey ca.key -CAcreateserial \
    -out user-service.crt -days ${VALIDITY_DAYS} -extfile user-service.ext

openssl pkcs12 -export -out user-service.pfx -inkey user-service.key \
    -in user-service.crt -certfile ca.crt -passout pass:changeit

log_info "UserService certificate generated: user-service.crt, user-service.pfx"

# =============================================================================
# 6. Cleanup temporary files
# =============================================================================
rm -f *.csr *.ext *.srl

# =============================================================================
# 7. Summary
# =============================================================================
log_info "============================================"
log_info "Certificate generation complete!"
log_info "============================================"
log_info "Generated files:"
ls -la "${CERTS_DIR}"

log_warn "IMPORTANT: Store certificates securely and never commit to version control!"
log_warn "PFX password for all certificates: changeit"
log_warn "Update passwords in production deployments!"
