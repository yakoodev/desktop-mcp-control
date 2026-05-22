using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Exceptions;
using DesktopMcp.Core.Linux;
using DesktopMcp.Core.Models;
using DesktopMcp.Core.Services;
using DesktopMcp.Mcp;

namespace DesktopMcp.Tests;

public class DesktopCapabilityTests
{
    [Fact]
    public void IsWaylandSession_ReturnsTrue_WhenSessionTypeIsWayland()
    {
        var originalSessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
        var originalWaylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");

        try
        {
            Environment.SetEnvironmentVariable("XDG_SESSION_TYPE", "wayland");
            Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", null);

            Assert.True(LinuxDesktopCapabilityProbe.IsWaylandSession());
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_SESSION_TYPE", originalSessionType);
            Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", originalWaylandDisplay);
        }
    }

    [Fact]
    public async Task Controller_ThrowsCapabilityUnavailable_WhenMouseCapabilityIsDisabled()
    {
        var backend = new FakeBackend(
            new DesktopCapabilities(
                Mouse: false,
                Keyboard: true,
                Capture: true,
                WindowList: true,
                Tray: true,
                GlobalHotkey: true));
        var controller = new DesktopAutomationController(
            backend,
            new SerializedActionQueue(),
            new EmergencyStopService());

        var ex = await Assert.ThrowsAsync<CapabilityUnavailableException>(
            () => controller.MouseMoveAsync(new ScreenPoint(10, 20)));

        Assert.Equal(DesktopCapability.Mouse, ex.Capability);
    }

    [Fact]
    public async Task McpTool_MouseMove_ReturnsCapabilityUnavailablePayload()
    {
        var tools = new DesktopAutomationTools(new ThrowingController());

        var result = await tools.MouseMove(10, 20);

        Assert.True(result.IsError);
        Assert.True(result.StructuredContent.HasValue);
        var payload = result.StructuredContent.GetValueOrDefault();
        Assert.Equal("capability_unavailable", payload.GetProperty("code").GetString());
        Assert.Equal("mouse", payload.GetProperty("capability").GetString());
    }

    [Fact]
    public async Task McpTool_GetCapabilities_ReturnsFlags()
    {
        var tools = new DesktopAutomationTools(new CapabilityController());

        var result = await tools.GetCapabilities();

        Assert.False(result.IsError);
        Assert.True(result.StructuredContent.HasValue);
        var payload = result.StructuredContent.GetValueOrDefault();
        Assert.True(payload.GetProperty("mouse").GetBoolean());
        Assert.False(payload.GetProperty("globalHotkey").GetBoolean());
    }

    private sealed class ThrowingController : IDesktopAutomationController
    {
        public Task<DesktopCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new DesktopCapabilities(
                    Mouse: false,
                    Keyboard: false,
                    Capture: false,
                    WindowList: false,
                    Tray: false,
                    GlobalHotkey: false));
        }

        public Task<VirtualDesktopInfo> GetDisplaysAsync(CancellationToken cancellationToken = default)
        {
            throw new CapabilityUnavailableException(DesktopCapability.Capture);
        }

        public Task<IReadOnlyList<WindowInfo>> ListWindowsAsync(WindowQuery query, CancellationToken cancellationToken = default)
        {
            throw new CapabilityUnavailableException(DesktopCapability.WindowList);
        }

        public Task<ScreenPoint> MouseMoveAsync(ScreenPoint point, CancellationToken cancellationToken = default)
        {
            throw new CapabilityUnavailableException(DesktopCapability.Mouse);
        }

        public Task<ScreenPoint> MouseClickAsync(ScreenPoint point, MouseButton button, int clickCount, CancellationToken cancellationToken = default)
        {
            throw new CapabilityUnavailableException(DesktopCapability.Mouse);
        }

        public Task<ScreenPoint> MouseScrollAsync(ScreenPoint point, int deltaY, int deltaX, CancellationToken cancellationToken = default)
        {
            throw new CapabilityUnavailableException(DesktopCapability.Mouse);
        }

        public Task KeyboardTypeAsync(string text, CancellationToken cancellationToken = default)
        {
            throw new CapabilityUnavailableException(DesktopCapability.Keyboard);
        }

        public Task KeyboardHotkeyAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken = default)
        {
            throw new CapabilityUnavailableException(DesktopCapability.Keyboard);
        }

        public Task DragDropAsync(ScreenPoint from, ScreenPoint to, MouseButton button, int durationMs, CancellationToken cancellationToken = default)
        {
            throw new CapabilityUnavailableException(DesktopCapability.Mouse);
        }

        public Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default)
        {
            throw new CapabilityUnavailableException(DesktopCapability.Capture);
        }

        public void TriggerEmergencyStop(string reason)
        {
        }

        public void ResetEmergencyStop()
        {
        }
    }

    private sealed class CapabilityController : IDesktopAutomationController
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
                    GlobalHotkey: false));
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

    private sealed class FakeBackend : IDesktopAutomationBackend
    {
        private readonly DesktopCapabilities _capabilities;

        public FakeBackend(DesktopCapabilities capabilities)
        {
            _capabilities = capabilities;
        }

        public DesktopCapabilities GetCapabilities() => _capabilities;

        public VirtualDesktopInfo GetVirtualDesktopInfo()
        {
            return new VirtualDesktopInfo(
                new ScreenRect(0, 0, 1920, 1080),
                [new DisplayInfo("display-1", new ScreenRect(0, 0, 1920, 1080), true, 1.0)]);
        }

        public IReadOnlyList<WindowInfo> ListWindows(WindowQuery query) => [];

        public bool TryGetWindowBounds(string windowId, out ScreenRect bounds)
        {
            bounds = default;
            return false;
        }

        public void MoveMouse(ScreenPoint point)
        {
        }

        public void Click(ScreenPoint point, MouseButton button, int clickCount)
        {
        }

        public void Scroll(ScreenPoint point, int deltaY, int deltaX)
        {
        }

        public void TypeText(string text)
        {
        }

        public void PressHotkey(IReadOnlyList<string> keys)
        {
        }

        public Task DragDropAsync(ScreenPoint from, ScreenPoint to, MouseButton button, int durationMs, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public ScreenPoint GetCursorPosition() => new(0, 0);

        public CaptureResult Capture(CaptureRequest request)
        {
            return new CaptureResult(
                [],
                "image/png",
                0,
                0,
                new ScreenRect(0, 0, 0, 0),
                new ScreenPoint(0, 0),
                "display",
                null);
        }
    }
}
