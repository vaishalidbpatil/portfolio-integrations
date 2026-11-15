using Microsoft.AspNetCore.Mvc;
using Portfolio.Mcp.Server.Services;

namespace Portfolio.Mcp.Server.Tools;

[ApiController]
[Route("tools")]
public class MetricsTool : ControllerBase
{
    private readonly OrdersApiClient _api;
    public MetricsTool(OrdersApiClient api) => _api = api;

    /// <summary>
    /// get_data_quality_report — returns order volume and status metrics.
    /// Full Python data-quality library integration tracked in issue #18.
    /// </summary>
    [HttpGet("data-quality-report")]
    public async Task<IActionResult> GetDataQualityReport()
    {
        var (code, body) = await _api.GetMetricsAsync();
        return StatusCode(code, body);
    }
}
