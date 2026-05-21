using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Services;
using DesktopMcp.Core.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopMcp.Core.DependencyInjection;

public static class DesktopAutomationServiceCollectionExtensions
{
    public static IServiceCollection AddDesktopAutomationCore(this IServiceCollection services)
    {
        services.AddSingleton<IDisplayService, Win32DisplayService>();
        services.AddSingleton<IWindowService, Win32WindowService>();
        services.AddSingleton<IInputAutomationService, Win32InputAutomationService>();
        services.AddSingleton<IScreenshotService, GdiScreenshotService>();
        services.AddSingleton<IActionQueue, SerializedActionQueue>();
        services.AddSingleton<IEmergencyStopService, EmergencyStopService>();
        services.AddSingleton<IDesktopAutomationController, DesktopAutomationController>();
        return services;
    }
}
