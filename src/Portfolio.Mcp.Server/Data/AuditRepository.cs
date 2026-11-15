using Npgsql;

namespace Portfolio.Mcp.Server.Data;

public class AuditRepository
{
    private readonly string? _connectionString;

    public AuditRepository(IConfiguration config) =>
        _connectionString = config["Database:ConnectionString"];

    public async Task EnsureTableAsync()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS mcp_tool_calls (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    tool TEXT NOT NULL,
                    method TEXT NOT NULL,
                    client_id TEXT,
                    status_code INT,
                    called_at TIMESTAMPTZ NOT NULL DEFAULT now()
                );
                """;
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AuditRepository] Table setup failed: {ex.Message}");
        }
    }

    public async Task RecordAsync(string tool, string method, string? clientId, int statusCode)
    {
        if (string.IsNullOrEmpty(_connectionString)) return;
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "INSERT INTO mcp_tool_calls (tool, method, client_id, status_code) VALUES ($1, $2, $3, $4)";
            cmd.Parameters.AddWithValue(tool);
            cmd.Parameters.AddWithValue(method);
            cmd.Parameters.AddWithValue((object?)clientId ?? DBNull.Value);
            cmd.Parameters.AddWithValue(statusCode);
            await cmd.ExecuteNonQueryAsync();
        }
        catch { /* audit failure must never break the tool call */ }
    }
}
