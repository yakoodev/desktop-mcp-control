using System.Text.Json;
using DesktopMcp.App.Models;
using DesktopMcp.Mcp;

namespace DesktopMcp.App.Services;

public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public AppSettingsStore(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? BuildDefaultSettingsPath();
    }

    public string SettingsPath => _settingsPath;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return Normalize(settings);
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var normalized = Normalize(settings);
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private static AppSettings Normalize(AppSettings? settings)
    {
        if (settings is null)
        {
            return new AppSettings();
        }

        var token = settings.Token?.Trim() ?? string.Empty;
        var mode = settings.AuthMode is AuthMode.None or AuthMode.Token
            ? settings.AuthMode
            : AuthMode.None;
        var host = string.IsNullOrWhiteSpace(settings.NetworkHost)
            ? "127.0.0.1"
            : settings.NetworkHost.Trim();
        var port = int.TryParse(settings.NetworkPort?.Trim(), out var parsedPort) &&
            parsedPort is >= 1 and <= 65535
                ? parsedPort.ToString()
                : "45454";
        var protocol = string.Equals(settings.NetworkProtocol, "HTTPS", StringComparison.OrdinalIgnoreCase)
            ? "HTTPS"
            : "HTTP";

        return new AppSettings
        {
            AuthMode = mode,
            Token = token,
            NetworkHost = host,
            NetworkPort = port,
            NetworkProtocol = protocol
        };
    }

    private static string BuildDefaultSettingsPath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "desktop-mcp-control", "settings.json");
    }
}
