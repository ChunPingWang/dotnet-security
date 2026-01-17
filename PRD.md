# PRD.md - 產品需求文件

## RBAC-SSO-POC (.NET Core 版)

**版本**: 1.1
**日期**: 2026-01-17
**狀態**: Planning Complete
**稽核機制**: Domain Events

### 規劃文件

| 文件 | 路徑 | 狀態 |
|------|------|------|
| 功能規格 | `specs/001-rbac-sso-poc/spec.md` | ✅ Complete |
| 實作計畫 | `specs/001-rbac-sso-poc/plan.md` | ✅ Complete |
| 任務清單 | `specs/001-rbac-sso-poc/tasks.md` | ✅ 125 tasks |
| 資料模型 | `specs/001-rbac-sso-poc/data-model.md` | ✅ Complete |
| API 契約 | `specs/001-rbac-sso-poc/contracts/openapi.yaml` | ✅ Complete |
| 快速開始 | `specs/001-rbac-sso-poc/quickstart.md` | ✅ Complete |
| 分析報告 | `specs/001-rbac-sso-poc/001-rbac-sso-poc.md` | ✅ All issues resolved |

---

## 1. 產品概述

### 1.1 產品願景

建立一個基於 .NET Core 的多租戶電子商務平台 POC，展示企業級 RBAC 權限控制與 SSO 單一登入整合，採用 Hexagonal Architecture 與 Domain-Driven Design，並使用 **Domain Events 進行稽核日誌** 記錄。

### 1.2 目標用戶

| 角色 | 說明 |
|------|------|
| 系統管理員 (ADMIN) | 管理所有租戶、使用者、商品 |
| 租戶管理員 (TENANT_ADMIN) | 管理所屬租戶的商品與使用者 |
| 一般使用者 (USER) | 查看商品、下單 |
| 唯讀使用者 (VIEWER) | 僅能查看商品資訊 |

### 1.3 核心價值主張

1. **多租戶資料隔離** - 確保租戶間資料安全
2. **細粒度權限控制** - 基於角色的存取控制 (RBAC)
3. **單一登入體驗** - OAuth2/OIDC 整合 Keycloak
4. **完整稽核追蹤** - Domain Events 驅動的稽核日誌
5. **架構最佳實踐** - Hexagonal Architecture + DDD + CQRS

---

## 2. 功能需求

### 2.1 認證與授權 (Authentication & Authorization)

#### FR-AUTH-001: SSO 單一登入
- **描述**: 使用者透過 Keycloak 進行 OAuth2/OIDC 認證
- **優先級**: P0 (必要)
- **驗收條件**:
  - [ ] 支援 Authorization Code Flow
  - [ ] 支援 LDAP 使用者同步
  - [ ] JWT Token 包含 roles、tenant_id claims
  - [ ] Token 過期自動刷新

#### FR-AUTH-002: RBAC 權限控制
- **描述**: 基於角色的存取控制
- **優先級**: P0 (必要)
- **角色權限矩陣**:

| 端點 | ADMIN | TENANT_ADMIN | USER | VIEWER |
|------|-------|--------------|------|--------|
| `GET /api/products` | ✅ | ✅ | ✅ | ✅ |
| `GET /api/products/{id}` | ✅ | ✅ | ✅ | ✅ |
| `POST /api/products` | ✅ | ✅ | ❌ | ❌ |
| `PUT /api/products/{id}` | ✅ | ✅ | ❌ | ❌ |
| `DELETE /api/products/{id}` | ✅ | ❌ | ❌ | ❌ |
| `GET /api/users/me` | ✅ | ✅ | ✅ | ✅ |
| `GET /api/admin/users` | ✅ | ❌ | ❌ | ❌ |
| `GET /api/audit/logs` | ✅ | ✅ | ❌ | ❌ |

### 2.2 商品管理 (Product Management)

#### FR-PROD-001: 商品 CRUD
- **描述**: 商品的建立、讀取、更新、刪除
- **優先級**: P0 (必要)
- **驗收條件**:
  - [ ] 建立商品時自動產生商品代碼 (P + 6位數字)
  - [ ] 商品代碼不可重複
  - [ ] 價格必須為正數
  - [ ] 刪除為軟刪除 (狀態變為 DELETED)
  - [ ] 自動記錄 createdBy、updatedBy

#### FR-PROD-002: 商品查詢
- **描述**: 支援分頁、篩選、排序
- **優先級**: P1 (重要)
- **驗收條件**:
  - [ ] 支援分頁查詢 (page, size)
  - [ ] 支援分類篩選
  - [ ] 支援排序 (name, price, createdAt)
  - [ ] 租戶隔離 - 只能查看所屬租戶商品

### 2.3 多租戶 (Multi-Tenancy)

#### FR-TENANT-001: 租戶資料隔離
- **描述**: 確保不同租戶的資料互相隔離
- **優先級**: P0 (必要)
- **驗收條件**:
  - [ ] 租戶 A 無法存取租戶 B 的商品
  - [ ] 租戶 ID 從 JWT Token 自動擷取
  - [ ] 系統管理員可跨租戶查詢
  - [ ] 所有 API 自動套用租戶過濾

### 2.4 稽核日誌 (Audit Logging) - Domain Events

#### FR-AUDIT-001: Domain Events 稽核
- **描述**: 使用 Domain Events 機制記錄所有重要操作
- **優先級**: P0 (必要)
- **觸發事件**:

| Domain Event | 觸發時機 | 記錄資訊 |
|--------------|----------|----------|
| ProductCreated | 商品建立 | productId, name, price, createdBy |
| ProductUpdated | 商品更新 | productId, 變更欄位, updatedBy |
| ProductPriceChanged | 價格變更 | productId, oldPrice, newPrice, changedBy |
| ProductDeleted | 商品刪除 | productId, deletedBy |
| UserLoggedIn | 使用者登入 | username, loginTime, clientIp |
| PermissionDenied | 權限拒絕 | username, resource, action |

- **驗收條件**:
  - [ ] Domain Event 在 Aggregate 內部產生
  - [ ] 透過 MediatR INotification 發布
  - [ ] AuditEventHandler 訂閱並記錄
  - [ ] 稽核日誌包含 correlationId 支援追蹤
  - [ ] 支援稽核日誌查詢 API

#### FR-AUDIT-002: 稽核日誌查詢
- **描述**: 提供稽核日誌查詢功能
- **優先級**: P1 (重要)
- **驗收條件**:
  - [ ] 依使用者查詢
  - [ ] 依事件類型查詢
  - [ ] 依時間範圍查詢
  - [ ] 依 correlationId 查詢
  - [ ] 支援分頁

### 2.5 東西向安全 (Service-to-Service Security)

#### FR-SEC-001: mTLS 雙向認證
- **描述**: 服務間通訊使用 mTLS 加密
- **優先級**: P1 (重要)
- **驗收條件**:
  - [ ] 使用 cert-manager 自動簽發憑證
  - [ ] 憑證自動更新 (到期前 30 天)
  - [ ] 服務間強制驗證客戶端憑證
  - [ ] 支援 TLS 1.3

---

## 3. 非功能需求

### 3.1 效能 (Performance)

| 指標 | 目標值 |
|------|--------|
| API 回應時間 (P95) | < 200ms |
| 吞吐量 | > 1000 RPS |
| 資料庫連線池 | 20-50 connections |

### 3.2 可用性 (Availability)

| 指標 | 目標值 |
|------|--------|
| 系統可用性 | 99.9% |
| RTO (Recovery Time Objective) | < 1 小時 |
| RPO (Recovery Point Objective) | < 5 分鐘 |

### 3.3 安全性 (Security)

| 需求 | 說明 |
|------|------|
| 傳輸加密 | HTTPS (TLS 1.2+) |
| Token 安全 | JWT RS256 簽章 |
| 密碼儲存 | bcrypt / Argon2 |
| 稽核日誌 | 保留 90 天 |

### 3.4 可維護性 (Maintainability)

| 需求 | 說明 |
|------|------|
| 程式碼覆蓋率 | > 80% |
| 架構合規測試 | NetArchTest |
| API 文件 | OpenAPI 3.0 (Swagger) |

---

## 4. 使用案例 (Use Cases)

### UC-001: 管理員建立商品

```gherkin
# language: zh-TW
功能: 商品管理

  場景: 管理員建立新商品
    假設 使用者 "admin" 已登入系統，角色為 "ADMIN"
    當 使用者建立商品:
      | 商品名稱    | 價格  | 分類     | 描述           |
      | 測試商品 A | 1000  | 電子產品 | 這是測試商品 A |
    那麼 系統應回傳成功訊息
    而且 商品應該被成功建立
    而且 應產生 "ProductCreated" 稽核事件
```

### UC-002: 多租戶資料隔離

```gherkin
# language: zh-TW
功能: 多租戶資料隔離

  場景: 租戶只能查看自己的商品
    假設 租戶 "tenant-a" 有商品 "商品 A1" 和 "商品 A2"
    而且 租戶 "tenant-b" 有商品 "商品 B1"
    當 租戶 "tenant-a" 的使用者查詢商品列表
    那麼 只應看到屬於 "tenant-a" 的商品
    而且 不應看到 "tenant-b" 的商品
```

### UC-003: Domain Event 稽核流程

```gherkin
# language: zh-TW
功能: Domain Event 稽核

  場景: 商品建立產生稽核事件
    假設 使用者 "admin" 已登入系統
    當 使用者建立商品 "新商品"
    那麼 Product Aggregate 應產生 "ProductCreated" 事件
    而且 MediatR 應發布該事件
    而且 AuditEventHandler 應記錄稽核日誌
    而且 稽核日誌應包含:
      | 欄位        | 值                |
      | eventType   | CREATE_PRODUCT    |
      | username    | admin             |
      | result      | SUCCESS           |
```

---

## 5. 資料模型

### 5.1 Product Aggregate

```
Product (Aggregate Root)
├── ProductId (Value Object)
├── ProductCode (Value Object) - P + 6位數字
├── Name (string)
├── Price (Money Value Object)
├── Category (string)
├── Description (string)
├── Status (Enum: ACTIVE, INACTIVE, DELETED)
├── TenantId (string)
├── CreatedBy (string)
├── CreatedAt (DateTimeOffset)
├── UpdatedBy (string)
├── UpdatedAt (DateTimeOffset)
└── DomainEvents (List<IDomainEvent>)
```

### 5.2 AuditLog Entity

```
AuditLog
├── Id (Guid)
├── Timestamp (DateTimeOffset)
├── EventType (string)
├── AggregateType (string)
├── AggregateId (string)
├── Username (string)
├── ServiceName (string)
├── Action (string)
├── Payload (JSON string)
├── Result (Enum: SUCCESS, FAILURE)
├── ErrorMessage (string?)
├── ClientIp (string?)
├── CorrelationId (string)
└── PayloadTruncated (bool)
```

---

## 6. API 規格摘要

### 6.1 Product API

| Method | Endpoint | 說明 | Auth |
|--------|----------|------|------|
| GET | /api/products | 商品列表 | ADMIN, TENANT_ADMIN, USER, VIEWER |
| GET | /api/products/{id} | 商品詳情 | ADMIN, TENANT_ADMIN, USER, VIEWER |
| POST | /api/products | 建立商品 | ADMIN, TENANT_ADMIN |
| PUT | /api/products/{id} | 更新商品 | ADMIN, TENANT_ADMIN |
| DELETE | /api/products/{id} | 刪除商品 | ADMIN |

### 6.2 Audit API

| Method | Endpoint | 說明 | Auth |
|--------|----------|------|------|
| GET | /api/audit/logs | 稽核日誌列表 | ADMIN, TENANT_ADMIN |
| GET | /api/audit/logs/{id} | 稽核日誌詳情 | ADMIN, TENANT_ADMIN |
| GET | /api/audit/logs/by-user/{username} | 依使用者查詢 | ADMIN |
| GET | /api/audit/logs/by-correlation/{id} | 依關聯 ID 查詢 | ADMIN |

---

## 7. 驗收標準

### 7.1 功能驗收

- [ ] 所有 BDD 場景測試通過 (18+ scenarios)
- [ ] 單元測試覆蓋率 > 80%
- [ ] API 整合測試通過
- [ ] Domain Events 稽核正常運作

### 7.2 非功能驗收

- [ ] API 回應時間 P95 < 200ms
- [ ] 無安全漏洞 (OWASP Top 10)
- [ ] Docker/K8s 部署成功
- [ ] mTLS 服務間通訊正常

---

## 8. 里程碑

| 里程碑 | 目標日期 | 交付物 |
|--------|----------|--------|
| M1: 基礎架構 | Week 3 | Solution 結構、Domain Layer |
| M2: 核心服務 | Week 7 | Product/User/Audit Services |
| M3: 安全整合 | Week 10 | Keycloak、RBAC、mTLS |
| M4: 測試部署 | Week 13 | 測試完成、K8s 部署 |

---

## 附錄

### A. 名詞解釋

| 名詞 | 說明 |
|------|------|
| RBAC | Role-Based Access Control，基於角色的存取控制 |
| SSO | Single Sign-On，單一登入 |
| Domain Event | 領域事件，表示領域中發生的重要事實 |
| Aggregate | 聚合，DDD 中的一致性邊界 |
| CQRS | Command Query Responsibility Segregation，命令查詢責任分離 |

### B. 參考文件

- TECH.md - 技術架構文件
- INFRA.md - 基礎設施文件
