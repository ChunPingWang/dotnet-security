using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacSso.Common.Middleware;
using RbacSso.Common.Responses;
using RbacSso.Security.Authentication;

namespace RbacSso.Gateway.Controllers;

/// <summary>
/// Controller for authentication operations.
/// Handles OAuth2/OIDC flows with Keycloak.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ICorrelationIdAccessor correlationIdAccessor,
        ILogger<AuthenticationController> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _correlationIdAccessor = correlationIdAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Initiates the OAuth2 authorization code flow.
    /// Redirects the user to Keycloak login page.
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        var authority = _configuration["Keycloak:Authority"];
        var clientId = _configuration["Keycloak:ClientId"];
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/callback";

        var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(returnUrl ?? "/"));

        var authUrl = $"{authority}/protocol/openid-connect/auth" +
            $"?client_id={clientId}" +
            $"&response_type=code" +
            $"&scope=openid profile email" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&state={Uri.EscapeDataString(state)}";

        _logger.LogInformation("Redirecting user to Keycloak login");
        return Redirect(authUrl);
    }

    /// <summary>
    /// Handles the OAuth2 callback from Keycloak.
    /// Exchanges the authorization code for tokens.
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken cancellationToken)
    {
        try
        {
            var authority = _configuration["Keycloak:Authority"];
            var clientId = _configuration["Keycloak:ClientId"];
            var clientSecret = _configuration["Keycloak:ClientSecret"];
            var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/callback";

            var client = _httpClientFactory.CreateClient();

            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = clientId ?? string.Empty,
                ["client_secret"] = clientSecret ?? string.Empty,
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            });

            var response = await client.PostAsync(
                $"{authority}/protocol/openid-connect/token",
                tokenRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token exchange failed: {StatusCode}", response.StatusCode);
                return BadRequest(ErrorResponse.BadRequest(
                    "AUTH-B00001",
                    "Token exchange failed",
                    _correlationIdAccessor.CorrelationId));
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);

            // Decode state to get return URL
            var returnUrl = "/";
            try
            {
                returnUrl = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(state));
            }
            catch
            {
                // Ignore decoding errors
            }

            _logger.LogInformation("User authenticated successfully");

            // In a real application, you might set cookies or return the token
            return Ok(ApiResponse<TokenResponse>.Ok(tokenResponse!, _correlationIdAccessor.CorrelationId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication callback failed");
            return StatusCode(500, ErrorResponse.InternalError(_correlationIdAccessor.CorrelationId));
        }
    }

    /// <summary>
    /// Refreshes an expired access token.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var authority = _configuration["Keycloak:Authority"];
            var clientId = _configuration["Keycloak:ClientId"];
            var clientSecret = _configuration["Keycloak:ClientSecret"];

            var client = _httpClientFactory.CreateClient();

            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = clientId ?? string.Empty,
                ["client_secret"] = clientSecret ?? string.Empty,
                ["refresh_token"] = request.RefreshToken
            });

            var response = await client.PostAsync(
                $"{authority}/protocol/openid-connect/token",
                tokenRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token refresh failed: {StatusCode}", response.StatusCode);
                return BadRequest(ErrorResponse.BadRequest(
                    "AUTH-B00002",
                    "Token refresh failed",
                    _correlationIdAccessor.CorrelationId));
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);

            _logger.LogInformation("Token refreshed successfully");
            return Ok(ApiResponse<TokenResponse>.Ok(tokenResponse!, _correlationIdAccessor.CorrelationId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return StatusCode(500, ErrorResponse.InternalError(_correlationIdAccessor.CorrelationId));
        }
    }

    /// <summary>
    /// Gets the current user's information.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser([FromServices] ICurrentUser currentUser)
    {
        var userInfo = new UserInfoResponse
        {
            Username = currentUser.Username,
            Email = currentUser.Email,
            TenantId = currentUser.TenantId,
            Roles = currentUser.Roles.ToList()
        };

        return Ok(ApiResponse<UserInfoResponse>.Ok(userInfo, _correlationIdAccessor.CorrelationId));
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        var authority = _configuration["Keycloak:Authority"];
        var logoutUrl = $"{authority}/protocol/openid-connect/logout";

        _logger.LogInformation("User logged out");
        return Ok(ApiResponse.Ok(_correlationIdAccessor.CorrelationId, "Logged out successfully"));
    }
}

public record TokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
}

public record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public record UserInfoResponse
{
    public string Username { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
}
