using System.Runtime.InteropServices;

namespace DesktopMcp.App.Services;

public sealed class LinuxX11GlobalEmergencyHotkey : IGlobalEmergencyHotkey
{
    private const int KeyPress = 2;
    private const int GrabModeAsync = 1;
    private const int ControlMask = 1 << 2;
    private const int Mod1Mask = 1 << 3;
    private const int LockMask = 1 << 1;
    private const int Mod2Mask = 1 << 4;
    private const uint XkPause = 0xFF13;

    private static readonly int[] ModifierMasks =
    [
        ControlMask | Mod1Mask,
        ControlMask | Mod1Mask | LockMask,
        ControlMask | Mod1Mask | Mod2Mask,
        ControlMask | Mod1Mask | LockMask | Mod2Mask
    ];

    private IntPtr _display;
    private IntPtr _rootWindow;
    private byte _keycode;
    private Thread? _eventThread;
    private volatile bool _isRunning;

    public event EventHandler? Triggered;

    public bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY"));

    public string ShortcutDisplayName => "Ctrl+Alt+Pause";

    public void Start()
    {
        if (!IsSupported || _isRunning)
        {
            return;
        }

        _display = XOpenDisplay(IntPtr.Zero);
        if (_display == IntPtr.Zero)
        {
            return;
        }

        var screen = XDefaultScreen(_display);
        _rootWindow = XRootWindow(_display, screen);
        _keycode = XKeysymToKeycode(_display, XkPause);
        if (_keycode == 0)
        {
            Dispose();
            return;
        }

        foreach (var modifiers in ModifierMasks)
        {
            XGrabKey(_display, _keycode, modifiers, _rootWindow, true, GrabModeAsync, GrabModeAsync);
        }

        XSync(_display, false);

        _isRunning = true;
        _eventThread = new Thread(EventLoop)
        {
            IsBackground = true,
            Name = "DesktopMcp.LinuxHotkey"
        };
        _eventThread.Start();
    }

    public void Dispose()
    {
        _isRunning = false;
        if (_eventThread is { IsAlive: true })
        {
            _eventThread.Join(TimeSpan.FromMilliseconds(400));
        }

        if (_display != IntPtr.Zero)
        {
            foreach (var modifiers in ModifierMasks)
            {
                XUngrabKey(_display, _keycode, modifiers, _rootWindow);
            }

            XCloseDisplay(_display);
            _display = IntPtr.Zero;
        }
    }

    private void EventLoop()
    {
        while (_isRunning && _display != IntPtr.Zero)
        {
            if (XPending(_display) == 0)
            {
                Thread.Sleep(35);
                continue;
            }

            XNextEvent(_display, out var xevent);
            if (xevent.Type == KeyPress)
            {
                Triggered?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 192)]
    private struct XEvent
    {
        [FieldOffset(0)]
        public int Type;
    }

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr displayName);

    [DllImport("libX11.so.6")]
    private static extern int XDefaultScreen(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XRootWindow(IntPtr display, int screenNumber);

    [DllImport("libX11.so.6")]
    private static extern byte XKeysymToKeycode(IntPtr display, uint keysym);

    [DllImport("libX11.so.6")]
    private static extern int XGrabKey(
        IntPtr display,
        int keycode,
        int modifiers,
        IntPtr grabWindow,
        bool ownerEvents,
        int pointerMode,
        int keyboardMode);

    [DllImport("libX11.so.6")]
    private static extern int XUngrabKey(IntPtr display, int keycode, int modifiers, IntPtr grabWindow);

    [DllImport("libX11.so.6")]
    private static extern int XPending(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XNextEvent(IntPtr display, out XEvent xevent);

    [DllImport("libX11.so.6")]
    private static extern int XSync(IntPtr display, bool discard);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);
}
