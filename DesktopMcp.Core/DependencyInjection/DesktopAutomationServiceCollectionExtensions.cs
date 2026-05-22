using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Linux;
using DesktopMcp.Core.Linux.Wayland;
using DesktopMcp.Core.Linux.X11;
using DesktopMcp.Core.Services;
using DesktopMcp.Core.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;

namespace DesktopMcp.Core.DependencyInjection;

public static class DesktopAutomationServiceCollectionExtensions
{
    public static IServiceCollection AddDesktopAutomationCore(this IServiceCollection services)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IDisplayService, Win32DisplayService>();
            services.AddSingleton<IWindowService, Win32WindowService>();
            services.AddSingleton<IInputAutomationService, Win32InputAutomationService>();
            services.AddSingleton<IScreenshotService, GdiScreenshotService>();
            services.AddSingleton<IDesktopAutomationBackend, Win32DesktopAutomationBackend>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (LinuxDesktopCapabilityProbe.IsWaylandSession())
            {
                services.AddSingleton<IDesktopAutomationBackend, WaylandPortalDesktopAutomationBackend>();
            }
            else
            {
                services.AddSingleton<IDesktopAutomationBackend, X11DesktopAutomationBackend>();
            }
        }
        else
        {
            services.AddSingleton<IDesktopAutomationBackend, UnsupportedDesktopAutomationBackend>();
        }

        services.AddSingleton<IActionQueue, SerializedActionQueue>();
        services.AddSingleton<IEmergencyStopService, EmergencyStopService>();
        services.AddSingleton<IDesktopAutomationController, DesktopAutomationController>();
        return services;
    }
}
