using DesktopMcp.App.Models;
using DesktopMcp.App.Services;
using DesktopMcp.Mcp;

namespace DesktopMcp.Tests;

public class AppSettingsStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsAuthorizationSettings()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "desktop-mcp-tests", $"{Guid.NewGuid():N}", "settings.json");
        var store = new AppSettingsStore(tempPath);

        store.Save(
            new AppSettings
            {
                AuthMode = AuthMode.Token,
                Token = "  abc-token  ",
                NetworkHost = "  0.0.0.0  ",
                NetworkPort = " 45555 ",
                NetworkProtocol = "https"
            });

        var loaded = store.Load();

        Assert.Equal(AuthMode.Token, loaded.AuthMode);
        Assert.Equal("abc-token", loaded.Token);
        Assert.Equal("0.0.0.0", loaded.NetworkHost);
        Assert.Equal("45555", loaded.NetworkPort);
        Assert.Equal("HTTPS", loaded.NetworkProtocol);
    }

    [Fact]
    public void Load_ReturnsDefaultsForInvalidJson()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "desktop-mcp-tests", $"{Guid.NewGuid():N}", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
        File.WriteAllText(tempPath, "{ invalid json");

        var store = new AppSettingsStore(tempPath);
        var loaded = store.Load();

        Assert.Equal(AuthMode.None, loaded.AuthMode);
        Assert.Equal(string.Empty, loaded.Token);
        Assert.Equal("127.0.0.1", loaded.NetworkHost);
        Assert.Equal("45454", loaded.NetworkPort);
        Assert.Equal("HTTP", loaded.NetworkProtocol);
    }
}
