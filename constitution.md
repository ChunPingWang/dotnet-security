# Constitution - 開發準則憲章

> **版本**: 2.0  
> **建立日期**: 2026-01-04  
> **適用範圍**: 銀行客戶優惠規則及相關微服務開發

---

## 文件定位

本文件為「開發準則憲章」，定義開發標準與規範，回答「開發時應遵循什麼原則」。

**關聯文件：**
- **PRD.md** - 產品需求規格（做什麼）
- **TECH.md** - 技術實作規格（怎麼做）

```
PRD.md (What)
    │
    ▼
TECH.md (How) ──▶ 實作時必須遵循 constitution.md (Standards)
```

---

## 核心原則金字塔

```
                         ┌─────────┐
                         │  DDD    │  ← 領域驅動設計
                       ┌─┴─────────┴─┐
                       │   SOLID     │  ← 物件導向設計原則
                     ┌─┴─────────────┴─┐
                     │ Hexagonal Arch  │  ← 六角形架構
                   ┌─┴─────────────────┴─┐
                   │    TDD / BDD         │  ← 測試驅動開發
                 ┌─┴─────────────────────┴─┐
                 │     Code Quality         │  ← 程式碼品質
               └─────────────────────────────┘
```

---

## 1. 程式碼品質 (Code Quality)

### 1.1 命名規範

#### 套件命名

```
com.bank.discount
├── domain                    # 領域層
│   ├── model
│   │   ├── entity           # 實體
│   │   └── valueobject      # 值物件
│   ├── service              # 領域服務
│   └── exception            # 領域例外
├── application              # 應用層
│   ├── service              # 應用服務
│   ├── usecase              # 使用案例
│   └── port                 # 埠介面
│       ├── in               # 輸入埠
│       └── out              # 輸出埠
└── infrastructure           # 基礎設施層
    ├── adapter
    │   ├── in               # 輸入轉接器
    │   └── out              # 輸出轉接器
    └── config               # 配置
```

#### 類別與方法命名

| 類型 | 命名規則 | 範例 |
|------|----------|------|
| 類別 | PascalCase | `DiscountService`, `UsageRecord` |
| 介面 | PascalCase | `DiscountRuleEngine`, `UsageRecordRepository` |
| 方法 | camelCase，動詞開頭 | `evaluate()`, `findByKey()`, `consumeUsage()` |
| 常數 | UPPER_SNAKE_CASE | `MAX_RETRY_COUNT`, `DEFAULT_TIMEOUT` |
| 變數 | camelCase | `discountAmount`, `remainingCount` |
| 資料表 | UPPER_SNAKE_CASE | `DECISION_TREE`, `USAGE_COUNT_DTL` |
| 資料表欄位 | UPPER_SNAKE_CASE | `CIF_NO`, `CUSTOMER_ID` |

#### 資料字典命名規範

變數命名應組合以下詞彙：

**名詞**：`transaction`, `account`, `customer`, `discount`, `fee`, `amount`, `tree`, `node`, `factor`

**動詞**：`create`, `check`, `approve`, `evaluate`, `calculate`, `publish`, `upload`

**形容詞**：`available`, `used`, `total`, `remaining`, `valid`, `active`

### 1.2 程式碼風格

```java
// ✅ 正確：簡潔、有意義的命名
public class DiscountService {
    private final UsageRecordRepository repository;
    
    public DiscountResult evaluate(DiscountRequest request) {
        // 業務邏輯
    }
}

// ❌ 錯誤：冗長、模糊的命名
public class DiscountServiceImplementationClass {
    private final UsageRecordRepositoryInterface repo;
    
    public DiscountResultObject doEvaluateDiscountProcess(DiscountRequestObject req) {
        // ...
    }
}
```

### 1.3 註解規範

```java
/**
 * 優惠金額計算服務
 * 
 * <p>負責根據客戶條件評估並計算適用的優惠金額
 * 
 * @author Development Team
 * @since 1.0.0
 */
public class DiscountCalculator {
    
    /**
     * 計算固定收費類型的優惠金額
     * 
     * @param request 優惠請求
     * @param context 計算上下文
     * @return 優惠計算結果
     * @throws NoRemainingUsageException 當優惠次數已用完時
     */
    public DiscountResult calculateFixed(DiscountRequest request, CalculationContext context) {
        // ...
    }
}
```

---

## 2. SOLID 原則

### 2.1 Single Responsibility Principle (單一職責原則)

每個類別只應有一個改變的理由。

```java
// ✅ 正確：各司其職
public class DiscountEvaluator {
    // 僅負責評估優惠條件
    public boolean isEligible(DiscountRequest request) { ... }
}

public class DiscountCalculator {
    // 僅負責計算優惠金額
    public BigDecimal calculate(DiscountRequest request) { ... }
}

// ❌ 錯誤：職責混雜
public class DiscountManager {
    public boolean isEligible(DiscountRequest request) { ... }
    public BigDecimal calculate(DiscountRequest request) { ... }
    public void saveToDatabase(DiscountResult result) { ... }
    public void sendNotification(DiscountResult result) { ... }
}
```

### 2.2 Open-Closed Principle (開放封閉原則)

對擴展開放，對修改封閉。

```java
// ✅ 正確：透過介面擴展
public interface ChargeCalculator {
    BigDecimal calculate(DiscountContext context);
}

public class FixedChargeCalculator implements ChargeCalculator {
    @Override
    public BigDecimal calculate(DiscountContext context) { ... }
}

public class PercentageChargeCalculator implements ChargeCalculator {
    @Override
    public BigDecimal calculate(DiscountContext context) { ... }
}

// 新增階梯計算只需新增類別，不需修改既有程式碼
public class TieredChargeCalculator implements ChargeCalculator {
    @Override
    public BigDecimal calculate(DiscountContext context) { ... }
}
```

### 2.3 Liskov Substitution Principle (里氏替換原則)

子類別必須能夠替換其父類別。

```java
// ✅ 正確：子類別可完全替換父類別
public abstract class DiscountRule {
    public abstract boolean matches(DiscountRequest request);
    public abstract BigDecimal apply(DiscountRequest request);
}

public class FeeDiscountRule extends DiscountRule {
    @Override
    public boolean matches(DiscountRequest request) { ... }
    
    @Override
    public BigDecimal apply(DiscountRequest request) { ... }
}
```

### 2.4 Interface Segregation Principle (介面隔離原則)

不應強迫客戶端依賴於它們不使用的介面。

```java
// ✅ 正確：細粒度介面
public interface UsageRecordReader {
    Optional<UsageRecord> findByKey(UsageRecordKey key);
}

public interface UsageRecordWriter {
    void save(UsageRecord record);
}

// 組合介面
public interface UsageRecordRepository extends UsageRecordReader, UsageRecordWriter {
}
```

### 2.5 Dependency Inversion Principle (依賴反轉原則)

高層模組不應依賴低層模組，兩者都應依賴抽象。

```java
// ✅ 正確：依賴抽象

// Application Layer 定義介面 (Port)
public interface DiscountRuleEngine {
    DiscountResult evaluate(DiscountRequest request, DiscountContext context);
}

// Infrastructure Layer 實作介面 (Adapter)
@Component
public class DroolsRuleEngineAdapter implements DiscountRuleEngine {
    @Override
    public DiscountResult evaluate(DiscountRequest request, DiscountContext context) {
        // Drools 實作
    }
}

// Application Service 依賴介面，不依賴實作
@Service
public class DiscountServiceImpl implements DiscountService {
    private final DiscountRuleEngine ruleEngine; // 依賴介面
    
    public DiscountServiceImpl(DiscountRuleEngine ruleEngine) {
        this.ruleEngine = ruleEngine;
    }
}
```

---

## 3. 六角形架構原則 (Hexagonal Architecture)

### 3.1 核心概念

六角形架構（又稱 Ports and Adapters）將系統分為三層，確保領域邏輯與技術實作解耦。

```
┌─────────────────────────────────────────────────────────────────┐
│                     Infrastructure Layer                         │
│  (REST Controller, Database, External API, Drools, Excel Parser) │
│                              │                                   │
│                     implements Port                              │
│                              ▼                                   │
├─────────────────────────────────────────────────────────────────┤
│                      Application Layer                           │
│           (Application Service, Use Case, Port Interface)        │
│                              │                                   │
│                           依賴                                   │
│                              ▼                                   │
├─────────────────────────────────────────────────────────────────┤
│                        Domain Layer                              │
│    (Entity, Value Object, Domain Service, Domain Event)          │
│                     【最內層，無外部依賴】                         │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 分層依賴規則

| 依賴方向 | 允許 |
|----------|------|
| Infrastructure → Application | ✅ |
| Application → Domain | ✅ |
| Application → Infrastructure | ❌ |
| Domain → Application | ❌ |
| Domain → Infrastructure | ❌ |

### 3.3 分層職責

| 層級 | 職責 | 包含元件 |
|------|------|----------|
| Domain Layer | 核心業務邏輯、領域模型 | Entity, Value Object, Domain Service, Domain Event |
| Application Layer | 應用程式流程編排、Use Case | Application Service, Port Interface |
| Infrastructure Layer | 技術實作、外部整合 | Controller, Repository 實作, Adapter |

### 3.4 Port 與 Adapter 關係

```java
// Port (定義於 Application Layer)
public interface UsageRecordRepository {
    Optional<UsageRecord> findByKey(UsageRecordKey key);
    void save(UsageRecord record);
}

// Adapter (實作於 Infrastructure Layer)
@Repository
public class JpaUsageRecordRepository implements UsageRecordRepository {
    
    private final UsageRecordJpaRepository jpaRepository;
    
    @Override
    public Optional<UsageRecord> findByKey(UsageRecordKey key) {
        // JPA 實作
    }
    
    @Override
    public void save(UsageRecord record) {
        // JPA 實作
    }
}
```

### 3.5 Domain Layer 規範

**Domain Layer 禁止：**
- ❌ 使用 Spring 註解 (@Service, @Component, @Autowired)
- ❌ 依賴 JPA/Hibernate 註解 (@Entity, @Table)
- ❌ 使用任何框架特定的類別
- ❌ 直接存取資料庫或外部服務

**Domain Layer 應該：**
- ✅ 使用純 Java 類別
- ✅ 包含業務邏輯與驗證
- ✅ 定義領域事件
- ✅ 保持可測試性（不需 Spring Context）

---

## 4. 測試驅動開發 (TDD / BDD)

### 4.1 TDD 原則

```
┌─────────────────────────────────────────────────────────┐
│                    TDD 循環                              │
│                                                         │
│     ┌─────────┐                                         │
│     │  Red    │  1. 撰寫失敗的測試                       │
│     └────┬────┘                                         │
│          │                                              │
│          ▼                                              │
│     ┌─────────┐                                         │
│     │  Green  │  2. 撰寫最少程式碼使測試通過              │
│     └────┬────┘                                         │
│          │                                              │
│          ▼                                              │
│     ┌─────────┐                                         │
│     │Refactor │  3. 重構程式碼，保持測試通過             │
│     └────┬────┘                                         │
│          │                                              │
│          └──────────▶ 回到 Red                          │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 4.2 測試層級

| 測試類型 | 範圍 | 框架 |
|----------|------|------|
| 單元測試 | 單一類別/方法 | JUnit 5 + Mockito |
| 整合測試 | 多個元件協作 | Spring Boot Test + Testcontainers |
| BDD 測試 | 業務場景 | Cucumber |

### 4.3 單元測試規範

```java
@ExtendWith(MockitoExtension.class)
class DiscountCalculatorTest {
    
    @Mock
    private UsageRecordRepository usageRecordRepository;
    
    @InjectMocks
    private DiscountCalculator calculator;
    
    @Test
    @DisplayName("當剩餘次數大於0時，應計算優惠金額")
    void shouldCalculateDiscount_whenRemainingCountGreaterThanZero() {
        // Given
        var request = createDiscountRequest();
        var usageRecord = createUsageRecord(3); // 剩餘 3 次
        when(usageRecordRepository.findByKey(any())).thenReturn(Optional.of(usageRecord));
        
        // When
        var result = calculator.calculate(request);
        
        // Then
        assertThat(result.isEligible()).isTrue();
        assertThat(result.getDiscountAmount()).isEqualTo(new BigDecimal("100.00"));
    }
    
    @Test
    @DisplayName("當剩餘次數為0時，應拋出 NoRemainingUsageException")
    void shouldThrowException_whenNoRemainingUsage() {
        // Given
        var request = createDiscountRequest();
        var usageRecord = createUsageRecord(0); // 剩餘 0 次
        when(usageRecordRepository.findByKey(any())).thenReturn(Optional.of(usageRecord));
        
        // When & Then
        assertThatThrownBy(() -> calculator.calculate(request))
            .isInstanceOf(NoRemainingUsageException.class)
            .hasMessageContaining("剩餘次數不足");
    }
}
```

### 4.4 BDD 測試規範

#### Feature 檔案 (Gherkin 語法)

```gherkin
# language: zh-TW
功能: 優惠次數限制檢查

  場景大綱: 檢查客戶優惠剩餘次數
    假設 存在以下優惠使用紀錄:
      | CIF NO      | 客戶證號    | 整合優惠編號  | 指定通路 | 優惠次數 | 已使用次數 | 剩餘次數 |
      | 20250901002 | A123456789 | 20250922002  | BIP     | 3       | <已使用>   | <剩餘>   |
    當 客戶 "A123456789" 申請優惠
    那麼 剩餘次數應為 <剩餘>
    而且 次數限制判斷結果應為 "<結果>"

    例子:
      | 已使用 | 剩餘 | 結果     |
      | 0     | 3   | 可使用   |
      | 2     | 1   | 可使用   |
      | 3     | 0   | 已用完   |

  場景: 無次數限制的優惠
    假設 存在無次數限制的優惠方案 "VIP手續費減免"
    當 客戶 "A123456789" 申請該優惠
    那麼 應跳過次數檢查
    而且 直接進入金額計算
```

#### Step Definitions 命名規則

**重要規範：**
- ✅ Step 註解必須使用英文套件：`io.cucumber.java.en.*`（@Given, @When, @Then, @And）
- ❌ 禁止使用中文套件：`io.cucumber.java.zh_tw.*`（@假設, @當, @那麼, @而且, @並且）
- ✅ 註解內的 Step 文字可使用中文，方便對應 Feature 檔案

```java
// ✅ 正確：使用 io.cucumber.java.en.* 套件
import io.cucumber.java.en.Given;
import io.cucumber.java.en.When;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.And;

// ❌ 禁止：不可使用 io.cucumber.java.zh_tw.* 套件
// import io.cucumber.java.zh_tw.假設;
// import io.cucumber.java.zh_tw.當;
// import io.cucumber.java.zh_tw.那麼;
// import io.cucumber.java.zh_tw.而且;
```

```java
@CucumberContextConfiguration
@SpringBootTest
public class UsageLimitStepDefinitions {

    @Autowired
    private DiscountService discountService;

    @Autowired
    private UsageRecordRepository usageRecordRepository;

    private DiscountRequest request;
    private DiscountResult result;

    @Given("存在以下優惠使用紀錄:")  // 使用 @Given，Step 文字可用中文
    public void givenUsageRecords(DataTable dataTable) {  // 方法名稱用英文
        dataTable.asMaps().forEach(row -> {
            var record = UsageRecord.builder()
                .cifNo(row.get("CIF NO"))
                .customerId(row.get("客戶證號"))
                .discountPackageId(row.get("整合優惠編號"))
                .channel(Channel.valueOf(row.get("指定通路")))
                .totalUsageLimit(Integer.parseInt(row.get("優惠次數")))
                .usedCount(Integer.parseInt(row.get("已使用次數")))
                .remainingCount(Integer.parseInt(row.get("剩餘次數")))
                .build();
            usageRecordRepository.save(record);
        });
    }
    
    @當("客戶 {string} 申請優惠")
    public void 客戶申請優惠(String customerId) {
        request = DiscountRequest.builder()
            .customerId(customerId)
            .build();
        result = discountService.evaluate(request);
    }
    
    @那麼("剩餘次數應為 {int}")
    public void 剩餘次數應為(int expectedCount) {
        assertThat(result.getRemainingUsage()).isEqualTo(expectedCount);
    }
    
    @那麼("次數限制判斷結果應為 {string}")
    public void 次數限制判斷結果應為(String expectedResult) {
        assertThat(result.getUsageLimitStatus()).isEqualTo(expectedResult);
    }
}
```

---

## 5. 異常處理規範

### 5.1 錯誤代碼編碼規則

```
{組件代碼}-{大類代碼}{流水號}

範例: DIS-B00001
```

| 欄位 | 長度 | 說明 |
|------|------|------|
| 組件代碼 | 3 | 服務代碼，跨組件使用 `STD` |
| 分隔符號 | 1 | 固定 `-` |
| 大類代碼 | 1 | B=業務, S=系統, V=驗證 |
| 流水號 | 5 | 自 00001 起編 |

### 5.2 例外類別設計

```java
// Domain Exception 基底類別
public abstract class DomainException extends RuntimeException {
    private final String code;
    
    protected DomainException(String code, String message) {
        super(message);
        this.code = code;
    }
    
    public String getCode() {
        return code;
    }
}

// 具體 Domain Exception
public class NoRemainingUsageException extends DomainException {
    public NoRemainingUsageException(UsageRecordId id) {
        super("DIS-B00001", String.format("優惠次數已用完: %s", id));
    }
}

public class TreeNotFoundException extends DomainException {
    public TreeNotFoundException(String treeId) {
        super("DIS-B00006", String.format("決策樹不存在: %s", treeId));
    }
}
```

### 5.3 全域例外處理

```java
@RestControllerAdvice
public class GlobalExceptionHandler {
    
    @ExceptionHandler(DomainException.class)
    public ResponseEntity<ErrorResponse> handleDomainException(DomainException ex) {
        return ResponseEntity
            .badRequest()
            .body(new ErrorResponse(ex.getCode(), ex.getMessage()));
    }
    
    @ExceptionHandler(ValidationException.class)
    public ResponseEntity<ErrorResponse> handleValidationException(ValidationException ex) {
        return ResponseEntity
            .badRequest()
            .body(new ErrorResponse(ex.getCode(), ex.getMessage(), ex.getErrors()));
    }
    
    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponse> handleException(Exception ex) {
        log.error("Unexpected error", ex);
        return ResponseEntity
            .internalServerError()
            .body(new ErrorResponse("STD-S00001", "系統內部錯誤"));
    }
}
```

---

## 6. API 設計規範

### 6.1 RESTful API 原則

本專案採用 **Richardson Maturity Model Level 2**：
- 使用正確的 HTTP 方法 (GET, POST, PUT, DELETE)
- 使用有意義的 URL 路徑
- 使用適當的 HTTP 狀態碼

### 6.2 URL 設計原則

```
/{context-path}/{version}/{resource}/{resource-id}/{sub-resource}

範例:
POST /api/v1/discounts/evaluate
GET  /api/v1/trees/{id}/versions
POST /api/v1/trees/{id}/publish
```

### 6.3 HTTP 狀態碼

| 狀態碼 | 使用情境 |
|--------|----------|
| 200 | 請求成功 |
| 201 | 資源建立成功 |
| 400 | 請求參數錯誤、業務邏輯錯誤 |
| 401 | 未認證 |
| 403 | 無權限 |
| 404 | 資源不存在 |
| 500 | 系統內部錯誤 |

### 6.4 Response 格式

```json
{
  "success": true,
  "code": "0000",
  "message": "成功",
  "data": {
    // 回傳資料
  },
  "timestamp": "2026-01-04T12:00:00.000Z",
  "requestId": "550e8400-e29b-41d4-a716-446655440000"
}
```

---

## 7. 日誌規範

### 7.1 日誌等級

| Level | 使用時機 |
|-------|----------|
| ERROR | 非預期的業務邏輯錯誤或系統性錯誤 |
| WARN | 潛在問題或警告 |
| INFO | 重要業務事件 |
| DEBUG | 開發除錯資訊 |
| TRACE | 詳細追蹤資訊 |

### 7.2 日誌格式

```java
// ✅ 正確：使用參數化訊息
log.info("優惠評估完成 - 客戶:{}, 優惠金額:{}", customerId, discountAmount);

// ❌ 錯誤：字串拼接
log.info("優惠評估完成 - 客戶:" + customerId + ", 優惠金額:" + discountAmount);
```

### 7.3 MDC 使用

```java
try {
    MDC.put("requestId", requestId);
    MDC.put("customerId", customerId);
    MDC.put("treeId", treeId);
    
    // 業務邏輯
    
} finally {
    MDC.clear();
}
```

---

## 8. 版本控制規範

### 8.1 分支策略

```
main (正式環境)
  │
  ├── release/1.0.0 (UAT 環境)
  │     │
  │     └── develop (開發整合)
  │           │
  │           ├── feature/DIS-001-multi-tree-support
  │           ├── feature/DIS-002-excel-parser
  │           └── bugfix/DIS-003-calculation-error
```

### 8.2 Commit 訊息格式

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Type:**
- `feat`: 新功能
- `fix`: 錯誤修復
- `refactor`: 重構
- `test`: 測試
- `docs`: 文件
- `chore`: 雜務

**範例:**
```
feat(tree): 實作決策樹動態載入功能

- 新增 KieBaseRegistry 管理多個 KieBase
- 新增 RuleGenerator 生成 DRL
- 實作熱部署流程

Refs: DIS-001
```

---

## 9. CI/CD 規範

### 9.1 必要檢查項目

| 檢查項目 | 工具 | 標準 |
|----------|------|------|
| 編譯 | Gradle | 無錯誤 |
| 單元測試 | JUnit 5 | 100% 通過 |
| 測試覆蓋率 | JaCoCo | 行覆蓋率 ≥ 80% |
| 程式碼品質 | SonarQube | 無 Blocker/Critical |
| 安全掃描 | Checkmarx | 無高風險漏洞 |
| BDD 測試 | Cucumber | 100% 通過 |

### 9.2 建置指令

```bash
# 完整建置
./gradlew clean build

# 執行測試
./gradlew test

# 執行 BDD 測試
./gradlew cucumberTest

# 產生測試報告
./gradlew jacocoTestReport
```

---

## 附錄 A：開發檢查清單

### 新功能開發檢查清單

- [ ] 是否先撰寫 Feature 檔案 (BDD)？
- [ ] 是否先撰寫單元測試 (TDD)？
- [ ] 是否遵循六角形架構分層？
- [ ] Domain Layer 是否無框架依賴？
- [ ] 是否遵循 SOLID 原則？
- [ ] 是否使用通用語言命名？
- [ ] 測試覆蓋率是否達標？
- [ ] 是否有適當的例外處理？
- [ ] 是否有適當的日誌記錄？
- [ ] 程式碼是否通過 SonarQube 檢查？

### Code Review 檢查清單

- [ ] 分層依賴是否正確？
- [ ] 是否有適當的測試？
- [ ] 命名是否清晰？
- [ ] 是否有不必要的複雜度？
- [ ] 是否遵循 DRY 原則？
- [ ] 是否有適當的註解？
- [ ] 是否有適當的錯誤處理？
- [ ] 是否有潛在的效能問題？

---

## 附錄 B：變更紀錄

| 版本 | 日期 | 變更內容 |
|------|------|----------|
| 1.0 | 2026-01-04 | 初版建立 |
| 2.0 | 2026-01-04 | 重構：(1) 移除具體技術實作至 TECH.md (2) 精簡為開發原則與規範 (3) 明確文件定位與關聯 |

---

## 附錄 C：參考文件

| 文件 | 說明 |
|------|------|
| PRD.md | 產品需求規格書 |
| TECH.md | 技術規格書 |

---

> **本文件為專案開發的最高指導原則，所有開發人員必須嚴格遵守。**
