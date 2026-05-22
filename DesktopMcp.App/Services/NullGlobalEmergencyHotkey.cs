namespace DesktopMcp.App.Services;

public sealed class NullGlobalEmergencyHotkey : IGlobalEmergencyHotkey
{
#pragma warning disable CS0067
    public event EventHandler? Triggered;
#pragma warning restore CS0067

    public bool IsSupported => false;

    public string ShortcutDisplayName => "Unavailable on this platform";

    public void Start()
    {
    }

    public void Dispose()
    {
    }
}
