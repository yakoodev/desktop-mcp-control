using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;

namespace DesktopMcp.App.Services;

public sealed class AvaloniaTrayIntegration : ITrayIntegration
{
    private TrayIcon? _trayIcon;

    public bool IsSupported => true;

    public void Initialize(
        Action onOpenControlPanel,
        Action onOpenTools,
        Action onOpenSettings,
        Func<Task> onExitAsync)
    {
        if (_trayIcon is not null)
        {
            return;
        }

        using var iconStream = AssetLoader.Open(new Uri("avares://DesktopMcp.App/Assets/icon.png"));
        var menu = BuildMenu(onOpenControlPanel, onOpenTools, onOpenSettings, onExitAsync);

        _trayIcon = new TrayIcon
        {
            ToolTipText = "Desktop MCP",
            Icon = new WindowIcon(iconStream),
            IsVisible = true,
            Menu = menu
        };

        _trayIcon.Clicked += (_, _) =>
        {
            Dispatcher.UIThread.Post(onOpenControlPanel);
        };
    }

    public void Dispose()
    {
        if (_trayIcon is null)
        {
            return;
        }

        _trayIcon.IsVisible = false;
        _trayIcon.Dispose();
        _trayIcon = null;
    }

    private static NativeMenu BuildMenu(
        Action onOpenControlPanel,
        Action onOpenTools,
        Action onOpenSettings,
        Func<Task> onExitAsync)
    {
        return new NativeMenu
        {
            Items =
            {
                CreateItem("Open Control Panel", onOpenControlPanel),
                CreateItem("Available Tools", onOpenTools),
                CreateItem("Settings", onOpenSettings),
                new NativeMenuItemSeparator(),
                CreateItem("Exit", () => _ = onExitAsync())
            }
        };
    }

    private static NativeMenuItem CreateItem(string text, Action onClick)
    {
        var item = new NativeMenuItem(text);
        item.Click += (_, _) => Dispatcher.UIThread.Post(onClick);
        return item;
    }
}
