using Microsoft.Extensions.AI;

namespace Portfolio.Agent.Worker.Services;

public class PortfolioAgent(IChatClient chatClient, OrdersApiClient ordersApi, ILogger<PortfolioAgent> logger)
{
    private const string ModelId = "gemini-1.5-flash";

    private const string SystemPrompt =
        "You are a portfolio health analyst for a trade order processing system. " +
        "When asked to assess the system, always call get_metrics first, then list_orders to sample recent activity. " +
        "Flag anomalies: rejection rate >10%, any DEAD_LETTERED orders, outbox backlog >50. " +
        "Be concise, specific, and always cite concrete numbers from the data you retrieve.";

    public async Task<AgentResponse> RunAsync(string? prompt, CancellationToken ct = default)
    {
        var userPrompt = string.IsNullOrWhiteSpace(prompt)
            ? "Provide a comprehensive health assessment of the trade order system. Check metrics, review recent orders, and flag any anomalies."
            : prompt;

        logger.LogInformation("Agent run started. Prompt: {Prompt}", userPrompt);

        var tools = new AITool[]
        {
            AIFunctionFactory.Create(
                new Func<Task<string>>(ordersApi.GetMetricsJsonAsync),
                "get_metrics",
                "Retrieve current trade order metrics: total counts, breakdown by status (PENDING/FILLED/REJECTED/DEAD_LETTERED), orders in the last hour, and outbox health."),
            AIFunctionFactory.Create(
                new Func<string?, int, Task<string>>(ordersApi.ListOrdersJsonAsync),
                "list_orders",
                "List recent trade orders with optional filters.")
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var options = new ChatOptions
        {
            ModelId      = ModelId,
            MaxOutputTokens = 4096,
            Tools        = [.. tools],
            ToolMode     = ChatToolMode.Auto
        };

        var response = await chatClient.GetResponseAsync(messages, options, ct);

        logger.LogInformation("Agent run completed. Input tokens: {In}, Output tokens: {Out}",
            response.Usage?.InputTokenCount, response.Usage?.OutputTokenCount);

        return new AgentResponse
        {
            Result       = response.Text ?? string.Empty,
            Model        = ModelId,
            InputTokens  = (int)(response.Usage?.InputTokenCount ?? 0),
            OutputTokens = (int)(response.Usage?.OutputTokenCount ?? 0)
        };
    }
}

public record AgentResponse
{
    public string Result       { get; init; } = string.Empty;
    public string Model        { get; init; } = string.Empty;
    public int    InputTokens  { get; init; }
    public int    OutputTokens { get; init; }
}
