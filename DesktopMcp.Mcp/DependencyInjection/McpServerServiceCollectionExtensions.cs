using Microsoft.Extensions.DependencyInjection;

namespace DesktopMcp.Mcp.DependencyInjection;

public static class McpServerServiceCollectionExtensions
{
    public static IServiceCollection AddDesktopMcpServer(
        this IServiceCollection services,
        McpServerRuntimeOptions? options = null)
    {
        services.AddSingleton(options ?? new McpServerRuntimeOptions());
        services.AddSingleton<IMcpServerRuntime, McpServerRuntime>();
        return services;
    }
}
