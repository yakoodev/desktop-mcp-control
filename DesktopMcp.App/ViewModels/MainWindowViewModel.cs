using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using DesktopMcp.App.Models;
using DesktopMcp.Core.Abstractions;
using DesktopMcp.Mcp;
using System.Collections.ObjectModel;
using System.Globalization;

namespace DesktopMcp.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const int EnabledToolCount = 10;
    private const int ToastDurationMs = 1600;

    private readonly IMcpServerRuntime _runtime;
    private readonly IDesktopAutomationController _controller;
    private CancellationTokenSource? _toastCancellation;

    [ObservableProperty]
    private bool _isServerRunning;

    [ObservableProperty]
    private string _endpoint = string.Empty;

    [ObservableProperty]
    private string _startedAt = "—";

    [ObservableProperty]
    private string _lastError = "—";

    [ObservableProperty]
    private string _emergencyState = "Ready";

    [ObservableProperty]
    private string _hotkeyHint = "Ctrl+Alt+Pause";

    [ObservableProperty]
    private bool _isTokenAuthEnabled;

    [ObservableProperty]
    private string _token = string.Empty;

    [ObservableProperty]
    private bool _isTokenVisible;

    [ObservableProperty]
    private bool _canCopyToken;

    [ObservableProperty]
    private bool _showTokenPlaceholder = true;

    [ObservableProperty]
    private string _statusText = "Stopped";

    [ObservableProperty]
    private string _authModeLabel = "No auth";

    [ObservableProperty]
    private string _authTokenPreview = "—";

    [ObservableProperty]
    private string _toolsSummary = $"{EnabledToolCount} tools enabled";

    [ObservableProperty]
    private string _networkHost = "127.0.0.1";

    [ObservableProperty]
    private string _networkPort = "45454";

    [ObservableProperty]
    private string _networkProtocol = "HTTP";

    [ObservableProperty]
    private string _settingsHost = "127.0.0.1";

    [ObservableProperty]
    private string _settingsPort = "45454";

    [ObservableProperty]
    private string _settingsProtocol = "HTTP";

    [ObservableProperty]
    private bool _settingsIsTokenAuthEnabled;

    [ObservableProperty]
    private string _settingsToken = string.Empty;

    [ObservableProperty]
    private bool _settingsIsTokenVisible;

    [ObservableProperty]
    private string _settingsAuthModeLabel = "No auth";

    [ObservableProperty]
    private bool _isToastVisible;

    [ObservableProperty]
    private string _toastText = string.Empty;

    public string[] ProtocolOptions { get; } = ["HTTP", "HTTPS"];

    public MainWindowViewModel(
        IMcpServerRuntime runtime,
        IDesktopAutomationController controller)
    {
        _runtime = runtime;
        _controller = controller;

        ToggleServerCommand = new AsyncRelayCommand(ToggleServerAsync);
        ToggleAuthModeCommand = new RelayCommand(() => SettingsIsTokenAuthEnabled = !SettingsIsTokenAuthEnabled);
        ToggleTokenVisibilityCommand = new RelayCommand(() => SettingsIsTokenVisible = !SettingsIsTokenVisible);
        CopyEndpointCommand = new RelayCommand(CopyEndpoint);
        CopyTokenCommand = new RelayCommand(CopyToken);
        CopySettingsTokenCommand = new RelayCommand(CopySettingsToken);
        RegenerateTokenCommand = new RelayCommand(RegenerateToken);
        EmergencyStopCommand = new RelayCommand(EmergencyStop);
        EmergencyResetCommand = new RelayCommand(EmergencyReset);
        OpenControlPanelCommand = new RelayCommand(() => OpenControlPanelRequested?.Invoke(this, EventArgs.Empty));
        OpenToolsCommand = new RelayCommand(() => ToolsRequested?.Invoke(this, EventArgs.Empty));
        OpenSettingsCommand = new RelayCommand(() => SettingsRequested?.Invoke(this, EventArgs.Empty));
        ResetSettingsCommand = new RelayCommand(ResetSettingsDraft);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        HideToTrayCommand = new RelayCommand(() => HideToTrayRequested?.Invoke(this, EventArgs.Empty));
        ExitApplicationCommand = new RelayCommand(() => ExitRequested?.Invoke(this, EventArgs.Empty));

        AvailableTools = new ObservableCollection<ToolDefinitionViewModel>(
            new[]
            {
                ToolDefinitionViewModel.Write("desktop.mouse_move", "Move mouse to coordinates"),
                ToolDefinitionViewModel.Write("desktop.mouse_click", "Click mouse button"),
                ToolDefinitionViewModel.Write("desktop.mouse_scroll", "Scroll mouse wheel"),
                ToolDefinitionViewModel.Write("desktop.keyboard_type", "Type text on keyboard"),
                ToolDefinitionViewModel.Write("desktop.keyboard_hotkey", "Press keyboard hotkey"),
                ToolDefinitionViewModel.Write("desktop.drag_drop", "Drag and drop"),
                ToolDefinitionViewModel.Read("desktop.capture", "Capture screenshot"),
                ToolDefinitionViewModel.Read("desktop.predict_click", "AI predict click position"),
                ToolDefinitionViewModel.Read("desktop.predict_swipe", "AI predict swipe direction"),
                ToolDefinitionViewModel.Write("desktop.emergency_stop", "Emergency stop all actions"),
            });

        RefreshStatus();
    }

    public event EventHandler? HideToTrayRequested;

    public event EventHandler? OpenControlPanelRequested;

    public event EventHandler? ToolsRequested;

    public event EventHandler? SettingsRequested;

    public event EventHandler? SettingsSavedRequested;

    public event EventHandler? ExitRequested;

    public event Action<string>? CopyRequested;

    public IAsyncRelayCommand ToggleServerCommand { get; }

    public IRelayCommand ToggleAuthModeCommand { get; }

    public IRelayCommand ToggleTokenVisibilityCommand { get; }

    public IRelayCommand CopyEndpointCommand { get; }

    public IRelayCommand CopyTokenCommand { get; }

    public IRelayCommand CopySettingsTokenCommand { get; }

    public IRelayCommand RegenerateTokenCommand { get; }

    public IRelayCommand EmergencyStopCommand { get; }

    public IRelayCommand EmergencyResetCommand { get; }

    public IRelayCommand OpenControlPanelCommand { get; }

    public IRelayCommand OpenToolsCommand { get; }

    public IRelayCommand OpenSettingsCommand { get; }

    public IRelayCommand ResetSettingsCommand { get; }

    public IAsyncRelayCommand SaveSettingsCommand { get; }

    public IRelayCommand HideToTrayCommand { get; }

    public IRelayCommand ExitApplicationCommand { get; }

    public ObservableCollection<ToolDefinitionViewModel> AvailableTools { get; }

    public void InitializeAuthorization(AuthMode mode, string? token)
    {
        _runtime.SetAuthorization(mode, token);
        ApplyAuthorizationSnapshot(_runtime.Authorization);
    }

    public void InitializeNetworkSettings(AppSettings settings)
    {
        SettingsHost = NormalizeHost(settings.NetworkHost);
        SettingsPort = NormalizePort(settings.NetworkPort);
        SettingsProtocol = NormalizeProtocol(settings.NetworkProtocol);

        _runtime.ConfigureEndpointAsync(BuildRuntimeUrl(SettingsHost, SettingsPort, SettingsProtocol))
            .GetAwaiter()
            .GetResult();

        RefreshStatus();
        ResetSettingsDraft();
    }

    public AppSettings CreateSettingsSnapshot()
    {
        return new AppSettings
        {
            AuthMode = IsTokenAuthEnabled ? AuthMode.Token : AuthMode.None,
            Token = Token,
            NetworkHost = NetworkHost,
            NetworkPort = NetworkPort,
            NetworkProtocol = NetworkProtocol
        };
    }

    public void RefreshStatus()
    {
        var status = _runtime.Status;
        IsServerRunning = status.IsRunning;
        Endpoint = status.Endpoint;
        RefreshNetworkFields(status.Endpoint);
        StartedAt = status.StartedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "—";
        LastError = string.IsNullOrWhiteSpace(status.LastError) ? "—" : status.LastError;
        ApplyAuthorizationSnapshot(_runtime.Authorization);
        UpdateDerivedLabels();
    }

    public void SetEmergencyState(string text)
    {
        EmergencyState = text;
    }

    private async Task StartServerAsync()
    {
        await _runtime.StartAsync();
        RefreshStatus();
    }

    private async Task StopServerAsync()
    {
        await _runtime.StopAsync();
        RefreshStatus();
    }

    private async Task ToggleServerAsync()
    {
        if (_runtime.Status.IsRunning)
        {
            await StopServerAsync();
        }
        else
        {
            await StartServerAsync();
        }
    }

    private void EmergencyStop()
    {
        _controller.TriggerEmergencyStop("Emergency stop from UI.");
        EmergencyState = "STOPPED (manual)";
    }

    private void EmergencyReset()
    {
        _controller.ResetEmergencyStop();
        EmergencyState = "Ready";
    }

    private void RegenerateToken()
    {
        SettingsToken = McpAuthToken.Generate();
        SettingsIsTokenAuthEnabled = true;
    }

    private void CopyEndpoint()
    {
        if (!string.IsNullOrWhiteSpace(Endpoint))
        {
            CopyRequested?.Invoke(Endpoint);
            ShowToast("Endpoint copied");
        }
    }

    private void CopyToken()
    {
        if (!string.IsNullOrWhiteSpace(Token))
        {
            CopyRequested?.Invoke(Token);
            ShowToast("Token copied");
        }
    }

    private void CopySettingsToken()
    {
        if (!string.IsNullOrWhiteSpace(SettingsToken))
        {
            CopyRequested?.Invoke(SettingsToken);
            ShowToast("Token copied");
        }
    }

    private void ShowToast(string text)
    {
        _toastCancellation?.Cancel();
        _toastCancellation = new CancellationTokenSource();
        _ = ShowToastAsync(text, _toastCancellation.Token);
    }

    private async Task ShowToastAsync(string text, CancellationToken cancellationToken)
    {
        ToastText = text;
        IsToastVisible = true;

        try
        {
            await Task.Delay(ToastDurationMs, cancellationToken);
            IsToastVisible = false;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task SaveSettingsAsync()
    {
        var host = NormalizeHost(SettingsHost);
        var port = NormalizePort(SettingsPort);
        var protocol = NormalizeProtocol(SettingsProtocol);
        var url = BuildRuntimeUrl(host, port, protocol);
        var authMode = SettingsIsTokenAuthEnabled ? AuthMode.Token : AuthMode.None;
        var token = SettingsToken.Trim();

        if (authMode == AuthMode.Token && string.IsNullOrWhiteSpace(token))
        {
            token = McpAuthToken.Generate();
        }

        try
        {
            await _runtime.ConfigureEndpointAsync(url);
            _runtime.SetAuthorization(authMode, token);

            SettingsHost = host;
            SettingsPort = port;
            SettingsProtocol = protocol;
            SettingsIsTokenAuthEnabled = authMode == AuthMode.Token;
            SettingsToken = token;
            RefreshStatus();
            ResetSettingsDraft();
            SettingsSavedRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            EmergencyState = $"Settings error: {ex.Message}";
            RefreshStatus();
        }
    }

    partial void OnIsServerRunningChanged(bool value)
    {
        StatusText = value ? "Running" : "Stopped";
    }

    partial void OnIsTokenAuthEnabledChanged(bool value)
    {
        UpdateDerivedLabels();
    }

    partial void OnTokenChanged(string value)
    {
        UpdateDerivedLabels();
    }

    partial void OnSettingsIsTokenAuthEnabledChanged(bool value)
    {
        if (value && string.IsNullOrWhiteSpace(SettingsToken))
        {
            SettingsToken = McpAuthToken.Generate();
        }

        SettingsAuthModeLabel = value ? "Token (Bearer)" : "No auth";
    }

    private void ApplyAuthorizationSnapshot(McpAuthorizationState state)
    {
        IsTokenAuthEnabled = state.Mode == AuthMode.Token;
        Token = state.Token;
        UpdateDerivedLabels();
    }

    private void UpdateDerivedLabels()
    {
        StatusText = IsServerRunning ? "Running" : "Stopped";
        AuthModeLabel = IsTokenAuthEnabled ? "Token (Bearer)" : "No auth";
        CanCopyToken = IsTokenAuthEnabled && !string.IsNullOrWhiteSpace(Token);
        ShowTokenPlaceholder = !CanCopyToken;
        AuthTokenPreview = CanCopyToken ? MaskToken(Token) : "—";
        ToolsSummary = $"{EnabledToolCount} tools enabled";
    }

    private void RefreshNetworkFields(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return;
        }

        NetworkHost = uri.Host;
        NetworkPort = uri.Port.ToString(CultureInfo.InvariantCulture);
        NetworkProtocol = uri.Scheme.ToUpperInvariant();
    }

    public void ResetSettingsDraft()
    {
        SettingsHost = NetworkHost;
        SettingsPort = NetworkPort;
        SettingsProtocol = NetworkProtocol;
        SettingsIsTokenAuthEnabled = IsTokenAuthEnabled;
        SettingsToken = Token;
        SettingsIsTokenVisible = false;
        SettingsAuthModeLabel = SettingsIsTokenAuthEnabled ? "Token (Bearer)" : "No auth";
    }

    private static string NormalizeHost(string? host)
    {
        return string.IsNullOrWhiteSpace(host)
            ? "127.0.0.1"
            : host.Trim();
    }

    private static string NormalizePort(string? port)
    {
        if (int.TryParse(port?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var parsedPort) &&
            parsedPort is >= 1 and <= 65535)
        {
            return parsedPort.ToString(CultureInfo.InvariantCulture);
        }

        return "45454";
    }

    private static string NormalizeProtocol(string? protocol)
    {
        return string.Equals(protocol, "HTTPS", StringComparison.OrdinalIgnoreCase)
            ? "HTTPS"
            : "HTTP";
    }

    private static string BuildRuntimeUrl(string host, string port, string protocol)
    {
        return $"{protocol.ToLowerInvariant()}://{host}:{port}";
    }

    private static string MaskToken(string token)
    {
        const int maxMaskedTailLength = 16;

        var normalized = token.Trim();
        if (normalized.Length <= 5)
        {
            return normalized;
        }

        return normalized[..5] + new string('*', Math.Min(maxMaskedTailLength, normalized.Length - 5));
    }
}

public sealed record ToolDefinitionViewModel(
    string Name,
    string Description,
    string Access,
    IBrush AccentBrush,
    IBrush BadgeBackground)
{
    public static ToolDefinitionViewModel Write(string name, string description)
    {
        return new ToolDefinitionViewModel(
            name,
            description,
            "write",
            SolidColorBrush.Parse("#64E28B"),
            SolidColorBrush.Parse("#203C2E"));
    }

    public static ToolDefinitionViewModel Read(string name, string description)
    {
        return new ToolDefinitionViewModel(
            name,
            description,
            "read",
            SolidColorBrush.Parse("#82A5FF"),
            SolidColorBrush.Parse("#23335F"));
    }
}
