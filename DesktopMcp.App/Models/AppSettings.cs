using DesktopMcp.Mcp;

namespace DesktopMcp.App.Models;

public sealed class AppSettings
{
    public AuthMode AuthMode { get; init; } = AuthMode.None;

    public string Token { get; init; } = string.Empty;

    public string NetworkHost { get; init; } = "127.0.0.1";

    public string NetworkPort { get; init; } = "45454";

    public string NetworkProtocol { get; init; } = "HTTP";
}
