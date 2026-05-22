namespace DesktopMcp.Core.Models;

public sealed record DesktopCapabilities(
    bool Mouse,
    bool Keyboard,
    bool Capture,
    bool WindowList,
    bool Tray,
    bool GlobalHotkey)
{
    public bool Has(DesktopCapability capability)
    {
        return capability switch
        {
            DesktopCapability.Mouse => Mouse,
            DesktopCapability.Keyboard => Keyboard,
            DesktopCapability.Capture => Capture,
            DesktopCapability.WindowList => WindowList,
            DesktopCapability.Tray => Tray,
            DesktopCapability.GlobalHotkey => GlobalHotkey,
            _ => false
        };
    }
}
