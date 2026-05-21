using System.Diagnostics;
using System.Text;
using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Windows;

public sealed class Win32WindowService : IWindowService
{
    public IReadOnlyList<WindowInfo> ListWindows(WindowQuery query)
    {
        var results = new List<WindowInfo>();
        var foreground = NativeMethods.GetForegroundWindow();

        NativeMethods.EnumWindows(
            (hWnd, _) =>
            {
                var isVisible = NativeMethods.IsWindowVisible(hWnd);
                if (query.OnlyVisible && !isVisible)
                {
                    return true;
                }

                if (!NativeMethods.GetWindowRect(hWnd, out var rect))
                {
                    return true;
                }

                var bounds = rect.ToScreenRect();
                if (bounds.IsEmpty)
                {
                    return true;
                }

                var title = GetWindowTitle(hWnd);
                if (!string.IsNullOrWhiteSpace(query.TitleContains) &&
                    (title.Length == 0 ||
                     title.IndexOf(query.TitleContains, StringComparison.OrdinalIgnoreCase) < 0))
                {
                    return true;
                }

                var processName = GetProcessName(hWnd);
                if (!string.IsNullOrWhiteSpace(query.ProcessName) &&
                    processName.IndexOf(query.ProcessName, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return true;
                }

                results.Add(
                    new WindowInfo(
                        WindowIdFromHandle(hWnd),
                        title,
                        processName,
                        bounds,
                        isVisible,
                        hWnd == foreground));

                return true;
            },
            nint.Zero);

        return results
            .OrderByDescending(w => w.IsForeground)
            .ThenBy(w => w.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool TryGetWindowBounds(string windowId, out ScreenRect bounds)
    {
        bounds = default;

        if (!TryParseWindowHandle(windowId, out var handle))
        {
            return false;
        }

        if (!NativeMethods.GetWindowRect(handle, out var rect))
        {
            return false;
        }

        bounds = rect.ToScreenRect();
        return !bounds.IsEmpty;
    }

    private static string GetWindowTitle(nint hWnd)
    {
        var length = NativeMethods.GetWindowTextLengthW(hWnd);
        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        _ = NativeMethods.GetWindowTextW(hWnd, builder, builder.Capacity);
        return builder.ToString().Trim();
    }

    private static string GetProcessName(nint hWnd)
    {
        try
        {
            _ = NativeMethods.GetWindowThreadProcessId(hWnd, out var processId);
            if (processId == 0)
            {
                return string.Empty;
            }

            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string WindowIdFromHandle(nint hWnd)
    {
        return $"0x{hWnd.ToInt64():X}";
    }

    private static bool TryParseWindowHandle(string windowId, out nint handle)
    {
        handle = nint.Zero;
        if (string.IsNullOrWhiteSpace(windowId))
        {
            return false;
        }

        var raw = windowId.Trim();
        if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            raw = raw[2..];
        }

        if (!long.TryParse(raw, System.Globalization.NumberStyles.HexNumber, null, out var value))
        {
            return false;
        }

        handle = new nint(value);
        return handle != nint.Zero;
    }
}
