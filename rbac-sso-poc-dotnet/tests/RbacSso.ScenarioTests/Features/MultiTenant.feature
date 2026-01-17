@multi-tenant @us3
Feature: Multi-Tenant Data Isolation
  多租戶資料隔離功能
  As a system
  I want to ensure complete tenant data isolation
  So that tenant A cannot access tenant B's data

  Background:
    Given 系統已啟動
    And Keycloak 認證服務已連線

  @isolation @critical
  Scenario: Tenant A cannot see Tenant B's products
    # 租戶 A 無法看到租戶 B 的產品
    Given 系統中存在 "tenant-a" 租戶的產品 "Product A"
    And 系統中存在 "tenant-b" 租戶的產品 "Product B"
    And 我以 "tenant-a-admin" 身份登入
    When 我查詢產品列表
    Then 回應應該包含 "Product A"
    And 回應不應該包含 "Product B"

  @isolation @critical
  Scenario: Tenant A cannot access Tenant B's product by ID
    # 租戶 A 無法透過 ID 存取租戶 B 的產品
    Given 系統中存在 "tenant-b" 租戶的產品 "Secret Product" 且 ID 為 "<product_id>"
    And 我以 "tenant-a-admin" 身份登入
    When 我嘗試存取產品 ID "<product_id>"
    Then 回應狀態碼應該是 404
    # 為安全考量，回傳 404 而非 403

  @isolation @critical
  Scenario: Tenant A cannot update Tenant B's product
    # 租戶 A 無法更新租戶 B 的產品
    Given 系統中存在 "tenant-b" 租戶的產品 "Protected Product" 且 ID 為 "<product_id>"
    And 我以 "tenant-a-admin" 身份登入
    When 我嘗試更新產品 ID "<product_id>"
    Then 回應狀態碼應該是 404

  @isolation @critical
  Scenario: Tenant A cannot delete Tenant B's product
    # 租戶 A 無法刪除租戶 B 的產品
    Given 系統中存在 "tenant-b" 租戶的產品 "Protected Product" 且 ID 為 "<product_id>"
    And 我以 "tenant-a-admin" 身份登入
    When 我嘗試刪除產品 ID "<product_id>"
    Then 回應狀態碼應該是 404

  @admin-bypass
  Scenario: ADMIN can see all tenants' products
    # ADMIN 可以看到所有租戶的產品
    Given 系統中存在 "tenant-a" 租戶的產品 "Product A"
    And 系統中存在 "tenant-b" 租戶的產品 "Product B"
    And 系統中存在 "tenant-c" 租戶的產品 "Product C"
    And 我以 "admin" 身份登入
    When 我查詢產品列表
    Then 回應應該包含 "Product A"
    And 回應應該包含 "Product B"
    And 回應應該包含 "Product C"

  @admin-bypass
  Scenario: ADMIN can update any tenant's product
    # ADMIN 可以更新任何租戶的產品
    Given 系統中存在 "tenant-b" 租戶的產品 "Any Product"
    And 我以 "admin" 身份登入
    When 我更新產品 "Any Product":
      | Name            |
      | Updated Product |
    Then 回應狀態碼應該是 200
    And 產品名稱應該是 "Updated Product"

  @tenant-context
  Scenario: Product is created with user's tenant ID
    # 產品建立時使用使用者的租戶 ID
    Given 我以 "tenant-a-admin" 身份登入
    When 我建立一個新產品:
      | Name       | Price | Category |
      | New Item   | 49.99 | Other    |
    Then 回應狀態碼應該是 201
    And 產品的租戶 ID 應該是 "tenant-a"

  @tenant-context
  Scenario: Tenant ID cannot be spoofed in request
    # 租戶 ID 無法在請求中被偽造
    Given 我以 "tenant-a-admin" 身份登入
    When 我嘗試建立產品並偽造租戶 ID 為 "tenant-b":
      | Name         | Price | Category |
      | Spoofed Item | 99.99 | Other    |
    Then 產品的租戶 ID 應該是 "tenant-a"
    # 系統應忽略請求中的租戶 ID，使用 JWT 中的值

  @query-filter
  Scenario: Database query filter enforces tenant isolation
    # 資料庫查詢篩選器強制租戶隔離
    Given 資料庫中存在以下產品:
      | Name      | TenantId |
      | Product 1 | tenant-a |
      | Product 2 | tenant-a |
      | Product 3 | tenant-b |
      | Product 4 | tenant-c |
    And 我以 "tenant-a-admin" 身份登入
    When 我查詢產品列表
    Then 回應應該只包含 2 個產品
    And 所有產品的租戶 ID 都應該是 "tenant-a"

  @audit
  Scenario: Cross-tenant access attempts are logged
    # 跨租戶存取嘗試被記錄
    Given 系統中存在 "tenant-b" 租戶的產品 "Secret Data" 且 ID 為 "<product_id>"
    And 我以 "tenant-a-admin" 身份登入
    When 我嘗試存取產品 ID "<product_id>"
    Then 應該產生 "CrossTenantAccessAttempt" 審計事件
    And 審計事件應該包含嘗試者的租戶 ID "tenant-a"
    And 審計事件應該包含目標資源的 ID

  @new-tenant
  Scenario: New tenant starts with empty product catalog
    # 新租戶開始時產品目錄為空
    Given 系統中新增租戶 "tenant-new"
    And 我以 "tenant-new" 的管理員身份登入
    When 我查詢產品列表
    Then 回應應該包含 0 個產品

  @migration
  Scenario: Product tenant assignment is immutable
    # 產品的租戶指派不可變更
    Given 系統中存在 "tenant-a" 租戶的產品 "Fixed Product"
    And 我以 "admin" 身份登入
    When 我嘗試將產品的租戶 ID 變更為 "tenant-b"
    Then 回應狀態碼應該是 400
    And 錯誤代碼應該是 "PRD-B00004"
    And 錯誤訊息應該包含 "租戶指派不可變更"
