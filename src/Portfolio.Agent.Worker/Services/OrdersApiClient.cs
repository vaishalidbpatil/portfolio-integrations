namespace Portfolio.Agent.Worker.Services;

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

            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "client_credentials",
                ["client_id"]     = _config["Orders:ClientId"]!,
                ["client_secret"] = _config["Orders:ClientSecret"]!,
                ["scope"]         = _config["Orders:Scope"]!
            });

            var resp = await _http.PostAsync(_config["Orders:TokenUrl"], form);
            resp.EnsureSuccessStatusCode();
            var token = await resp.Content.ReadFromJsonAsync<TokenResponse>();
            _cachedToken = token!.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 30);
            return _cachedToken;
        }
        finally { _lock.Release(); }
    }

    private async Task<HttpRequestMessage> AuthorizedRequest(HttpMethod method, string path)
    {
        var token = await GetTokenAsync();
        var req = new HttpRequestMessage(method, _config["Orders:BaseUrl"] + path);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    public async Task<string> GetMetricsJsonAsync()
    {
        // Metrics endpoint is public (no auth required)
        var resp = await _http.GetAsync(_config["Orders:BaseUrl"] + "/v1/metrics");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync();
    }

    public async Task<string> ListOrdersJsonAsync(string? status, int pageSize)
    {
        var qs = $"?pageSize={Math.Clamp(pageSize, 1, 50)}";
        if (!string.IsNullOrEmpty(status))
            qs += $"&status={Uri.EscapeDataString(status)}";

        var resp = await _http.SendAsync(await AuthorizedRequest(HttpMethod.Get, $"/v1/orders{qs}"));
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync();
    }
}

public record TokenResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken,
    [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn
);
