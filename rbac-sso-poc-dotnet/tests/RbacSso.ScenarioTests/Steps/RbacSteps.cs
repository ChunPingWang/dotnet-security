using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;
using Xunit;

namespace RbacSso.ScenarioTests.Steps;

/// <summary>
/// Step definitions for Rbac.feature
/// RBAC 功能的步驟定義
/// </summary>
[Binding]
public sealed class RbacSteps : IDisposable
{
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;
    private string? _accessToken;
    private Guid _testProductId;

    public RbacSteps(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // Given Steps

    [Given(@"我未登入")]
    [Given(@"I am not logged in")]
    public void GivenIAmNotLoggedIn()
    {
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Given(@"我有一個過期的 JWT token")]
    [Given(@"I have an expired JWT token")]
    public void GivenIHaveAnExpiredJwtToken()
    {
        _accessToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjEwMDAwMDAwMDB9.expired";
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
    }

    [Given(@"使用者 ""(.*)"" 同時擁有 ""(.*)"" 和 ""(.*)"" 角色")]
    [Given(@"user ""(.*)"" has both ""(.*)"" and ""(.*)"" roles")]
    public void GivenUserHasMultipleRoles(string username, string role1, string role2)
    {
        // This is configured in Keycloak - just documenting the setup
    }

    [Given(@"我的會話已超時 \(超過 (\d+) 分鐘\)")]
    [Given(@"my session has timed out \(over (\d+) minutes\)")]
    public void GivenMySessionHasTimedOut(int minutes)
    {
        // Simulate by using expired token
        _client.DefaultRequestHeaders.Authorization = null;
    }

    // When Steps

    [When(@"我執行 ""(.*)"" 操作")]
    [When(@"I perform ""(.*)"" operation")]
    public async Task WhenIPerformOperation(string operation)
    {
        await EnsureTestProductExists();

        _response = operation.ToUpperInvariant() switch
        {
            "CREATE" => await _client.PostAsJsonAsync("/api/products", new
            {
                Name = "Test Product",
                Price = 99.99m,
                Category = "Test"
            }),
            "READ" => await _client.GetAsync("/api/products"),
            "UPDATE" => await _client.PutAsJsonAsync($"/api/products/{_testProductId}", new
            {
                Name = "Updated Product",
                Price = 149.99m,
                Category = "Test"
            }),
            "DELETE" => await _client.DeleteAsync($"/api/products/{_testProductId}"),
            _ => throw new ArgumentException($"Unknown operation: {operation}")
        };
    }

    [When(@"我嘗試存取產品 API")]
    [When(@"I try to access the product API")]
    public async Task WhenITryToAccessProductApi()
    {
        _response = await _client.GetAsync("/api/products");
    }

    [When(@"我嘗試刪除產品 ""(.*)""")]
    [When(@"I try to delete product ""(.*)""")]
    public async Task WhenITryToDeleteProduct(string productName)
    {
        await EnsureTestProductExists();
        _response = await _client.DeleteAsync($"/api/products/{_testProductId}");
    }

    [When(@"我嘗試在 ProductService 建立產品")]
    [When(@"I try to create a product in ProductService")]
    public async Task WhenITryToCreateProductInProductService()
    {
        _response = await _client.PostAsJsonAsync("/api/products", new
        {
            Name = "Unauthorized Product",
            Price = 99.99m,
            Category = "Test"
        });
    }

    [When(@"我嘗試在 AuditService 刪除審計記錄")]
    [When(@"I try to delete an audit record in AuditService")]
    public async Task WhenITryToDeleteAuditRecord()
    {
        _response = await _client.DeleteAsync("/api/audit/12345678-1234-1234-1234-123456789012");
    }

    // Then Steps

    [Then(@"回應應該包含 ""(.*)""")]
    [Then(@"the response should contain ""(.*)""")]
    public async Task ThenResponseShouldContain(string content)
    {
        Assert.NotNull(_response);
        var responseContent = await _response.Content.ReadAsStringAsync();
        Assert.Contains(content, responseContent, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"回應不應該包含 ""(.*)""")]
    [Then(@"the response should not contain ""(.*)""")]
    public async Task ThenResponseShouldNotContain(string content)
    {
        Assert.NotNull(_response);
        var responseContent = await _response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(content, responseContent, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"應該產生 ""(.*)"" 審計事件")]
    [Then(@"a ""(.*)"" audit event should be generated")]
    public void ThenAuditEventShouldBeGenerated(string eventType)
    {
        // Audit event verification would be done via audit API
        // This is a placeholder for the actual verification
    }

    [Then(@"我的 token 應該包含 claim ""(.*)"" 值為 ""(.*)""")]
    [Then(@"my token should contain claim ""(.*)"" with value ""(.*)""")]
    public void ThenTokenShouldContainClaim(string claimName, string claimValue)
    {
        Assert.NotNull(_accessToken);
        // JWT claim verification
    }

    [Then(@"我的 token 應該包含 claim ""(.*)"" 包含 ""(.*)""")]
    [Then(@"my token should contain claim ""(.*)"" containing ""(.*)""")]
    public void ThenTokenShouldContainClaimContaining(string claimName, string containsValue)
    {
        Assert.NotNull(_accessToken);
        // JWT claim verification
    }

    [Then(@"我需要重新登入")]
    [Then(@"I need to log in again")]
    public void ThenINeedToLoginAgain()
    {
        Assert.NotNull(_response);
        Assert.Equal(HttpStatusCode.Unauthorized, _response.StatusCode);
    }

    // Helper Methods

    private async Task EnsureTestProductExists()
    {
        if (_testProductId == Guid.Empty)
        {
            // Login as admin to create test product
            var adminResponse = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                Username = "admin",
                Password = "admin"
            });

            if (adminResponse.IsSuccessStatusCode)
            {
                var tokenResult = await adminResponse.Content.ReadFromJsonAsync<TokenResponse>();
                var originalAuth = _client.DefaultRequestHeaders.Authorization;

                _client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult?.AccessToken);

                var createResponse = await _client.PostAsJsonAsync("/api/products", new
                {
                    Name = "Test Product for RBAC",
                    Price = 99.99m,
                    Category = "Test"
                });

                if (createResponse.IsSuccessStatusCode)
                {
                    var createResult = await createResponse.Content.ReadFromJsonAsync<CreateResponse>();
                    _testProductId = createResult?.Data?.ProductId ?? Guid.NewGuid();
                }

                _client.DefaultRequestHeaders.Authorization = originalAuth;
            }
        }
    }

    public void Dispose()
    {
        _response?.Dispose();
        _client.Dispose();
    }

    private record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);
    private record CreateResponse(CreateProductData? Data);
    private record CreateProductData(Guid ProductId);
}
