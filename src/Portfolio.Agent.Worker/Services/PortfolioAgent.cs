using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Portfolio.Agent.Worker.Tools;
using AnthropicTool = Anthropic.SDK.Common.Tool;

namespace Portfolio.Agent.Worker.Services;

public class PortfolioAgent
{
    private readonly AnthropicClient _claude;
    private readonly List<AnthropicTool> _tools;
    private readonly ILogger<PortfolioAgent> _logger;

    private const string SystemPrompt =
        "You are a portfolio health analyst for a trade order processing system. " +
        "When asked to assess the system, always call get_metrics first, then list_orders to sample recent activity. " +
        "Flag anomalies: rejection rate >10%, any DEAD_LETTERED orders, outbox backlog >50. " +
        "Be concise, specific, and always include concrete numbers from the data you retrieve.";

    public PortfolioAgent(OrdersApiClient ordersApi, IConfiguration config, ILogger<PortfolioAgent> logger)
    {
        _logger = logger;
        var apiKey = config["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic:ApiKey configuration is missing.");

        _claude = new AnthropicClient(apiKey);

        var defs = new PortfolioToolDefinitions(ordersApi);
        _tools =
        [
            AnthropicTool.GetOrCreateTool(defs, nameof(PortfolioToolDefinitions.GetMetrics),
                "Retrieve current trade order metrics: total counts, status breakdown (PENDING/FILLED/REJECTED/DEAD_LETTERED), hourly volume, and outbox health."),
            AnthropicTool.GetOrCreateTool(defs, nameof(PortfolioToolDefinitions.ListOrders),
                "List recent trade orders, optionally filtered by status."),
        ];
    }

    public async Task<AgentResponse> RunAsync(string? prompt, CancellationToken ct = default)
    {
        var userPrompt = string.IsNullOrWhiteSpace(prompt)
            ? "Provide a comprehensive health assessment of the trade order system. Check metrics, review recent orders, and flag any anomalies."
            : prompt;

        _logger.LogInformation("Agent run started. Prompt: {Prompt}", userPrompt);

        var messages = new List<Message>
        {
            new(RoleType.User, userPrompt)
        };

        var parameters = new MessageParameters
        {
            Model   = AnthropicModels.Claude46Sonnet,
            MaxTokens = 4096,
            System  = [new SystemMessage(SystemPrompt)],
            Messages = messages,
            Tools   = _tools
        };

        int toolCallsExecuted = 0;

        for (int iteration = 0; iteration < 10; iteration++)
        {
            var response = await _claude.Messages.GetClaudeMessageAsync(parameters, ct);
            messages.Add(response.Message);

            var toolCalls = response.ToolCalls?.ToList() ?? [];
            if (toolCalls.Count == 0)
            {
                var result = response.Message.ToString();
                _logger.LogInformation("Agent run completed. Tool calls: {Count}. Tokens used: {Tokens}",
                    toolCallsExecuted, response.Usage?.OutputTokens);

                return new AgentResponse
                {
                    Result           = result,
                    ToolCallsExecuted = toolCallsExecuted,
                    Model            = parameters.Model,
                    InputTokens      = response.Usage?.InputTokens ?? 0,
                    OutputTokens     = response.Usage?.OutputTokens ?? 0
                };
            }

            toolCallsExecuted += toolCalls.Count;
            _logger.LogDebug("Executing {Count} tool call(s) in iteration {Iteration}",
                toolCalls.Count, iteration + 1);

            foreach (var toolCall in toolCalls)
            {
                var toolResult = await toolCall.InvokeAsync<string>();
                messages.Add(new Message(toolCall, toolResult));
            }
        }

        return new AgentResponse
        {
            Result            = "Agent reached maximum iterations without a final answer.",
            ToolCallsExecuted = toolCallsExecuted,
            Model             = parameters.Model
        };
    }
}

public record AgentResponse
{
    public string Result            { get; init; } = string.Empty;
    public int    ToolCallsExecuted { get; init; }
    public string Model             { get; init; } = string.Empty;
    public int    InputTokens       { get; init; }
    public int    OutputTokens      { get; init; }
}
