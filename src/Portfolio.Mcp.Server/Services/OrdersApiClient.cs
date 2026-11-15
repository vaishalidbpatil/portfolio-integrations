namespace Portfolio.Mcp.Server.Services;

public class OrdersApiClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public OrdersApiClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    private async Task<string> GetTokenAsync()
    {
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        await _lock.WaitAsync();
        try
        {
            if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
                return _cachedToken;

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _config["Orders:ClientId"]!,
                ["client_secret"] = _config["Orders:ClientSecret"]!,
                ["scope"] = _config["Orders:Scope"]!
            });

            var resp = await _http.PostAsync(_config["Orders:TokenUrl"], content);
            resp.EnsureSuccessStatusCode();
            var token = await resp.Content.ReadFromJsonAsync<TokenResponse>();
            _cachedToken = token!.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 30);
            return _cachedToken;
        }
        finally { _lock.Release(); }
    }

    private async Task<HttpRequestMessage> BuildRequest(HttpMethod method, string path)
    {
        var token = await GetTokenAsync();
        var req = new HttpRequestMessage(method, _config["Orders:BaseUrl"] + path);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    public async Task<(int StatusCode, string Body)> GetOrderStatusAsync(Guid id)
    {
        var resp = await _http.SendAsync(await BuildRequest(HttpMethod.Get, $"/v1/orders/{id}"));
        return ((int)resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    public async Task<(int StatusCode, string Body)> ListOrdersAsync(
        string? accountId, string? symbol, string? status, int page, int pageSize)
    {
        var qs = $"?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(accountId)) qs += $"&accountId={Uri.EscapeDataString(accountId)}";
        if (!string.IsNullOrEmpty(symbol))    qs += $"&symbol={Uri.EscapeDataString(symbol)}";
        if (!string.IsNullOrEmpty(status))    qs += $"&status={Uri.EscapeDataString(status)}";
        var resp = await _http.SendAsync(await BuildRequest(HttpMethod.Get, $"/v1/orders{qs}"));
        return ((int)resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    public async Task<(int StatusCode, string Body)> GetMetricsAsync()
    {
        var resp = await _http.SendAsync(await BuildRequest(HttpMethod.Get, "/v1/metrics"));
        return ((int)resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    public async Task<(int StatusCode, string Body)> ReplayDeadLetterAsync(string messageId)
    {
        var resp = await _http.SendAsync(
            await BuildRequest(HttpMethod.Post, $"/v1/orders/dead-letter/{messageId}/replay"));
        return ((int)resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }
}

public record TokenResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken,
    [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn
);
