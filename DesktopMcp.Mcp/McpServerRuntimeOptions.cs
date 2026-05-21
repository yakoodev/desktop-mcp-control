namespace DesktopMcp.Mcp;

public sealed record McpServerRuntimeOptions(
    string Url = "http://127.0.0.1:45454",
    string Path = "/mcp",
    AuthMode AuthMode = AuthMode.None,
    string Token = "");
