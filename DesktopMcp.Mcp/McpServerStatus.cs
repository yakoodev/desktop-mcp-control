namespace DesktopMcp.Mcp;

public sealed record McpServerStatus(
    bool IsRunning,
    string Endpoint,
    DateTimeOffset? StartedAt,
    string? LastError,
    AuthMode AuthMode,
    bool IsTokenConfigured);
