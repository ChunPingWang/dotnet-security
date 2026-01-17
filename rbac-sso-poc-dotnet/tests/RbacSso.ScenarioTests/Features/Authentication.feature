# language: zh-TW
@US1 @Authentication @P0
功能: 單一登入認證
  作為 系統使用者
  我想要 透過組織的身份提供者進行一次登入
  以便 存取平台功能而無需管理個別帳戶密碼

  背景:
    假設 Keycloak 身份提供者已啟動並可用
    而且 用戶端 "gateway" 已在 Keycloak 中設定

  @smoke
  場景: 成功登入取得 JWT Token
    假設 一個使用者 "admin" 在 Keycloak 中擁有有效憑證
    而且 該使用者擁有角色 "ADMIN"
    而且 該使用者屬於租戶 "tenant-a"
    當 使用者透過平台發起登入
    那麼 使用者被重新導向到身份提供者
    當 使用者在身份提供者完成認證
    那麼 使用者收到有效的 JWT Token
    而且 Token 包含 claim "preferred_username" 值為 "admin"
    而且 Token 包含 claim "tenant_id" 值為 "tenant-a"
    而且 Token 包含角色 "ADMIN"

  場景: SSO 跨應用程式自動登入
    假設 使用者 "admin" 已在另一個使用相同身份提供者的應用程式認證
    當 使用者存取此平台
    那麼 使用者自動登入無需重新輸入憑證
    而且 使用者收到有效的 JWT Token

  場景: Token 過期自動刷新
    假設 使用者 "admin" 擁有有效的認證會話
    而且 存取 Token 即將過期
    當 系統偵測到 Token 即將過期
    那麼 系統自動刷新 Token 而不中斷使用者
    而且 使用者收到新的有效 JWT Token

  場景: 登入失敗顯示錯誤訊息
    假設 一個使用者 "unknown" 在 Keycloak 中沒有有效憑證
    當 使用者嘗試登入
    那麼 使用者收到清楚的錯誤訊息
    而且 使用者仍停留在登入頁面
    而且 登入失敗被記錄到稽核日誌

  @LDAP
  場景: LDAP 使用者同步後登入
    假設 使用者 "ldap-user" 存在於 OpenLDAP 目錄
    而且 Keycloak 已設定 LDAP 聯盟
    當 使用者透過 LDAP 憑證登入
    那麼 使用者成功認證並收到 JWT Token
    而且 Token 包含從 LDAP 同步的 tenant_id claim
