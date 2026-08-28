using Microsoft.JSInterop;

namespace TruvoID.Components.Services;

/// <summary>
/// Manages JWT tokens via browser localStorage.
/// All JS calls are guarded against SSR prerendering (JS interop unavailable during static render).
/// </summary>
public class TokenService
{
    private readonly IJSRuntime _js;
    private const string AccessTokenKey = "truvoid_access_token";
    private const string RefreshTokenKey = "truvoid_refresh_token";
    private const string ExpiryKey = "truvoid_token_expiry";

    public TokenService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try { return await _js.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey); }
        catch (InvalidOperationException) { return null; }
        catch (JSDisconnectedException) { return null; }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        try { return await _js.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey); }
        catch (InvalidOperationException) { return null; }
        catch (JSDisconnectedException) { return null; }
    }

    public async Task<DateTime?> GetTokenExpiryAsync()
    {
        try
        {
            var expiryStr = await _js.InvokeAsync<string?>("localStorage.getItem", ExpiryKey);
            if (string.IsNullOrEmpty(expiryStr)) return null;
            return DateTime.TryParse(expiryStr, out var expiry) ? expiry : null;
        }
        catch (InvalidOperationException) { return null; }
        catch (JSDisconnectedException) { return null; }
    }

    public async Task SetTokensAsync(string accessToken, string refreshToken, DateTime expiresAt)
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
            await _js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
            await _js.InvokeVoidAsync("localStorage.setItem", ExpiryKey, expiresAt.ToString("O"));
        }
        catch (InvalidOperationException) { }
        catch (JSDisconnectedException) { }
    }

    public async Task ClearTokensAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
            await _js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
            await _js.InvokeVoidAsync("localStorage.removeItem", ExpiryKey);
        }
        catch (InvalidOperationException) { }
        catch (JSDisconnectedException) { }
    }

    public async Task<bool> IsTokenExpiredAsync()
    {
        var expiry = await GetTokenExpiryAsync();
        return expiry is null || expiry <= DateTime.UtcNow;
    }

    /// <summary>
    /// Extract the "role" claim from the JWT access token.
    /// </summary>
    public string? GetRoleFromToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            var bytes = Convert.FromBase64String(payload);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("role", out var roleProp))
            {
                return roleProp.ValueKind == System.Text.Json.JsonValueKind.String
                    ? roleProp.GetString()
                    : roleProp.GetRawText();
            }
        }
        catch { }
        return null;
    }
}
