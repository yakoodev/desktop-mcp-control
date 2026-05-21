using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Windows;

public sealed class Win32DisplayService : IDisplayService
{
    public VirtualDesktopInfo GetVirtualDesktopInfo()
    {
        var displays = new List<DisplayInfo>();
        var index = 0;

        NativeMethods.EnumDisplayMonitors(
            nint.Zero,
            nint.Zero,
            delegate(nint hMonitor, nint _, ref NativeMethods.Rect __, nint ___)
            {
                var monitorInfo = new NativeMethods.MonitorInfoEx
                {
                    CbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MonitorInfoEx>(),
                    SzDevice = string.Empty
                };

                if (!NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo))
                {
                    return true;
                }

                var bounds = monitorInfo.RcMonitor.ToScreenRect();
                var isPrimary = (monitorInfo.DwFlags & 1U) == 1U;
                var dpiScale = 1d;

                var hr = NativeMethods.GetDpiForMonitor(
                    hMonitor,
                    NativeMethods.MdtEffectiveDpi,
                    out var dpiX,
                    out var dpiY);

                if (hr == 0 && dpiX > 0 && dpiY > 0)
                {
                    dpiScale = dpiX / 96d;
                }

                index++;
                var displayId = $"{monitorInfo.SzDevice.Trim()}#{index}";
                displays.Add(new DisplayInfo(displayId, bounds, isPrimary, dpiScale));
                return true;
            },
            nint.Zero);

        var virtualBounds = new ScreenRect(
            NativeMethods.GetSystemMetrics(NativeMethods.SmXVirtualScreen),
            NativeMethods.GetSystemMetrics(NativeMethods.SmYVirtualScreen),
            NativeMethods.GetSystemMetrics(NativeMethods.SmCxVirtualScreen),
            NativeMethods.GetSystemMetrics(NativeMethods.SmCyVirtualScreen));

        return new VirtualDesktopInfo(virtualBounds, displays);
    }
}
