using System.ComponentModel;
using ModelContextProtocol.Server;
using Portfolio.Mcp.Server.Services;

namespace Portfolio.Mcp.Server.Tools;

[McpServerToolType]
public class MetricsTools
{
    [McpServerTool, Description(
        "Get a data quality report: order volumes, status breakdown, outbox queue depth, and error counts. " +
        "Full Python data-quality library integration is tracked in issue #18.")]
    public static async Task<string> GetDataQualityReport(OrdersApiClient api)
    {
        var (_, body) = await api.GetMetricsAsync();
        return body;
    }
}
