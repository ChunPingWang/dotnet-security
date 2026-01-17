@product @us2
Feature: Product Management
  產品管理功能
  As a user with appropriate permissions
  I want to manage products
  So that I can maintain the product catalog

  Background:
    Given 系統已啟動
    And Keycloak 認證服務已連線

  @create @happy-path
  Scenario: ADMIN can create a product
    # ADMIN 可以建立產品
    Given 我以 "admin" 身份登入
    When 我建立一個新產品:
      | Name       | Price  | Category    | Description          |
      | Test Phone | 999.99 | Electronics | A test smartphone    |
    Then 回應狀態碼應該是 201
    And 產品應該被成功建立
    And 產品應該屬於 "system" 租戶

  @create @happy-path
  Scenario: TENANT_ADMIN can create a product for their tenant
    # TENANT_ADMIN 可以為其租戶建立產品
    Given 我以 "tenant-a-admin" 身份登入
    When 我建立一個新產品:
      | Name        | Price  | Category    | Description      |
      | Test Laptop | 1499.99| Electronics | A test laptop    |
    Then 回應狀態碼應該是 201
    And 產品應該屬於 "tenant-a" 租戶

  @create @happy-path
  Scenario: USER can create a product
    # USER 可以建立產品
    Given 我以 "user-a" 身份登入
    When 我建立一個新產品:
      | Name       | Price | Category | Description     |
      | Test Mouse | 29.99 | Accessories | A test mouse |
    Then 回應狀態碼應該是 201
    And 產品應該屬於 "tenant-a" 租戶

  @create @negative
  Scenario: VIEWER cannot create a product
    # VIEWER 無法建立產品
    Given 我以 "viewer" 身份登入
    When 我建立一個新產品:
      | Name       | Price | Category | Description |
      | Test Item  | 9.99  | Other    | A test item |
    Then 回應狀態碼應該是 403
    And 錯誤訊息應該包含 "Forbidden"

  @read @happy-path
  Scenario: All authenticated users can read products
    # 所有已認證使用者都可以讀取產品
    Given 系統中存在產品 "Existing Product"
    And 我以 "viewer" 身份登入
    When 我查詢產品列表
    Then 回應狀態碼應該是 200
    And 回應應該包含產品列表

  @update @happy-path
  Scenario: TENANT_ADMIN can update their tenant's product
    # TENANT_ADMIN 可以更新其租戶的產品
    Given 系統中存在 "tenant-a" 租戶的產品 "Product A"
    And 我以 "tenant-a-admin" 身份登入
    When 我更新產品 "Product A":
      | Name          | Price  |
      | Product A v2  | 199.99 |
    Then 回應狀態碼應該是 200
    And 產品名稱應該是 "Product A v2"

  @update @negative
  Scenario: VIEWER cannot update a product
    # VIEWER 無法更新產品
    Given 系統中存在產品 "Read Only Product"
    And 我以 "viewer" 身份登入
    When 我更新產品 "Read Only Product":
      | Name           |
      | Hacked Product |
    Then 回應狀態碼應該是 403

  @delete @happy-path
  Scenario: ADMIN can delete any product
    # ADMIN 可以刪除任何產品
    Given 系統中存在產品 "Product to Delete"
    And 我以 "admin" 身份登入
    When 我刪除產品 "Product to Delete"
    Then 回應狀態碼應該是 204
    And 產品應該被標記為已刪除

  @delete @happy-path
  Scenario: TENANT_ADMIN can delete their tenant's product
    # TENANT_ADMIN 可以刪除其租戶的產品
    Given 系統中存在 "tenant-a" 租戶的產品 "Tenant Product"
    And 我以 "tenant-a-admin" 身份登入
    When 我刪除產品 "Tenant Product"
    Then 回應狀態碼應該是 204

  @delete @negative
  Scenario: USER cannot delete a product
    # USER 無法刪除產品
    Given 系統中存在產品 "Protected Product"
    And 我以 "user-a" 身份登入
    When 我刪除產品 "Protected Product"
    Then 回應狀態碼應該是 403

  @pagination @happy-path
  Scenario: Products can be paginated
    # 產品可以分頁
    Given 系統中存在 25 個產品
    And 我以 "admin" 身份登入
    When 我查詢產品列表，每頁 10 筆
    Then 回應狀態碼應該是 200
    And 回應應該包含 10 個產品
    And 總數應該是 25

  @filter @happy-path
  Scenario: Products can be filtered by category
    # 產品可以依類別篩選
    Given 系統中存在以下產品:
      | Name     | Category    |
      | Phone 1  | Electronics |
      | Phone 2  | Electronics |
      | Shirt 1  | Clothing    |
    And 我以 "admin" 身份登入
    When 我查詢類別為 "Electronics" 的產品
    Then 回應狀態碼應該是 200
    And 回應應該包含 2 個產品

  @sort @happy-path
  Scenario: Products can be sorted by price
    # 產品可以依價格排序
    Given 系統中存在以下產品:
      | Name     | Price  |
      | Cheap    | 10.00  |
      | Medium   | 50.00  |
      | Expensive| 100.00 |
    And 我以 "admin" 身份登入
    When 我查詢產品列表，依 "price" 降序排列
    Then 回應狀態碼應該是 200
    And 第一個產品應該是 "Expensive"

  @validation @negative
  Scenario: Cannot create product with invalid price
    # 無法建立價格無效的產品
    Given 我以 "admin" 身份登入
    When 我建立一個新產品:
      | Name        | Price  | Category |
      | Bad Product | -10.00 | Other    |
    Then 回應狀態碼應該是 400
    And 錯誤代碼應該是 "PRD-V00001"
