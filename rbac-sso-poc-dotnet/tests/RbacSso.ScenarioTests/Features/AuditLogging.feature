@audit @us4
Feature: Audit Logging
  審計日誌功能
  As a compliance officer
  I want all significant operations to be logged
  So that I can investigate security incidents and maintain compliance

  Background:
    Given 系統已啟動
    And Keycloak 認證服務已連線
    And 審計服務已啟動

  @product-audit
  Scenario: Product creation generates audit log
    # 產品建立產生審計日誌
    Given 我以 "tenant-a-admin" 身份登入
    When 我建立一個新產品:
      | Name       | Price | Category    |
      | Audit Test | 99.99 | Electronics |
    Then 應該產生審計事件
    And 審計事件類型應該是 "ProductCreated"
    And 審計事件應該包含:
      | Field       | Value           |
      | UserId      | tenant-a-admin  |
      | TenantId    | tenant-a        |
      | Action      | CREATE          |
      | Result      | SUCCESS         |

  @product-audit
  Scenario: Product update generates audit log
    # 產品更新產生審計日誌
    Given 系統中存在產品 "Original Product"
    And 我以 "admin" 身份登入
    When 我更新產品 "Original Product":
      | Name            |
      | Updated Product |
    Then 應該產生審計事件
    And 審計事件類型應該是 "ProductUpdated"
    And 審計事件應該包含變更前後的值

  @product-audit
  Scenario: Product deletion generates audit log
    # 產品刪除產生審計日誌
    Given 系統中存在產品 "To Be Deleted"
    And 我以 "admin" 身份登入
    When 我刪除產品 "To Be Deleted"
    Then 應該產生審計事件
    And 審計事件類型應該是 "ProductDeleted"
    And 審計事件結果應該是 "SUCCESS"

  @product-audit
  Scenario: Price change generates specific audit event
    # 價格變更產生特定審計事件
    Given 系統中存在產品 "Price Test" 價格為 100.00
    And 我以 "admin" 身份登入
    When 我更新產品 "Price Test" 價格為 150.00
    Then 應該產生審計事件
    And 審計事件類型應該是 "ProductPriceChanged"
    And 審計事件應該包含:
      | Field     | Value  |
      | OldPrice  | 100.00 |
      | NewPrice  | 150.00 |

  @auth-audit
  Scenario: Successful login generates audit log
    # 成功登入產生審計日誌
    When 我以 "user-a" 身份登入
    Then 應該產生審計事件
    And 審計事件類型應該是 "LoginSucceeded"
    And 審計事件應該包含:
      | Field      | Value   |
      | UserId     | user-a  |
      | Result     | SUCCESS |
      | IpAddress  | *       |

  @auth-audit
  Scenario: Failed login generates audit log
    # 登入失敗產生審計日誌
    When 我以 "user-a" 身份使用錯誤密碼登入
    Then 應該產生審計事件
    And 審計事件類型應該是 "LoginFailed"
    And 審計事件結果應該是 "FAILURE"

  @auth-audit
  Scenario: Logout generates audit log
    # 登出產生審計日誌
    Given 我以 "user-a" 身份登入
    When 我請求登出
    Then 應該產生審計事件
    And 審計事件類型應該是 "LogoutCompleted"

  @permission-audit
  Scenario: Permission denied generates audit log
    # 權限拒絕產生審計日誌
    Given 我以 "viewer" 身份登入
    When 我嘗試刪除產品 "Protected Product"
    Then 應該產生審計事件
    And 審計事件類型應該是 "PermissionDenied"
    And 審計事件應該包含:
      | Field         | Value          |
      | UserId        | viewer         |
      | Action        | DELETE         |
      | Resource      | Product        |
      | RequiredRoles | ADMIN,TENANT_ADMIN |
      | Result        | DENIED         |

  @query-audit
  Scenario: Audit logs can be queried by date range
    # 審計日誌可依日期範圍查詢
    Given 系統中存在過去 7 天的審計日誌
    And 我以 "admin" 身份登入
    When 我查詢過去 3 天的審計日誌
    Then 回應應該只包含過去 3 天的日誌

  @query-audit
  Scenario: Audit logs can be filtered by event type
    # 審計日誌可依事件類型篩選
    Given 系統中存在多種類型的審計日誌
    And 我以 "admin" 身份登入
    When 我查詢事件類型為 "ProductCreated" 的審計日誌
    Then 所有回傳的日誌類型都應該是 "ProductCreated"

  @query-audit
  Scenario: Audit logs can be filtered by user
    # 審計日誌可依使用者篩選
    Given 系統中存在多個使用者的審計日誌
    And 我以 "admin" 身份登入
    When 我查詢使用者 "user-a" 的審計日誌
    Then 所有回傳的日誌使用者都應該是 "user-a"

  @tenant-audit
  Scenario: TENANT_ADMIN can only see their tenant's audit logs
    # TENANT_ADMIN 只能看到其租戶的審計日誌
    Given 系統中存在 "tenant-a" 租戶的審計日誌
    And 系統中存在 "tenant-b" 租戶的審計日誌
    And 我以 "tenant-a-admin" 身份登入
    When 我查詢審計日誌列表
    Then 所有回傳的日誌租戶 ID 都應該是 "tenant-a"

  @retention
  Scenario: Audit logs are retained for 90 days
    # 審計日誌保留 90 天
    Given 系統中存在 100 天前的審計日誌
    When 審計日誌清理排程執行
    Then 100 天前的審計日誌應該被刪除
    And 89 天前的審計日誌應該被保留

  @immutable
  Scenario: Audit logs cannot be modified
    # 審計日誌不可修改
    Given 系統中存在審計日誌 ID "<audit_id>"
    And 我以 "admin" 身份登入
    When 我嘗試修改審計日誌 ID "<audit_id>"
    Then 回應狀態碼應該是 405
    # Method Not Allowed

  @immutable
  Scenario: Audit logs cannot be deleted by API
    # 審計日誌無法透過 API 刪除
    Given 系統中存在審計日誌 ID "<audit_id>"
    And 我以 "admin" 身份登入
    When 我嘗試刪除審計日誌 ID "<audit_id>"
    Then 回應狀態碼應該是 405
    # Method Not Allowed

  @correlation
  Scenario: Audit logs include correlation ID
    # 審計日誌包含關聯 ID
    Given 我以 "admin" 身份登入
    When 我建立一個新產品並記錄 correlation ID
    Then 審計事件應該包含相同的 correlation ID

  @performance
  Scenario: Audit logging does not block main operations
    # 審計日誌不阻擋主要操作
    Given 我以 "admin" 身份登入
    When 我建立一個新產品
    Then 回應時間應該小於 500ms
    And 審計事件應該被非同步記錄
