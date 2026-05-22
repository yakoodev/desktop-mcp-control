[Русская версия](README.ru.md)

# Desktop MCP Control

A cross-platform MCP control panel that exposes safe, local desktop automation over Streamable HTTP. It gives AI agents a compact glass UI, tray integration, token authorization, screenshots, mouse and keyboard control, and an emergency stop.

![Desktop MCP Control preview](docs/assets/desktop-mcp-control-preview.png)

## Highlights

- Compact premium glass control panel for starting, stopping, and monitoring the MCP service.
- Streamable HTTP endpoint at `http://127.0.0.1:45454/mcp` by default.
- Optional `Authorization: Bearer <token>` protection with copy and regenerate actions.
- Dynamic network settings for host, port, and protocol without restarting the app manually.
- Custom tray popup for opening the panel, available tools, settings, and exit.
- Global emergency stop hotkey: `Ctrl+Alt+Pause`.
- Desktop automation tools for screenshots, mouse, keyboard, drag-and-drop, and action previews.
- Dynamic platform capabilities via `desktop.get_capabilities`.

## Available tools

| Tool | Access | Description |
| --- | --- | --- |
| `desktop.mouse_move` | write | Move mouse to coordinates. |
| `desktop.mouse_click` | write | Click a mouse button. |
| `desktop.mouse_scroll` | write | Scroll at target coordinates. |
| `desktop.keyboard_type` | write | Type text into the focused window. |
| `desktop.keyboard_hotkey` | write | Press a keyboard shortcut. |
| `desktop.drag_drop` | write | Drag and drop between coordinates. |
| `desktop.get_capabilities` | read | Returns capability flags for current platform session. |
| `desktop.capture` | read | Capture display, window, or region screenshots. |
| `desktop.predict_click` | read | Render a screenshot preview of a future click. |
| `desktop.predict_swipe` | read | Render a screenshot preview of a future swipe. |
| `desktop.emergency_stop` | write | Stop queued and future actions until reset. |

## Requirements

- Windows 10/11 or Linux (X11 and Wayland best-effort).
- .NET SDK 10.0 or newer for development.
- A Streamable HTTP compatible MCP client.
- On Linux X11, install `xdotool` and `wmctrl`; install `imagemagick` (`import`) or `grim` for screenshot capture.

## Quick start

```powershell
git clone https://github.com/yakoodev/desktop-mcp-control.git
cd desktop-mcp-control
dotnet restore DesktopMcp.slnx
dotnet build DesktopMcp.slnx
dotnet run --project DesktopMcp.App
```

The app starts the MCP runtime automatically and shows the current endpoint in the main window.

## Releases

Every push to `main` builds, tests, publishes, and updates the GitHub Release for the current version from `Directory.Build.props`. The current release tag is `v1.0.0`.

Release assets:

- `desktop-mcp-control-v1.0.0-win-x64-portable.exe` - portable self-contained executable.
- `desktop-mcp-control-v1.0.0-win-x64-setup.exe` - WiX setup executable with Start Menu shortcut.
- `desktop-mcp-control-v1.0.0-linux-x64-portable.tar.gz` - Linux x64 portable archive.
- `SHA256SUMS.txt` - checksums for all artifacts.

## MCP connection

Default endpoint:

```text
http://127.0.0.1:45454/mcp
```

Generic Streamable HTTP client configuration:

```json
{
  "mcpServers": {
    "desktop-mcp-control": {
      "type": "streamable-http",
      "url": "http://127.0.0.1:45454/mcp"
    }
  }
}
```

If token authorization is enabled, add the bearer header:

```json
{
  "headers": {
    "Authorization": "Bearer <token>"
  }
}
```

Client configuration formats vary, so use the equivalent Streamable HTTP URL and header fields in your MCP client.

## Settings

User settings are stored locally in:

```text
Windows: %LocalAppData%\desktop-mcp-control\settings.json
Linux: ~/.local/share/desktop-mcp-control/settings.json
```

The settings window controls:

- Host, port, and protocol.
- Authorization mode: no auth or bearer token.
- Token copy, reveal, and regeneration.

Network and authorization changes are applied to the running MCP runtime after saving.

## Safety notes

This app can control your real desktop. Keep the endpoint bound to `127.0.0.1` unless you explicitly need network access. If you bind to `0.0.0.0` or any external interface, enable token authorization and restrict network access at the firewall level.

Use `Ctrl+Alt+Pause` or the `desktop.emergency_stop` tool to stop actions immediately.

On Linux, especially Wayland sessions, some desktop automation features can be unavailable depending on compositor and portal support. Use `desktop.get_capabilities` to detect supported actions at runtime and handle `capability_unavailable` errors gracefully.

## Development

```powershell
dotnet build DesktopMcp.slnx
dotnet test DesktopMcp.slnx
```

Main projects:

- `DesktopMcp.App` - Avalonia UI, tray integration, settings, and view models.
- `DesktopMcp.Mcp` - MCP runtime, authorization, and tool definitions.
- `DesktopMcp.Core` - platform backends for Windows, Linux X11, and Linux Wayland best-effort modes.
- `DesktopMcp.Tests` - unit tests for runtime, settings, and view model behavior.

## Repository status

The first public release is `v1.0.0`. The project now includes Windows support and Linux support (X11 + Wayland best-effort capability model).


