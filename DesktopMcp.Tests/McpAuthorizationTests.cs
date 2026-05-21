using System.Net;
using System.Net.Sockets;
using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Models;
using DesktopMcp.Mcp;

namespace DesktopMcp.Tests;

public class McpAuthorizationTests
{
    [Fact]
    public void TryExtractBearerToken_ParsesValidHeader()
    {
        var ok = McpAuthToken.TryExtractBearerToken("Bearer secret-token", out var token);

        Assert.True(ok);
        Assert.Equal("secret-token", token);
    }

    [Fact]
    public void FixedTimeEquals_ComparesTokenValues()
    {
        Assert.True(McpAuthToken.FixedTimeEquals("abc", "abc"));
        Assert.False(McpAuthToken.FixedTimeEquals("abc", "abcd"));
        Assert.False(McpAuthToken.FixedTimeEquals("abc", "xyz"));
    }

    [Fact]
    public async Task Runtime_WithNoAuth_DoesNotReturnUnauthorized()
    {
        var baseUrl = $"http://127.0.0.1:{GetFreePort()}";
        await using var runtime = new McpServerRuntime(
            new FakeDesktopAutomationController(),
            new McpServerRuntimeOptions(baseUrl, "/mcp", AuthMode.None));

        await runtime.StartAsync();

        using var client = new HttpClient();
        var response = await client.GetAsync($"{baseUrl}/mcp");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Runtime_WithTokenAuth_ReturnsUnauthorizedWithoutToken()
    {
        var baseUrl = $"http://127.0.0.1:{GetFreePort()}";
        await using var runtime = new McpServerRuntime(
            new FakeDesktopAutomationController(),
            new McpServerRuntimeOptions(baseUrl, "/mcp", AuthMode.Token, "secret-token"));

        await runtime.StartAsync();

        using var client = new HttpClient();
        var response = await client.GetAsync($"{baseUrl}/mcp");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Runtime_WithTokenAuth_ReturnsUnauthorizedForInvalidToken()
    {
        var baseUrl = $"http://127.0.0.1:{GetFreePort()}";
        await using var runtime = new McpServerRuntime(
            new FakeDesktopAutomationController(),
            new McpServerRuntimeOptions(baseUrl, "/mcp", AuthMode.Token, "secret-token"));

        await runtime.StartAsync();

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer wrong-token");
        var response = await client.GetAsync($"{baseUrl}/mcp");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Runtime_WithTokenAuth_AllowsValidBearerToken()
    {
        var baseUrl = $"http://127.0.0.1:{GetFreePort()}";
        await using var runtime = new McpServerRuntime(
            new FakeDesktopAutomationController(),
            new McpServerRuntimeOptions(baseUrl, "/mcp", AuthMode.Token, "secret-token"));

        await runtime.StartAsync();

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer secret-token");
        var response = await client.GetAsync($"{baseUrl}/mcp");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Runtime_ConfigureEndpoint_RestartsOnNewUrl()
    {
        var firstUrl = $"http://127.0.0.1:{GetFreePort()}";
        var secondUrl = $"http://127.0.0.1:{GetFreePort()}";
        await using var runtime = new McpServerRuntime(
            new FakeDesktopAutomationController(),
            new McpServerRuntimeOptions(firstUrl, "/mcp", AuthMode.None));

        await runtime.StartAsync();
        Assert.Equal($"{firstUrl}/mcp", runtime.Status.Endpoint);

        await runtime.ConfigureEndpointAsync(secondUrl);

        Assert.True(runtime.Status.IsRunning);
        Assert.Equal($"{secondUrl}/mcp", runtime.Status.Endpoint);

        using var client = new HttpClient();
        var response = await client.GetAsync($"{secondUrl}/mcp");
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class FakeDesktopAutomationController : IDesktopAutomationController
    {
        public Task<VirtualDesktopInfo> GetDisplaysAsync(CancellationToken cancellationToken = default)
        {
            var info = new VirtualDesktopInfo(
                new ScreenRect(0, 0, 1920, 1080),
                [new DisplayInfo("display-1", new ScreenRect(0, 0, 1920, 1080), true, 1.0)]);
            return Task.FromResult(info);
        }

        public Task<IReadOnlyList<WindowInfo>> ListWindowsAsync(WindowQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<WindowInfo>>([]);
        }

        public Task<ScreenPoint> MouseMoveAsync(ScreenPoint point, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(point);
        }

        public Task<ScreenPoint> MouseClickAsync(ScreenPoint point, MouseButton button, int clickCount, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(point);
        }

        public Task<ScreenPoint> MouseScrollAsync(ScreenPoint point, int deltaY, int deltaX, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(point);
        }

        public Task KeyboardTypeAsync(string text, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task KeyboardHotkeyAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DragDropAsync(ScreenPoint from, ScreenPoint to, MouseButton button, int durationMs, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default)
        {
            var result = new CaptureResult(
                [],
                "image/png",
                0,
                0,
                new ScreenRect(0, 0, 0, 0),
                new ScreenPoint(0, 0),
                "display",
                null);
            return Task.FromResult(result);
        }

        public void TriggerEmergencyStop(string reason)
        {
        }

        public void ResetEmergencyStop()
        {
        }
    }
}
