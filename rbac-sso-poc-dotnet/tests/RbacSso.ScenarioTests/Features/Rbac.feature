@rbac @us2
Feature: Role-Based Access Control
  角色存取控制功能
  As a system
  I want to enforce role-based access control
  So that users can only perform authorized actions

  Background:
    Given 系統已啟動
    And Keycloak 認證服務已連線

  @permission-matrix
  Scenario Outline: Role permission matrix for product operations
    # 角色權限矩陣 - 產品操作
    Given 我以 "<role>" 身份登入
    When 我執行 "<operation>" 操作
    Then 回應狀態碼應該是 <expected_status>

    Examples: ADMIN permissions
      | role  | operation | expected_status |
      | admin | CREATE    | 201             |
      | admin | READ      | 200             |
      | admin | UPDATE    | 200             |
      | admin | DELETE    | 204             |

    Examples: TENANT_ADMIN permissions
      | role           | operation | expected_status |
      | tenant-a-admin | CREATE    | 201             |
      | tenant-a-admin | READ      | 200             |
      | tenant-a-admin | UPDATE    | 200             |
      | tenant-a-admin | DELETE    | 204             |

    Examples: USER permissions
      | role   | operation | expected_status |
      | user-a | CREATE    | 201             |
      | user-a | READ      | 200             |
      | user-a | UPDATE    | 200             |
      | user-a | DELETE    | 403             |

    Examples: VIEWER permissions
      | role   | operation | expected_status |
      | viewer | CREATE    | 403             |
      | viewer | READ      | 200             |
      | viewer | UPDATE    | 403             |
      | viewer | DELETE    | 403             |

  @unauthorized
  Scenario: Unauthenticated request is rejected
    # 未認證請求被拒絕
    Given 我未登入
    When 我嘗試存取產品 API
    Then 回應狀態碼應該是 401
    And 錯誤訊息應該包含 "Unauthorized"

  @token-expiration
  Scenario: Expired token is rejected
    # 過期的 token 被拒絕
    Given 我有一個過期的 JWT token
    When 我嘗試存取產品 API
    Then 回應狀態碼應該是 401

  @role-hierarchy
  Scenario: ADMIN has access to all tenant data
    # ADMIN 可存取所有租戶資料
    Given 系統中存在 "tenant-a" 租戶的產品 "Product A"
    And 系統中存在 "tenant-b" 租戶的產品 "Product B"
    And 我以 "admin" 身份登入
    When 我查詢產品列表
    Then 回應應該包含 "Product A"
    And 回應應該包含 "Product B"

  @audit-denied
  Scenario: Permission denied events are logged
    # 權限拒絕事件被記錄
    Given 我以 "viewer" 身份登入
    When 我嘗試刪除產品 "Some Product"
    Then 回應狀態碼應該是 403
    And 應該產生 "PermissionDenied" 審計事件

  @jwt-claims
  Scenario: JWT token contains required claims
    # JWT token 包含必要的 claims
    Given 我以 "tenant-a-admin" 身份登入
    Then 我的 token 應該包含 claim "tenant_id" 值為 "tenant-a"
    And 我的 token 應該包含 claim "realm_access.roles" 包含 "TENANT_ADMIN"

  @role-change
  Scenario: User with multiple roles gets highest permission
    # 擁有多個角色的使用者獲得最高權限
    Given 使用者 "multi-role-user" 同時擁有 "USER" 和 "TENANT_ADMIN" 角色
    And 我以 "multi-role-user" 身份登入
    When 我嘗試刪除產品
    Then 回應狀態碼應該是 204
    # TENANT_ADMIN 權限允許刪除

  @cross-service
  Scenario: Authorization is enforced consistently across services
    # 授權在所有服務間一致執行
    Given 我以 "viewer" 身份登入
    When 我嘗試在 ProductService 建立產品
    Then 回應狀態碼應該是 403
    When 我嘗試在 AuditService 刪除審計記錄
    Then 回應狀態碼應該是 403

  @session-timeout
  Scenario: Session timeout triggers re-authentication
    # 會話超時觸發重新認證
    Given 我以 "user-a" 身份登入
    And 我的會話已超時 (超過 30 分鐘)
    When 我嘗試存取產品 API
    Then 回應狀態碼應該是 401
    And 我需要重新登入
