namespace DesktopMcp.Mcp;

public interface IMcpServerRuntime : IAsyncDisposable
{
    McpServerStatus Status { get; }

    McpAuthorizationState Authorization { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task ConfigureEndpointAsync(string url, string path = "/mcp", CancellationToken cancellationToken = default);

    void SetAuthorization(AuthMode mode, string? token);

    string RegenerateToken();
}
