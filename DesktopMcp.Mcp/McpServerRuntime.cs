using DesktopMcp.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DesktopMcp.Mcp;

public sealed class McpServerRuntime : IMcpServerRuntime
{
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private readonly object _authorizationLock = new();
    private readonly IDesktopAutomationController _controller;

    private WebApplication? _app;
    private DateTimeOffset? _startedAt;
    private string? _lastError;
    private string _url;
    private string _path;
    private AuthMode _authMode;
    private string _token;

    public McpServerRuntime(
        IDesktopAutomationController controller,
        McpServerRuntimeOptions? options = null)
    {
        _controller = controller;
        var normalizedOptions = options ?? new McpServerRuntimeOptions();
        _url = NormalizeUrl(normalizedOptions.Url);
        _path = NormalizePath(normalizedOptions.Path);
        _authMode = normalizedOptions.AuthMode;
        _token = normalizedOptions.Token.Trim();

        if (_authMode == AuthMode.Token && string.IsNullOrWhiteSpace(_token))
        {
            _token = McpAuthToken.Generate();
        }
    }

    public McpServerStatus Status
    {
        get
        {
            var isRunning = _app is not null;
            var auth = Authorization;
            return new McpServerStatus(
                isRunning,
                BuildEndpoint(),
                _startedAt,
                _lastError,
                auth.Mode,
                auth.HasToken);
        }
    }

    public McpAuthorizationState Authorization
    {
        get
        {
            lock (_authorizationLock)
            {
                return new McpAuthorizationState(_authMode, _token).Normalize();
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_app is not null)
            {
                return;
            }

            await StartCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            throw;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_app is null)
            {
                return;
            }

            await StopCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            throw;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task ConfigureEndpointAsync(
        string url,
        string path = "/mcp",
        CancellationToken cancellationToken = default)
    {
        var normalizedUrl = NormalizeUrl(url);
        var normalizedPath = NormalizePath(path);

        await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (string.Equals(_url, normalizedUrl, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(_path, normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var wasRunning = _app is not null;
            if (wasRunning)
            {
                await StopCoreAsync(cancellationToken).ConfigureAwait(false);
            }

            _url = normalizedUrl;
            _path = normalizedPath;

            if (wasRunning)
            {
                await StartCoreAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            throw;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public void SetAuthorization(AuthMode mode, string? token)
    {
        lock (_authorizationLock)
        {
            var normalizedToken = token?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(normalizedToken))
            {
                _token = normalizedToken;
            }

            if (mode == AuthMode.Token && string.IsNullOrWhiteSpace(_token))
            {
                _token = McpAuthToken.Generate();
            }

            _authMode = mode;
        }
    }

    public string RegenerateToken()
    {
        lock (_authorizationLock)
        {
            _token = McpAuthToken.Generate();
            return _token;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _stateLock.Dispose();
    }

    private string BuildEndpoint()
    {
        return $"{_url.TrimEnd('/')}{_path}";
    }

    private async Task StartCoreAsync(CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(_url);
        builder.Services.AddSingleton(_controller);
        builder.Services.AddMcpServer()
            .WithTools<DesktopAutomationTools>()
            .WithHttpTransport();

        var app = builder.Build();
        var endpointPath = new PathString(_path);

        app.Use(
            async (context, next) =>
            {
                if (!context.Request.Path.StartsWithSegments(endpointPath, StringComparison.OrdinalIgnoreCase))
                {
                    await next().ConfigureAwait(false);
                    return;
                }

                var auth = Authorization;
                if (auth.Mode == AuthMode.None)
                {
                    await next().ConfigureAwait(false);
                    return;
                }

                if (!auth.HasToken)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync(
                            "Token authorization is enabled, but token is not configured.",
                            context.RequestAborted)
                        .ConfigureAwait(false);
                    return;
                }

                if (!context.Request.Headers.TryGetValue("Authorization", out var headerValues) ||
                    !McpAuthToken.TryExtractBearerToken(headerValues.ToString(), out var incomingToken) ||
                    !McpAuthToken.FixedTimeEquals(incomingToken, auth.Token))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid bearer token.", context.RequestAborted).ConfigureAwait(false);
                    return;
                }

                await next().ConfigureAwait(false);
            });

        app.MapMcp(endpointPath.Value!);

        await app.StartAsync(cancellationToken).ConfigureAwait(false);

        _app = app;
        _startedAt = DateTimeOffset.UtcNow;
        _lastError = null;
    }

    private async Task StopCoreAsync(CancellationToken cancellationToken)
    {
        if (_app is null)
        {
            return;
        }

        await _app.StopAsync(cancellationToken).ConfigureAwait(false);
        await _app.DisposeAsync().ConfigureAwait(false);
        _app = null;
        _startedAt = null;
    }

    private static string NormalizeUrl(string url)
    {
        var normalized = string.IsNullOrWhiteSpace(url)
            ? "http://127.0.0.1:45454"
            : url.Trim().TrimEnd('/');

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            uri.Port <= 0)
        {
            throw new ArgumentException("MCP endpoint URL must be an absolute HTTP/HTTPS URL with a valid port.", nameof(url));
        }

        return normalized;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/mcp";
        }

        return path.StartsWith('/') ? path : $"/{path}";
    }
}
