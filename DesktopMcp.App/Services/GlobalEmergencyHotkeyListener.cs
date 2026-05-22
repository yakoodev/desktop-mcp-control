using System.Runtime.InteropServices;

namespace DesktopMcp.App.Services;

public sealed class GlobalEmergencyHotkeyListener : IGlobalEmergencyHotkey
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int VkControl = 0x11;
    private const int VkAlt = 0x12;
    private const int VkPause = 0x13;

    private readonly LowLevelKeyboardProc _proc;
    private nint _hook;
    private DateTimeOffset _lastTriggered = DateTimeOffset.MinValue;

    public event EventHandler? Triggered;

    public bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public string ShortcutDisplayName => "Ctrl+Alt+Pause";

    public GlobalEmergencyHotkeyListener()
    {
        _proc = HookCallback;
    }

    public void Start()
    {
        if (!IsSupported)
        {
            return;
        }

        if (_hook != nint.Zero)
        {
            return;
        }

        _hook = SetWindowsHookEx(WhKeyboardLl, _proc, nint.Zero, 0);
        if (_hook == nint.Zero)
        {
            throw new InvalidOperationException("Failed to install global hotkey hook.");
        }
    }

    public void Dispose()
    {
        if (_hook != nint.Zero)
        {
            _ = UnhookWindowsHookEx(_hook);
            _hook = nint.Zero;
        }
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0 &&
            (wParam == (nint)WmKeyDown || wParam == (nint)WmSysKeyDown))
        {
            var info = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            if (info.VkCode == VkPause &&
                IsKeyDown(VkControl) &&
                IsKeyDown(VkAlt))
            {
                // Avoid fast repeats while key is held down.
                var now = DateTimeOffset.UtcNow;
                if (now - _lastTriggered > TimeSpan.FromMilliseconds(400))
                {
                    _lastTriggered = now;
                    Triggered?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private static bool IsKeyDown(int key)
    {
        return (GetAsyncKeyState(key) & 0x8000) != 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public uint VkCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public nuint DwExtraInfo;
    }

    private delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc lpfn,
        nint hMod,
        uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
