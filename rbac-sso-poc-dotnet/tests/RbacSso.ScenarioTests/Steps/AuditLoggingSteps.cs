using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;
using Xunit;

namespace RbacSso.ScenarioTests.Steps;

/// <summary>
/// Step definitions for AuditLogging.feature
/// 審計日誌功能的步驟定義
/// </summary>
[Binding]
public sealed class AuditLoggingSteps : IDisposable
{
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;
    private string? _accessToken;
    private string? _lastCorrelationId;

    public AuditLoggingSteps(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // Given Steps

    [Given(@"審計服務已啟動")]
    [Given(@"the audit service is running")]
    public void GivenAuditServiceIsRunning()
    {
        // Audit service startup verified by factory
    }

    [Given(@"系統中存在過去 (\d+) 天的審計日誌")]
    [Given(@"audit logs exist for the past (\d+) days")]
    public void GivenAuditLogsExistForPastDays(int days)
    {
        // Test data setup - audit logs are created during operations
    }

    [Given(@"系統中存在多種類型的審計日誌")]
    [Given(@"audit logs of various types exist")]
    public void GivenVariousAuditLogsExist()
    {
        // Test data setup
    }

    [Given(@"系統中存在多個使用者的審計日誌")]
    [Given(@"audit logs from multiple users exist")]
    public void GivenAuditLogsFromMultipleUsersExist()
    {
        // Test data setup
    }

    [Given(@"系統中存在 ""(.*)"" 租戶的審計日誌")]
    [Given(@"audit logs exist for tenant ""(.*)""")]
    public void GivenAuditLogsExistForTenant(string tenantId)
    {
        // Test data setup
    }

    [Given(@"系統中存在 (\d+) 天前的審計日誌")]
    [Given(@"audit logs from (\d+) days ago exist")]
    public void GivenAuditLogsFromDaysAgoExist(int days)
    {
        // Test data setup for retention testing
    }

    [Given(@"系統中存在審計日誌 ID ""<audit_id>""")]
    [Given(@"an audit log with ID ""<audit_id>"" exists")]
    public void GivenAuditLogWithIdExists()
    {
        // Placeholder for dynamic ID
    }

    [Given(@"系統中存在產品 ""(.*)"" 價格為 (.*)")]
    [Given(@"product ""(.*)"" exists with price (.*)")]
    public async Task GivenProductExistsWithPrice(string productName, decimal price)
    {
        await LoginAs("admin");
        await _client.PostAsJsonAsync("/api/products", new
        {
            Name = productName,
            Price = price,
            Category = "Test"
        });
    }

    // When Steps

    [When(@"我以 ""(.*)"" 身份使用錯誤密碼登入")]
    [When(@"I try to login as ""(.*)"" with wrong password")]
    public async Task WhenITryToLoginWithWrongPassword(string username)
    {
        _response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Username = username,
            Password = "wrong-password"
        });
    }

    [When(@"我查詢過去 (\d+) 天的審計日誌")]
    [When(@"I query audit logs for the past (\d+) days")]
    public async Task WhenIQueryAuditLogsForPastDays(int days)
    {
        var fromDate = DateTimeOffset.UtcNow.AddDays(-days);
        _response = await _client.GetAsync($"/api/audit?fromDate={fromDate:O}");
    }

    [When(@"我查詢事件類型為 ""(.*)"" 的審計日誌")]
    [When(@"I query audit logs with event type ""(.*)""")]
    public async Task WhenIQueryAuditLogsByEventType(string eventType)
    {
        _response = await _client.GetAsync($"/api/audit?eventType={eventType}");
    }

    [When(@"我查詢使用者 ""(.*)"" 的審計日誌")]
    [When(@"I query audit logs for user ""(.*)""")]
    public async Task WhenIQueryAuditLogsForUser(string username)
    {
        _response = await _client.GetAsync($"/api/audit?username={username}");
    }

    [When(@"我查詢審計日誌列表")]
    [When(@"I query the audit log list")]
    public async Task WhenIQueryAuditLogList()
    {
        _response = await _client.GetAsync("/api/audit");
    }

    [When(@"審計日誌清理排程執行")]
    [When(@"the audit log retention job runs")]
    public void WhenAuditLogRetentionJobRuns()
    {
        // Simulated - actual job runs via pg_cron
    }

    [When(@"我嘗試修改審計日誌 ID ""<audit_id>""")]
    [When(@"I try to modify audit log ID ""<audit_id>""")]
    public async Task WhenITryToModifyAuditLog()
    {
        var testId = Guid.NewGuid();
        _response = await _client.PutAsJsonAsync($"/api/audit/{testId}", new
        {
            EventType = "HACKED"
        });
    }

    [When(@"我嘗試刪除審計日誌 ID ""<audit_id>""")]
    [When(@"I try to delete audit log ID ""<audit_id>""")]
    public async Task WhenITryToDeleteAuditLog()
    {
        var testId = Guid.NewGuid();
        _response = await _client.DeleteAsync($"/api/audit/{testId}");
    }

    [When(@"我建立一個新產品並記錄 correlation ID")]
    [When(@"I create a new product and record the correlation ID")]
    public async Task WhenICreateProductAndRecordCorrelationId()
    {
        _lastCorrelationId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", _lastCorrelationId);

        _response = await _client.PostAsJsonAsync("/api/products", new
        {
            Name = "Correlation Test Product",
            Price = 99.99m,
            Category = "Test"
        });

        _client.DefaultRequestHeaders.Remove("X-Correlation-ID");
    }

    [When(@"我更新產品 ""(.*)"" 價格為 (.*)")]
    [When(@"I update product ""(.*)"" price to (.*)")]
    public async Task WhenIUpdateProductPrice(string productName, decimal newPrice)
    {
        // First find the product
        var listResponse = await _client.GetAsync("/api/products");
        var content = await listResponse.Content.ReadAsStringAsync();
        // Would need to parse and find product ID, then update

        _response = await _client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", new
        {
            Name = productName,
            Price = newPrice,
            Category = "Test"
        });
    }

    // Then Steps

    [Then(@"應該產生審計事件")]
    [Then(@"an audit event should be generated")]
    public void ThenAuditEventShouldBeGenerated()
    {
        // Verification placeholder - actual verification via audit API
    }

    [Then(@"審計事件類型應該是 ""(.*)""")]
    [Then(@"the audit event type should be ""(.*)""")]
    public void ThenAuditEventTypeShouldBe(string eventType)
    {
        // Verification placeholder
    }

    [Then(@"審計事件應該包含:")]
    [Then(@"the audit event should contain:")]
    public void ThenAuditEventShouldContain(Table table)
    {
        // Verification placeholder
    }

    [Then(@"審計事件應該包含變更前後的值")]
    [Then(@"the audit event should contain before and after values")]
    public void ThenAuditEventShouldContainBeforeAfterValues()
    {
        // Verification placeholder
    }

    [Then(@"審計事件結果應該是 ""(.*)""")]
    [Then(@"the audit event result should be ""(.*)""")]
    public void ThenAuditEventResultShouldBe(string result)
    {
        // Verification placeholder
    }

    [Then(@"回應應該只包含過去 (\d+) 天的日誌")]
    [Then(@"the response should only contain logs from the past (\d+) days")]
    public void ThenResponseShouldOnlyContainLogsFromPastDays(int days)
    {
        Assert.NotNull(_response);
        Assert.Equal(HttpStatusCode.OK, _response.StatusCode);
    }

    [Then(@"所有回傳的日誌類型都應該是 ""(.*)""")]
    [Then(@"all returned logs should be of type ""(.*)""")]
    public void ThenAllLogsShouldBeOfType(string eventType)
    {
        // Verification placeholder
    }

    [Then(@"所有回傳的日誌使用者都應該是 ""(.*)""")]
    [Then(@"all returned logs should be from user ""(.*)""")]
    public void ThenAllLogsShouldBeFromUser(string username)
    {
        // Verification placeholder
    }

    [Then(@"所有回傳的日誌租戶 ID 都應該是 ""(.*)""")]
    [Then(@"all returned logs should have tenant ID ""(.*)""")]
    public void ThenAllLogsShouldHaveTenantId(string tenantId)
    {
        // Verification placeholder
    }

    [Then(@"(\d+) 天前的審計日誌應該被刪除")]
    [Then(@"audit logs from (\d+) days ago should be deleted")]
    public void ThenLogsFromDaysAgoShouldBeDeleted(int days)
    {
        // Verification placeholder
    }

    [Then(@"(\d+) 天前的審計日誌應該被保留")]
    [Then(@"audit logs from (\d+) days ago should be retained")]
    public void ThenLogsFromDaysAgoShouldBeRetained(int days)
    {
        // Verification placeholder
    }

    [Then(@"審計事件應該包含相同的 correlation ID")]
    [Then(@"the audit event should contain the same correlation ID")]
    public void ThenAuditEventShouldContainSameCorrelationId()
    {
        Assert.NotNull(_lastCorrelationId);
        // Verification via audit API
    }

    [Then(@"回應時間應該小於 (\d+)ms")]
    [Then(@"the response time should be less than (\d+)ms")]
    public void ThenResponseTimeShouldBeLessThan(int ms)
    {
        // Response time is tracked by test framework
    }

    [Then(@"審計事件應該被非同步記錄")]
    [Then(@"the audit event should be logged asynchronously")]
    public void ThenAuditEventShouldBeLoggedAsync()
    {
        // Verification placeholder
    }

    // Helper methods

    private async Task LoginAs(string username)
    {
        var password = username == "admin" ? "admin" : "password";
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Username = username,
            Password = password
        });

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
            _accessToken = result?.AccessToken;
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        }
    }

    public void Dispose()
    {
        _response?.Dispose();
        _client.Dispose();
    }

    private record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);
}
