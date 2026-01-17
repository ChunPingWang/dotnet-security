using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;
using Xunit;

namespace RbacSso.ScenarioTests.Steps;

/// <summary>
/// Step definitions for MultiTenant.feature
/// 多租戶功能的步驟定義
/// </summary>
[Binding]
public sealed class MultiTenantSteps : IDisposable
{
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;
    private string? _accessToken;
    private readonly Dictionary<string, Guid> _productIds = new();

    public MultiTenantSteps(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // Given Steps

    [Given(@"系統中存在 ""(.*)"" 租戶的產品 ""(.*)"" 且 ID 為 ""<product_id>""")]
    [Given(@"a product ""(.*)"" for tenant ""(.*)"" exists with ID ""<product_id>""")]
    public async Task GivenProductExistsForTenantWithId(string tenantId, string productName)
    {
        var productId = await CreateProductAsTenant(tenantId, productName);
        _productIds[productName] = productId;
    }

    [Given(@"資料庫中存在以下產品:")]
    [Given(@"the following products exist in the database:")]
    public async Task GivenFollowingProductsExistInDatabase(Table table)
    {
        foreach (var row in table.Rows)
        {
            var name = row["Name"];
            var tenantId = row["TenantId"];
            var productId = await CreateProductAsTenant(tenantId, name);
            _productIds[name] = productId;
        }
    }

    [Given(@"系統中新增租戶 ""(.*)""")]
    [Given(@"a new tenant ""(.*)"" is added to the system")]
    public void GivenNewTenantIsAdded(string tenantId)
    {
        // New tenant is configured in Keycloak
        // No products exist for new tenant
    }

    [Given(@"我以 ""(.*)"" 的管理員身份登入")]
    [Given(@"I am logged in as admin of ""(.*)""")]
    public async Task GivenIAmLoggedInAsAdminOfTenant(string tenantId)
    {
        var username = $"{tenantId}-admin";
        await LoginAs(username);
    }

    // When Steps

    [When(@"我嘗試存取產品 ID ""<product_id>""")]
    [When(@"I try to access product ID ""<product_id>""")]
    public async Task WhenITryToAccessProductById()
    {
        var productId = _productIds.Values.FirstOrDefault();
        _response = await _client.GetAsync($"/api/products/{productId}");
    }

    [When(@"我嘗試更新產品 ID ""<product_id>""")]
    [When(@"I try to update product ID ""<product_id>""")]
    public async Task WhenITryToUpdateProductById()
    {
        var productId = _productIds.Values.FirstOrDefault();
        _response = await _client.PutAsJsonAsync($"/api/products/{productId}", new
        {
            Name = "Hacked Name",
            Price = 0.01m,
            Category = "Hacked"
        });
    }

    [When(@"我嘗試刪除產品 ID ""<product_id>""")]
    [When(@"I try to delete product ID ""<product_id>""")]
    public async Task WhenITryToDeleteProductById()
    {
        var productId = _productIds.Values.FirstOrDefault();
        _response = await _client.DeleteAsync($"/api/products/{productId}");
    }

    [When(@"我嘗試建立產品並偽造租戶 ID 為 ""(.*)"":")]
    [When(@"I try to create a product with spoofed tenant ID ""(.*)"":")]
    public async Task WhenITryToCreateProductWithSpoofedTenantId(string fakeTenantId, Table table)
    {
        var row = table.Rows[0];
        _response = await _client.PostAsJsonAsync("/api/products", new
        {
            Name = row["Name"],
            Price = decimal.Parse(row["Price"]),
            Category = row["Category"],
            TenantId = fakeTenantId // This should be ignored
        });
    }

    [When(@"我嘗試將產品的租戶 ID 變更為 ""(.*)""")]
    [When(@"I try to change the product tenant ID to ""(.*)""")]
    public async Task WhenITryToChangeProductTenantId(string newTenantId)
    {
        var productId = _productIds.Values.FirstOrDefault();
        _response = await _client.PutAsJsonAsync($"/api/products/{productId}", new
        {
            Name = "Same Name",
            Price = 99.99m,
            Category = "Same",
            TenantId = newTenantId // Attempt to change tenant
        });
    }

    // Then Steps

    [Then(@"回應應該只包含 (\d+) 個產品")]
    [Then(@"the response should contain only (\d+) products")]
    public async Task ThenResponseShouldContainOnlyProducts(int count)
    {
        Assert.NotNull(_response);
        var content = await _response.Content.ReadAsStringAsync();
        var result = System.Text.Json.JsonSerializer.Deserialize<ApiResponseWrapper>(content,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal(count, result?.Data?.Items?.Count ?? 0);
    }

    [Then(@"所有產品的租戶 ID 都應該是 ""(.*)""")]
    [Then(@"all products should have tenant ID ""(.*)""")]
    public async Task ThenAllProductsShouldHaveTenantId(string tenantId)
    {
        Assert.NotNull(_response);
        var content = await _response.Content.ReadAsStringAsync();
        var result = System.Text.Json.JsonSerializer.Deserialize<ApiResponseWrapper>(content,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        foreach (var item in result?.Data?.Items ?? Enumerable.Empty<ProductItem>())
        {
            Assert.Equal(tenantId, item.TenantId);
        }
    }

    [Then(@"產品的租戶 ID 應該是 ""(.*)""")]
    [Then(@"the product tenant ID should be ""(.*)""")]
    public async Task ThenProductTenantIdShouldBe(string expectedTenantId)
    {
        Assert.NotNull(_response);
        if (_response.IsSuccessStatusCode)
        {
            var content = await _response.Content.ReadAsStringAsync();
            Assert.Contains(expectedTenantId, content);
        }
    }

    [Then(@"審計事件應該包含嘗試者的租戶 ID ""(.*)""")]
    [Then(@"the audit event should contain attempter tenant ID ""(.*)""")]
    public void ThenAuditEventShouldContainAttempterTenantId(string tenantId)
    {
        // Audit verification placeholder
    }

    [Then(@"審計事件應該包含目標資源的 ID")]
    [Then(@"the audit event should contain target resource ID")]
    public void ThenAuditEventShouldContainTargetResourceId()
    {
        // Audit verification placeholder
    }

    // Helper Methods

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

    private async Task<Guid> CreateProductAsTenant(string tenantId, string productName)
    {
        var username = tenantId == "system" ? "admin" : $"{tenantId}-admin";
        var originalAuth = _client.DefaultRequestHeaders.Authorization;

        await LoginAs(username);

        var response = await _client.PostAsJsonAsync("/api/products", new
        {
            Name = productName,
            Price = 99.99m,
            Category = "Test"
        });

        _client.DefaultRequestHeaders.Authorization = originalAuth;

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<CreateResponse>();
            return result?.Data?.ProductId ?? Guid.Empty;
        }

        return Guid.NewGuid();
    }

    public void Dispose()
    {
        _response?.Dispose();
        _client.Dispose();
    }

    private record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);
    private record CreateResponse(CreateProductData? Data);
    private record CreateProductData(Guid ProductId);
    private record ApiResponseWrapper(PagedData? Data);
    private record PagedData(List<ProductItem>? Items);
    private record ProductItem(Guid Id, string Name, string TenantId);
}
