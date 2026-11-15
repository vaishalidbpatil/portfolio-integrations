using Microsoft.Extensions.AI;
using OpenAI;
using Portfolio.Agent.Worker.Services;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<OrdersApiClient>();

var geminiKey = builder.Configuration["Gemini:ApiKey"]
    ?? throw new InvalidOperationException("Gemini:ApiKey is not configured.");

builder.Services.AddChatClient(
    new OpenAIClient(
        new ApiKeyCredential(geminiKey),
        new OpenAIClientOptions { Endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/") }
    )
    .GetChatClient("gemini-1.5-flash")
    .AsIChatClient()
).UseFunctionInvocation();

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
