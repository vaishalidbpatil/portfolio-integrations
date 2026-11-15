using Microsoft.AspNetCore.Mvc;
using Portfolio.Mcp.Server.Services;
using System.Collections.Concurrent;

namespace Portfolio.Mcp.Server.Tools;

[ApiController]
[Route("tools")]
public class ReplayTool : ControllerBase
{
    private readonly OrdersApiClient _api;
    private readonly IConfiguration _config;

    // In-memory approval store: token → (messageId, expiry). Process-scoped is fine for this demo.
    private static readonly ConcurrentDictionary<string, (string MessageId, DateTime ExpiresAt)> _pending = new();

    public ReplayTool(OrdersApiClient api, IConfiguration config)
    {
        _api = api;
        _config = config;
    }

    /// <summary>
    /// replay_dead_letter_message — step 1.
    /// Returns an approval token that must be confirmed before the replay executes.
    /// Requires McpServer:EnableWriteTools=true.
    /// </summary>
    [HttpPost("replay-dead-letter/{messageId}")]
    public IActionResult RequestReplay(string messageId)
    {
        if (!_config.GetValue<bool>("McpServer:EnableWriteTools"))
            return StatusCode(403, new
            {
                error = "Write tools are disabled.",
                hint = "Set McpServer:EnableWriteTools=true to enable replay."
            });

        var token = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddMinutes(5);
        _pending[token] = (messageId, expiresAt);

        return Accepted(new
        {
            approval_token = token,
            expires_at = expiresAt,
            confirm_url = $"POST /tools/replay-approval?token={token}"
        });
    }

    /// <summary>
    /// replay_dead_letter_message — step 2 (approval gate).
    /// Human confirms the approval token before the replay actually runs.
    /// </summary>
    [HttpPost("replay-approval")]
    public async Task<IActionResult> ConfirmReplay([FromQuery] string token)
    {
        if (!_config.GetValue<bool>("McpServer:EnableWriteTools"))
            return StatusCode(403, new { error = "Write tools are disabled." });

        if (!_pending.TryRemove(token, out var approval))
            return BadRequest(new { error = "Invalid or expired approval token." });

        if (DateTime.UtcNow > approval.ExpiresAt)
            return BadRequest(new { error = "Approval token has expired (5-minute window)." });

        var (code, body) = await _api.ReplayDeadLetterAsync(approval.MessageId);
        return StatusCode(code, body);
    }
}
