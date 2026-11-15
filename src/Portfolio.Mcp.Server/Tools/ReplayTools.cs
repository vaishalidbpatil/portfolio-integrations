using System.Collections.Concurrent;
using System.ComponentModel;
using ModelContextProtocol.Server;
using Portfolio.Mcp.Server.Services;

namespace Portfolio.Mcp.Server.Tools;

[McpServerToolType]
public static class ReplayTools
{
    private static readonly ConcurrentDictionary<string, (string MessageId, DateTime ExpiresAt)> _pending = new();

    [McpServerTool, Description(
        "Request replay of a dead-lettered message. " +
        "Returns an approval token that must be confirmed with ConfirmReplay before the replay executes. " +
        "Requires McpServer:EnableWriteTools=true — disabled by default.")]
    public static string RequestReplay(
        [Description("The dead-letter message ID to replay")] string messageId,
        IConfiguration config)
    {
        if (!config.GetValue<bool>("McpServer:EnableWriteTools"))
            return "Error: write tools are disabled. Set McpServer:EnableWriteTools=true to enable replay.";

        var token = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddMinutes(5);
        _pending[token] = (messageId, expiresAt);
        return $"Approval required. Call ConfirmReplay with token: {token} (expires {expiresAt:u})";
    }

    [McpServerTool, Description(
        "Confirm and execute a previously requested dead-letter replay using the approval token from RequestReplay. " +
        "Token expires after 5 minutes.")]
    public static async Task<string> ConfirmReplay(
        [Description("The approval token returned by RequestReplay")] string token,
        IConfiguration config,
        OrdersApiClient api)
    {
        if (!config.GetValue<bool>("McpServer:EnableWriteTools"))
            return "Error: write tools are disabled.";

        if (!_pending.TryRemove(token, out var approval))
            return "Error: invalid or already-used approval token.";

        if (DateTime.UtcNow > approval.ExpiresAt)
            return "Error: approval token has expired (5-minute window).";

        var (code, body) = await api.ReplayDeadLetterAsync(approval.MessageId);
        return code is >= 200 and < 300
            ? $"Replay accepted: {body}"
            : $"Replay failed ({code}): {body}";
    }
}
