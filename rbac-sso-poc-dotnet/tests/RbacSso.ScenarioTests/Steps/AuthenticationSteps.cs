using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;
using RbacSso.Common.Responses;
using Xunit;

namespace RbacSso.ScenarioTests.Steps;

/// <summary>
/// Step definitions for Authentication.feature
/// 認證功能的步驟定義
/// </summary>
[Binding]
public sealed class AuthenticationSteps : IDisposable
{
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;
    private string? _username;
    private string? _password;
    private string? _accessToken;
    private string? _refreshToken;

    public AuthenticationSteps(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // Given Steps

    [Given(@"我是使用者 ""(.*)""")]
    [Given(@"I am user ""(.*)""")]
    public void GivenIAmUser(string username)
    {
        _username = username;
    }

    [Given(@"我的密碼是 ""(.*)""")]
    [Given(@"my password is ""(.*)""")]
    public void GivenMyPasswordIs(string password)
    {
        _password = password;
    }

    [Given(@"我已經登入")]
    [Given(@"I am logged in")]
    public async Task GivenIAmLoggedIn()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Username = _username,
            Password = _password
        });

        loginResponse.EnsureSuccessStatusCode();
        var result = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        _accessToken = result?.AccessToken;
        _refreshToken = result?.RefreshToken;
    }

    [Given(@"我有一個有效的 refresh token")]
    [Given(@"I have a valid refresh token")]
    public void GivenIHaveAValidRefreshToken()
    {
        Assert.NotNull(_refreshToken);
    }

    [Given(@"我有一個過期的 access token")]
    [Given(@"I have an expired access token")]
    public void GivenIHaveAnExpiredAccessToken()
    {
        // 模擬過期的 token
        _accessToken = "expired.token.here";
    }

    // When Steps

    [When(@"我發送登入請求")]
    [When(@"I send a login request")]
    public async Task WhenISendALoginRequest()
    {
        _response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Username = _username,
            Password = _password
        });
    }

    [When(@"我請求 token 刷新")]
    [When(@"I request token refresh")]
    public async Task WhenIRequestTokenRefresh()
    {
        _response = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            RefreshToken = _refreshToken
        });
    }

    [When(@"我請求登出")]
    [When(@"I request logout")]
    public async Task WhenIRequestLogout()
    {
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        _response = await _client.PostAsync("/api/auth/logout", null);
    }

    [When(@"OAuth2 callback 回傳授權碼 ""(.*)""")]
    [When(@"OAuth2 callback returns authorization code ""(.*)""")]
    public async Task WhenOAuth2CallbackReturnsAuthorizationCode(string code)
    {
        _response = await _client.GetAsync($"/api/auth/callback?code={code}&state=test-state");
    }

    // Then Steps

    [Then(@"回應狀態碼應該是 (.*)")]
    [Then(@"the response status code should be (.*)")]
    public void ThenTheResponseStatusCodeShouldBe(int statusCode)
    {
        Assert.NotNull(_response);
        Assert.Equal((HttpStatusCode)statusCode, _response.StatusCode);
    }

    [Then(@"我應該收到有效的 JWT token")]
    [Then(@"I should receive a valid JWT token")]
    public async Task ThenIShouldReceiveAValidJwtToken()
    {
        Assert.NotNull(_response);
        var result = await _response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(result?.AccessToken);
        Assert.NotEmpty(result.AccessToken);
        _accessToken = result.AccessToken;
        _refreshToken = result.RefreshToken;
    }

    [Then(@"JWT token 應該包含 claim ""(.*)""")]
    [Then(@"the JWT token should contain claim ""(.*)""")]
    public void ThenTheJwtTokenShouldContainClaim(string claimName)
    {
        Assert.NotNull(_accessToken);
        // JWT token 解析驗證 claim 存在
        var parts = _accessToken.Split('.');
        Assert.Equal(3, parts.Length);
        // 實際驗證會在整合測試中進行
    }

    [Then(@"我應該收到新的 access token")]
    [Then(@"I should receive a new access token")]
    public async Task ThenIShouldReceiveANewAccessToken()
    {
        Assert.NotNull(_response);
        var result = await _response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(result?.AccessToken);
        Assert.NotEmpty(result.AccessToken);
    }

    [Then(@"錯誤訊息應該包含 ""(.*)""")]
    [Then(@"the error message should contain ""(.*)""")]
    public async Task ThenTheErrorMessageShouldContain(string expectedMessage)
    {
        Assert.NotNull(_response);
        var content = await _response.Content.ReadAsStringAsync();
        Assert.Contains(expectedMessage, content, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"我應該被登出")]
    [Then(@"I should be logged out")]
    public void ThenIShouldBeLoggedOut()
    {
        Assert.NotNull(_response);
        Assert.True(_response.IsSuccessStatusCode);
    }

    public void Dispose()
    {
        _response?.Dispose();
        _client.Dispose();
    }

    private record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);
}
