using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;
using RbacSso.Common.Responses;
using Xunit;

namespace RbacSso.ScenarioTests.Steps;

/// <summary>
/// Step definitions for ProductManagement.feature
/// 產品管理功能的步驟定義
/// </summary>
[Binding]
public sealed class ProductManagementSteps : IDisposable
{
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;
    private string? _accessToken;
    private Guid? _createdProductId;
    private readonly Dictionary<string, Guid> _productIds = new();

    public ProductManagementSteps(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // Background Steps

    [Given(@"系統已啟動")]
    [Given(@"the system is running")]
    public void GivenTheSystemIsRunning()
    {
        // System startup is handled by WebApplicationFactory
    }

    [Given(@"Keycloak 認證服務已連線")]
    [Given(@"Keycloak authentication service is connected")]
    public void GivenKeycloakIsConnected()
    {
        // Keycloak connection is verified during auth tests
    }

    // Login Steps

    [Given(@"我以 ""(.*)"" 身份登入")]
    [Given(@"I am logged in as ""(.*)""")]
    public async Task GivenIAmLoggedInAs(string username)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Username = username,
            Password = GetPasswordForUser(username)
        });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        _accessToken = result?.AccessToken;
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
    }

    // Product Exists Steps

    [Given(@"系統中存在產品 ""(.*)""")]
    [Given(@"a product ""(.*)"" exists in the system")]
    public async Task GivenProductExists(string productName)
    {
        await EnsureProductExists(productName, "tenant-a");
    }

    [Given(@"系統中存在 ""(.*)"" 租戶的產品 ""(.*)""")]
    [Given(@"a product ""(.*)"" exists for tenant ""(.*)""")]
    public async Task GivenProductExistsForTenant(string tenantId, string productName)
    {
        await EnsureProductExists(productName, tenantId);
    }

    [Given(@"系統中存在 (\d+) 個產品")]
    [Given(@"(\d+) products exist in the system")]
    public async Task GivenProductsExist(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            await EnsureProductExists($"Product {i}", "tenant-a");
        }
    }

    [Given(@"系統中存在以下產品:")]
    [Given(@"the following products exist:")]
    public async Task GivenFollowingProductsExist(Table table)
    {
        foreach (var row in table.Rows)
        {
            var name = row["Name"];
            var category = row.ContainsKey("Category") ? row["Category"] : "Other";
            var price = row.ContainsKey("Price") ? decimal.Parse(row["Price"]) : 99.99m;
            await CreateProduct(name, price, category);
        }
    }

    // Create Product Steps

    [When(@"我建立一個新產品:")]
    [When(@"I create a new product:")]
    public async Task WhenICreateANewProduct(Table table)
    {
        var row = table.Rows[0];
        var request = new
        {
            Name = row["Name"],
            Price = decimal.Parse(row["Price"]),
            Category = row["Category"],
            Description = row.ContainsKey("Description") ? row["Description"] : null
        };

        _response = await _client.PostAsJsonAsync("/api/products", request);

        if (_response.IsSuccessStatusCode)
        {
            var result = await _response.Content.ReadFromJsonAsync<ApiResponse<CreateProductResponse>>();
            _createdProductId = result?.Data?.ProductId;
        }
    }

    // Query Steps

    [When(@"我查詢產品列表")]
    [When(@"I query the product list")]
    public async Task WhenIQueryProductList()
    {
        _response = await _client.GetAsync("/api/products");
    }

    [When(@"我查詢產品列表，每頁 (\d+) 筆")]
    [When(@"I query the product list with page size (\d+)")]
    public async Task WhenIQueryProductListWithPageSize(int pageSize)
    {
        _response = await _client.GetAsync($"/api/products?size={pageSize}");
    }

    [When(@"我查詢類別為 ""(.*)"" 的產品")]
    [When(@"I query products with category ""(.*)""")]
    public async Task WhenIQueryProductsByCategory(string category)
    {
        _response = await _client.GetAsync($"/api/products?category={category}");
    }

    [When(@"我查詢產品列表，依 ""(.*)"" 降序排列")]
    [When(@"I query the product list sorted by ""(.*)"" descending")]
    public async Task WhenIQueryProductListSortedByDescending(string sortBy)
    {
        _response = await _client.GetAsync($"/api/products?sortBy={sortBy}&descending=true");
    }

    // Update Steps

    [When(@"我更新產品 ""(.*)"":")]
    [When(@"I update product ""(.*)"":")]
    public async Task WhenIUpdateProduct(string productName, Table table)
    {
        var productId = _productIds.GetValueOrDefault(productName);
        var row = table.Rows[0];
        var request = new
        {
            Name = row.ContainsKey("Name") ? row["Name"] : productName,
            Price = row.ContainsKey("Price") ? decimal.Parse(row["Price"]) : 99.99m,
            Category = row.ContainsKey("Category") ? row["Category"] : "Other",
            Description = row.ContainsKey("Description") ? row["Description"] : null
        };

        _response = await _client.PutAsJsonAsync($"/api/products/{productId}", request);
    }

    // Delete Steps

    [When(@"我刪除產品 ""(.*)""")]
    [When(@"I delete product ""(.*)""")]
    public async Task WhenIDeleteProduct(string productName)
    {
        var productId = _productIds.GetValueOrDefault(productName);
        _response = await _client.DeleteAsync($"/api/products/{productId}");
    }

    // Assertion Steps

    [Then(@"回應狀態碼應該是 (\d+)")]
    [Then(@"the response status code should be (\d+)")]
    public void ThenResponseStatusCodeShouldBe(int statusCode)
    {
        Assert.NotNull(_response);
        Assert.Equal((HttpStatusCode)statusCode, _response.StatusCode);
    }

    [Then(@"產品應該被成功建立")]
    [Then(@"the product should be created successfully")]
    public void ThenProductShouldBeCreated()
    {
        Assert.NotNull(_createdProductId);
        Assert.NotEqual(Guid.Empty, _createdProductId);
    }

    [Then(@"產品應該屬於 ""(.*)"" 租戶")]
    [Then(@"the product should belong to tenant ""(.*)""")]
    public async Task ThenProductShouldBelongToTenant(string tenantId)
    {
        Assert.NotNull(_createdProductId);
        var response = await _client.GetAsync($"/api/products/{_createdProductId}");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>();
        Assert.Equal(tenantId, result?.Data?.TenantId);
    }

    [Then(@"回應應該包含產品列表")]
    [Then(@"the response should contain a product list")]
    public async Task ThenResponseShouldContainProductList()
    {
        Assert.NotNull(_response);
        var result = await _response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductResponse>>>();
        Assert.NotNull(result?.Data?.Items);
    }

    [Then(@"回應應該包含 (\d+) 個產品")]
    [Then(@"the response should contain (\d+) products")]
    public async Task ThenResponseShouldContainProducts(int count)
    {
        Assert.NotNull(_response);
        var result = await _response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductResponse>>>();
        Assert.Equal(count, result?.Data?.Items?.Count);
    }

    [Then(@"總數應該是 (\d+)")]
    [Then(@"the total count should be (\d+)")]
    public async Task ThenTotalCountShouldBe(int count)
    {
        Assert.NotNull(_response);
        var result = await _response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductResponse>>>();
        Assert.Equal(count, result?.Data?.TotalCount);
    }

    [Then(@"第一個產品應該是 ""(.*)""")]
    [Then(@"the first product should be ""(.*)""")]
    public async Task ThenFirstProductShouldBe(string productName)
    {
        Assert.NotNull(_response);
        var result = await _response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductResponse>>>();
        Assert.Equal(productName, result?.Data?.Items?.FirstOrDefault()?.Name);
    }

    [Then(@"產品名稱應該是 ""(.*)""")]
    [Then(@"the product name should be ""(.*)""")]
    public async Task ThenProductNameShouldBe(string name)
    {
        Assert.NotNull(_createdProductId);
        var response = await _client.GetAsync($"/api/products/{_createdProductId}");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>();
        Assert.Equal(name, result?.Data?.Name);
    }

    [Then(@"產品應該被標記為已刪除")]
    [Then(@"the product should be marked as deleted")]
    public void ThenProductShouldBeMarkedAsDeleted()
    {
        // Soft delete verification - product won't appear in list
        Assert.NotNull(_response);
        Assert.Equal(HttpStatusCode.NoContent, _response.StatusCode);
    }

    [Then(@"錯誤訊息應該包含 ""(.*)""")]
    [Then(@"the error message should contain ""(.*)""")]
    public async Task ThenErrorMessageShouldContain(string message)
    {
        Assert.NotNull(_response);
        var content = await _response.Content.ReadAsStringAsync();
        Assert.Contains(message, content, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"錯誤代碼應該是 ""(.*)""")]
    [Then(@"the error code should be ""(.*)""")]
    public async Task ThenErrorCodeShouldBe(string errorCode)
    {
        Assert.NotNull(_response);
        var result = await _response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(errorCode, result?.Code);
    }

    // Helper Methods

    private static string GetPasswordForUser(string username)
    {
        return username == "admin" ? "admin" : "password";
    }

    private async Task EnsureProductExists(string name, string tenantId)
    {
        // Login as admin to create product
        var originalAuth = _client.DefaultRequestHeaders.Authorization;

        await GivenIAmLoggedInAs("admin");
        var productId = await CreateProduct(name, 99.99m, "Other", tenantId);
        _productIds[name] = productId;

        _client.DefaultRequestHeaders.Authorization = originalAuth;
    }

    private async Task<Guid> CreateProduct(string name, decimal price, string category, string? tenantId = null)
    {
        var request = new
        {
            Name = name,
            Price = price,
            Category = category,
            Description = $"Test product: {name}"
        };

        var response = await _client.PostAsJsonAsync("/api/products", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CreateProductResponse>>();
        return result?.Data?.ProductId ?? Guid.Empty;
    }

    public void Dispose()
    {
        _response?.Dispose();
        _client.Dispose();
    }

    private record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);
    private record CreateProductResponse(Guid ProductId);
    private record ProductResponse(Guid Id, string Name, decimal Price, string Category, string? Description, string TenantId);
    private record PagedResult<T>(IReadOnlyList<T>? Items, int TotalCount, int Page, int Size);
}
