using Anthropic.SDK.Common;
using Portfolio.Agent.Worker.Services;

namespace Portfolio.Agent.Worker.Tools;

public class PortfolioToolDefinitions(OrdersApiClient api)
{
    public async Task<string> GetMetrics()
        => await api.GetMetricsJsonAsync();

    public async Task<string> ListOrders(
        [FunctionParameter("Status filter: PENDING, FILLED, REJECTED, DEAD_LETTERED. Omit to return orders across all statuses.", false)]
        string? status,
        [FunctionParameter("Maximum number of orders to return (1-50).", false)]
        int pageSize)
        => await api.ListOrdersJsonAsync(status, pageSize == 0 ? 20 : pageSize);
}
