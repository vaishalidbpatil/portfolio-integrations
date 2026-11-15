using System.ComponentModel;
using ModelContextProtocol.Server;
using Portfolio.Mcp.Server.Services;

namespace Portfolio.Mcp.Server.Tools;

[McpServerToolType]
public class OrderTools
{
    [McpServerTool, Description("Get the current status and details of a single trade order by its ID.")]
    public static async Task<string> GetOrderStatus(
        [Description("The order ID (GUID)")] string orderId,
        OrdersApiClient api)
    {
        if (!Guid.TryParse(orderId, out var id))
            return "Error: orderId must be a valid GUID.";

        var (code, body) = await api.GetOrderStatusAsync(id);
        return code == 404 ? $"Order {orderId} not found." : body;
    }

    [McpServerTool, Description("List recent trade orders with optional filters. Returns a paged result.")]
    public static async Task<string> ListRecentOrders(
        [Description("Filter by account ID (optional)")] string? accountId,
        [Description("Filter by symbol e.g. AAPL (optional)")] string? symbol,
        [Description("Filter by status: PENDING, FILLED, REJECTED, DEAD_LETTERED (optional)")] string? status,
        [Description("Page number, 1-based (default 1)")] int page,
        [Description("Page size, max 100 (default 20)")] int pageSize,
        OrdersApiClient api)
    {
        var (_, body) = await api.ListOrdersAsync(accountId, symbol, status, page, pageSize);
        return body;
    }
}
