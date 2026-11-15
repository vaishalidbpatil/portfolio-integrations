using Portfolio.Mcp.Server.Data;
using Portfolio.Mcp.Server.Middleware;
using Portfolio.Mcp.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<OrdersApiClient>(sp =>
    new OrdersApiClient(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
        sp.GetRequiredService<IConfiguration>()));
builder.Services.AddScoped<AuditRepository>();

var app = builder.Build();

// Ensure audit log table exists (non-fatal if DB not configured)
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<AuditRepository>().EnsureTableAsync();

app.UseMiddleware<AuditMiddleware>();
app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));

app.Run();
