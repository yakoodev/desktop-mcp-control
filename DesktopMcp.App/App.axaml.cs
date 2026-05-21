using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using DesktopMcp.App.Models;
using DesktopMcp.App.Services;
using DesktopMcp.App.ViewModels;
using DesktopMcp.App.Views;
using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.DependencyInjection;
using DesktopMcp.Mcp;
using DesktopMcp.Mcp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace DesktopMcp.App;

public partial class App : Avalonia.Application
{
    private ServiceProvider? _serviceProvider;
    private MainWindowViewModel? _viewModel;
    private IMcpServerRuntime? _mcpRuntime;
    private IDesktopAutomationController? _controller;
    private AppSettingsStore? _settingsStore;
    private IClassicDesktopStyleApplicationLifetime? _desktopLifetime;
    private MainWindow? _mainWindow;
    private TrayPopupWindow? _trayPopupWindow;
    private AvailableToolsWindow? _availableToolsWindow;
    private SettingsWindow? _settingsWindow;
    private GlobalEmergencyHotkeyListener? _hotkeyListener;
    private Forms.NotifyIcon? _notifyIcon;
    private Drawing.Icon? _notifyIconImage;
    private DispatcherTimer? _statusTimer;
    private bool _isShuttingDown;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktopLifetime = desktop;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _serviceProvider = BuildServiceProvider();
            _viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            _mcpRuntime = _serviceProvider.GetRequiredService<IMcpServerRuntime>();
            _controller = _serviceProvider.GetRequiredService<IDesktopAutomationController>();
            _settingsStore = _serviceProvider.GetRequiredService<AppSettingsStore>();
            _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

            var settings = _settingsStore.Load();
            _viewModel.InitializeAuthorization(settings.AuthMode, settings.Token);
            _viewModel.InitializeNetworkSettings(settings);

            _mainWindow.DataContext = _viewModel;
            _mainWindow.Closing += OnMainWindowClosing;

            _trayPopupWindow = new TrayPopupWindow
            {
                DataContext = _viewModel
            };
            _trayPopupWindow.Closing += OnTrayPopupClosing;
            _trayPopupWindow.Deactivated += OnTrayPopupDeactivated;

            _availableToolsWindow = new AvailableToolsWindow
            {
                DataContext = _viewModel
            };
            _availableToolsWindow.Closing += OnAuxiliaryWindowClosing;

            _settingsWindow = new SettingsWindow
            {
                DataContext = _viewModel
            };
            _settingsWindow.Closing += OnAuxiliaryWindowClosing;

            _viewModel.HideToTrayRequested += (_, _) =>
            {
                if (_mainWindow is not null)
                {
                    HideAuxiliaryWindows();
                    HideMainWindow(_mainWindow);
                }
            };
            _viewModel.OpenControlPanelRequested += (_, _) =>
            {
                if (_mainWindow is not null)
                {
                    ShowMainWindow(_mainWindow);
                }
            };
            _viewModel.ToolsRequested += (_, _) => ShowAvailableToolsWindow();
            _viewModel.SettingsRequested += (_, _) => ShowSettingsWindow();
            _viewModel.SettingsSavedRequested += (_, _) =>
            {
                PersistSettings();
                HideSettingsWindow();
            };
            _viewModel.CopyRequested += text => _ = CopyTextToClipboardAsync(text);
            _viewModel.ExitRequested += async (_, _) => await ExitApplicationAsync();

            desktop.MainWindow = _mainWindow;
            desktop.Exit += OnDesktopExit;

            ConfigureTray(_mainWindow);
            ConfigureHotkey();
            ConfigureStatusTimer();

            _ = StartServerInBackgroundAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddDesktopAutomationCore();
        services.AddDesktopMcpServer(new McpServerRuntimeOptions("http://127.0.0.1:45454", "/mcp"));
        services.AddSingleton<AppSettingsStore>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
        return services.BuildServiceProvider();
    }

    private async Task StartServerInBackgroundAsync()
    {
        if (_mcpRuntime is null || _viewModel is null)
        {
            return;
        }

        try
        {
            await _mcpRuntime.StartAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _viewModel.SetEmergencyState($"Start error: {ex.Message}");
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _viewModel.RefreshStatus();
            });
        }
    }

    private void ConfigureStatusTimer()
    {
        if (_viewModel is null)
        {
            return;
        }

        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _statusTimer.Tick += (_, _) => _viewModel.RefreshStatus();
        _statusTimer.Start();
    }

    private void ConfigureHotkey()
    {
        if (_controller is null || _viewModel is null)
        {
            return;
        }

        _hotkeyListener = new GlobalEmergencyHotkeyListener();
        _hotkeyListener.Triggered += (_, _) =>
        {
            _controller.TriggerEmergencyStop("Global emergency hotkey Ctrl+Alt+Pause.");
            _viewModel.SetEmergencyState($"STOPPED (hotkey {DateTime.Now:HH:mm:ss})");
        };
        _hotkeyListener.Start();
    }

    private void ConfigureTray(Window mainWindow)
    {
        using var iconStream = AssetLoader.Open(new Uri("avares://DesktopMcp.App/Assets/icon.png"));
        using var bitmap = new Drawing.Bitmap(iconStream);
        _notifyIconImage = Drawing.Icon.FromHandle(bitmap.GetHicon());
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Desktop MCP",
            Icon = _notifyIconImage,
            Visible = true
        };

        _notifyIcon.MouseClick += (_, e) =>
        {
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (e.Button == Forms.MouseButtons.Left)
                    {
                        ShowMainWindow(mainWindow);
                        return;
                    }

                    if (e.Button == Forms.MouseButtons.Right)
                    {
                        ShowTrayPopup();
                    }
                });
        };
    }

    private void ShowTrayPopup()
    {
        if (_trayPopupWindow is null)
        {
            return;
        }

        var screens = _mainWindow?.Screens ?? _trayPopupWindow.Screens;
        var targetScreen = screens?.Primary ?? screens?.All.FirstOrDefault();
        if (targetScreen is not null)
        {
            var workArea = targetScreen.WorkingArea;
            var popupWidth = (int)_trayPopupWindow.Width;
            var popupHeight = (int)_trayPopupWindow.Height;
            var x = workArea.X + Math.Max(0, workArea.Width - popupWidth - 10);
            var y = workArea.Y + Math.Max(0, workArea.Height - popupHeight - 14);
            _trayPopupWindow.Position = new PixelPoint(x, y);
        }

        if (!_trayPopupWindow.IsVisible)
        {
            _trayPopupWindow.Show();
        }

        _trayPopupWindow.Activate();
    }

    private void ShowAvailableToolsWindow()
    {
        if (_availableToolsWindow is null)
        {
            return;
        }

        ShowAuxiliaryWindow(_availableToolsWindow, verticalOffset: 0);
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow is null)
        {
            return;
        }

        _viewModel?.ResetSettingsDraft();
        ShowAuxiliaryWindow(_settingsWindow, verticalOffset: 540);
    }

    private void HideSettingsWindow()
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Hide();
        }
    }

    private void HideAuxiliaryWindows()
    {
        if (_availableToolsWindow is { IsVisible: true })
        {
            _availableToolsWindow.Hide();
        }

        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Hide();
        }
    }

    private void ShowAuxiliaryWindow(Window window, int verticalOffset)
    {
        HideTrayPopup();

        var screens = _mainWindow?.Screens ?? window.Screens;
        var targetScreen = screens?.Primary ?? screens?.All.FirstOrDefault();
        if (targetScreen is not null)
        {
            var workArea = targetScreen.WorkingArea;
            var x = workArea.X + Math.Max(0, workArea.Width - (int)window.Width - 36);
            var y = workArea.Y + 36 + verticalOffset;

            if (_mainWindow is { IsVisible: true } mainWindow)
            {
                x = mainWindow.Position.X + (int)mainWindow.Width + 28;
                y = mainWindow.Position.Y + verticalOffset;
            }

            var maxX = workArea.X + workArea.Width - (int)window.Width - 16;
            var maxY = workArea.Y + workArea.Height - (int)window.Height - 16;
            window.Position = new PixelPoint(
                Math.Clamp(x, workArea.X + 16, Math.Max(workArea.X + 16, maxX)),
                Math.Clamp(y, workArea.Y + 16, Math.Max(workArea.Y + 16, maxY)));
        }

        if (!window.IsVisible)
        {
            window.Show();
        }
        else
        {
            window.Activate();
        }
    }

    private void HideTrayPopup()
    {
        if (_trayPopupWindow is { IsVisible: true })
        {
            _trayPopupWindow.Hide();
        }
    }

    private async Task CopyTextToClipboardAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(
            async () =>
            {
                if (_mainWindow?.Clipboard is { } clipboard)
                {
                    await clipboard.SetTextAsync(text);
                }
            });
    }

    private void PersistSettings()
    {
        if (_settingsStore is null || _viewModel is null)
        {
            return;
        }

        _settingsStore.Save(_viewModel.CreateSettingsSnapshot());
    }

    private async Task ExitApplicationAsync()
    {
        if (_isShuttingDown)
        {
            return;
        }

        _isShuttingDown = true;
        HideTrayPopup();

        if (_mcpRuntime is not null)
        {
            await _mcpRuntime.StopAsync().ConfigureAwait(false);
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _desktopLifetime?.TryShutdown();
        });
    }

    private void ShowMainWindow(Window mainWindow)
    {
        HideTrayPopup();
        mainWindow.ShowInTaskbar = true;
        mainWindow.Show();
        mainWindow.Activate();
        if (mainWindow.WindowState == WindowState.Minimized)
        {
            mainWindow.WindowState = WindowState.Normal;
        }
    }

    private static void HideMainWindow(Window mainWindow)
    {
        mainWindow.ShowInTaskbar = false;
        mainWindow.Hide();
    }

    private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isShuttingDown)
        {
            return;
        }

        if (sender is Window mainWindow)
        {
            e.Cancel = true;
            HideMainWindow(mainWindow);
        }
    }

    private void OnTrayPopupClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isShuttingDown)
        {
            return;
        }

        e.Cancel = true;
        HideTrayPopup();
    }

    private void OnTrayPopupDeactivated(object? sender, EventArgs e)
    {
        if (_isShuttingDown)
        {
            return;
        }

        HideTrayPopup();
    }

    private void OnAuxiliaryWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isShuttingDown)
        {
            return;
        }

        e.Cancel = true;
        if (sender is Window window)
        {
            window.Hide();
        }
    }

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _isShuttingDown = true;
        _statusTimer?.Stop();
        _hotkeyListener?.Dispose();
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        _notifyIconImage?.Dispose();
        _trayPopupWindow?.Close();
        _availableToolsWindow?.Close();
        _settingsWindow?.Close();

        if (_mcpRuntime is not null)
        {
            _mcpRuntime.StopAsync().GetAwaiter().GetResult();
            _mcpRuntime.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        _serviceProvider?.Dispose();
    }
}
