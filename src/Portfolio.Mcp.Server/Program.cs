using Portfolio.Mcp.Server.Data;
using Portfolio.Mcp.Server.Middleware;
using Portfolio.Mcp.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<OrdersApiClient>(sp =>
    new OrdersApiClient(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
        sp.GetRequiredService<IConfiguration>()));
builder.Services.AddScoped<AuditRepository>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<Portfolio.Mcp.Server.Tools.OrderTools>()
    .WithTools<Portfolio.Mcp.Server.Tools.MetricsTools>()
    .WithTools<Portfolio.Mcp.Server.Tools.ReplayTools>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<AuditRepository>().EnsureTableAsync();

app.UseMiddleware<AuditMiddleware>();
app.MapMcp();
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));

app.Run();
