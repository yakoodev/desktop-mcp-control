using System.Net;
using System.Net.Sockets;
using DesktopMcp.App.ViewModels;
using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Models;
using DesktopMcp.Mcp;

namespace DesktopMcp.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public async Task SaveSettingsCommand_AppliesNetworkSettingsToRuntime()
    {
        var controller = new FakeDesktopAutomationController();
        var initialUrl = $"http://127.0.0.1:{GetFreePort()}";
        var updatedPort = GetFreePort().ToString();
        await using var runtime = new McpServerRuntime(
            controller,
            new McpServerRuntimeOptions(initialUrl, "/mcp", AuthMode.None));
        var viewModel = new MainWindowViewModel(runtime, controller);

        await runtime.StartAsync();
        viewModel.RefreshStatus();

        viewModel.SettingsHost = "127.0.0.1";
        viewModel.SettingsPort = updatedPort;
        viewModel.SettingsProtocol = "HTTP";
        await viewModel.SaveSettingsCommand.ExecuteAsync(null);

        Assert.True(runtime.Status.IsRunning);
        Assert.Equal($"http://127.0.0.1:{updatedPort}/mcp", runtime.Status.Endpoint);
        Assert.Equal($"http://127.0.0.1:{updatedPort}/mcp", viewModel.Endpoint);
        Assert.Equal("127.0.0.1", viewModel.NetworkHost);
        Assert.Equal(updatedPort, viewModel.NetworkPort);
        Assert.Equal("HTTP", viewModel.NetworkProtocol);
    }

    [Fact]
    public async Task SaveSettingsCommand_AppliesAuthorizationToRunningRuntime()
    {
        var controller = new FakeDesktopAutomationController();
        var baseUrl = $"http://127.0.0.1:{GetFreePort()}";
        await using var runtime = new McpServerRuntime(
            controller,
            new McpServerRuntimeOptions(baseUrl, "/mcp", AuthMode.None));
        var viewModel = new MainWindowViewModel(runtime, controller);

        await runtime.StartAsync();
        viewModel.RefreshStatus();
        viewModel.ResetSettingsDraft();
        viewModel.SettingsIsTokenAuthEnabled = true;
        viewModel.SettingsToken = "secret-token";

        await viewModel.SaveSettingsCommand.ExecuteAsync(null);

        Assert.True(runtime.Status.IsRunning);
        Assert.Equal(AuthMode.Token, runtime.Authorization.Mode);
        Assert.Equal("secret-token", runtime.Authorization.Token);
        Assert.True(viewModel.IsTokenAuthEnabled);
        Assert.Equal("Token (Bearer)", viewModel.AuthModeLabel);
        Assert.Equal("secre*******", viewModel.AuthTokenPreview);

        using var client = new HttpClient();
        var unauthorized = await client.GetAsync($"{baseUrl}/mcp");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        client.DefaultRequestHeaders.Add("Authorization", "Bearer secret-token");
        var authorized = await client.GetAsync($"{baseUrl}/mcp");
        Assert.NotEqual(HttpStatusCode.Unauthorized, authorized.StatusCode);
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class FakeDesktopAutomationController : IDesktopAutomationController
    {
        public Task<DesktopCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new DesktopCapabilities(
                    Mouse: true,
                    Keyboard: true,
                    Capture: true,
                    WindowList: true,
                    Tray: true,
                    GlobalHotkey: true));
        }

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
