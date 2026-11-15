using Microsoft.AspNetCore.Mvc;
using Portfolio.Mcp.Server.Services;

namespace Portfolio.Mcp.Server.Tools;

[ApiController]
[Route("tools")]
public class OrdersTool : ControllerBase
{
    private readonly OrdersApiClient _api;
    public OrdersTool(OrdersApiClient api) => _api = api;

    /// <summary>get_order_status — returns current status and details of a single order</summary>
    [HttpGet("order-status/{id:guid}")]
    public async Task<IActionResult> GetOrderStatus(Guid id)
    {
        var (code, body) = await _api.GetOrderStatusAsync(id);
        return StatusCode(code, body);
    }

    /// <summary>list_recent_orders — paginated order list with optional filters</summary>
    [HttpGet("orders")]
    public async Task<IActionResult> ListOrders(
        [FromQuery] string? accountId,
        [FromQuery] string? symbol,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (code, body) = await _api.ListOrdersAsync(accountId, symbol, status, page, pageSize);
        return StatusCode(code, body);
    }
}
