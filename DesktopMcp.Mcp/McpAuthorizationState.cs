namespace DesktopMcp.Mcp;

public sealed record McpAuthorizationState(AuthMode Mode, string Token)
{
    public static McpAuthorizationState None { get; } = new(AuthMode.None, string.Empty);

    public bool HasToken => !string.IsNullOrWhiteSpace(Token);

    public McpAuthorizationState Normalize()
    {
        if (Mode == AuthMode.None)
        {
            return None;
        }

        return new McpAuthorizationState(AuthMode.Token, Token?.Trim() ?? string.Empty);
    }
}
