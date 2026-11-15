using Portfolio.Mcp.Server.Data;

namespace Portfolio.Mcp.Server.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    public AuditMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, AuditRepository audit)
    {
        await _next(context);

        if (context.Request.Path.StartsWithSegments("/tools"))
        {
            var clientId = context.Request.Headers["X-Client-Id"].FirstOrDefault()
                           ?? context.User?.FindFirst("client_id")?.Value;
            await audit.RecordAsync(
                context.Request.Path.Value!,
                context.Request.Method,
                clientId,
                context.Response.StatusCode);
        }
    }
}
