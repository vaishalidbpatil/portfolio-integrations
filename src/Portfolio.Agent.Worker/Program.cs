using Portfolio.Agent.Worker.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<OrdersApiClient>();
builder.Services.AddSingleton<PortfolioAgent>();

var app = builder.Build();

app.MapPost("/agent/run", async (AgentRequest? req, PortfolioAgent agent, CancellationToken ct) =>
{
    var result = await agent.RunAsync(req?.Prompt, ct);
    return Results.Ok(result);
});

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));

app.Run();

public record AgentRequest(string? Prompt);
