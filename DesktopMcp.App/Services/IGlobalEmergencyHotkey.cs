namespace DesktopMcp.App.Services;

public interface IGlobalEmergencyHotkey : IDisposable
{
    event EventHandler? Triggered;

    bool IsSupported { get; }

    string ShortcutDisplayName { get; }

    void Start();
}
