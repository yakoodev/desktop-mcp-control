using System.ComponentModel;
using DesktopMcp.Core.Abstractions;
using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Windows;

public sealed class Win32InputAutomationService : IInputAutomationService
{
    private static readonly Dictionary<string, ushort> KeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CTRL"] = 0x11,
        ["CONTROL"] = 0x11,
        ["ALT"] = 0x12,
        ["SHIFT"] = 0x10,
        ["WIN"] = 0x5B,
        ["WINDOWS"] = 0x5B,
        ["ENTER"] = 0x0D,
        ["TAB"] = 0x09,
        ["ESC"] = 0x1B,
        ["ESCAPE"] = 0x1B,
        ["SPACE"] = 0x20,
        ["BACKSPACE"] = 0x08,
        ["DELETE"] = 0x2E,
        ["HOME"] = 0x24,
        ["END"] = 0x23,
        ["PGUP"] = 0x21,
        ["PAGEUP"] = 0x21,
        ["PGDN"] = 0x22,
        ["PAGEDOWN"] = 0x22,
        ["UP"] = 0x26,
        ["DOWN"] = 0x28,
        ["LEFT"] = 0x25,
        ["RIGHT"] = 0x27
    };

    private readonly IDisplayService _displayService;

    public Win32InputAutomationService(IDisplayService displayService)
    {
        _displayService = displayService;
    }

    public void MoveMouse(ScreenPoint point)
    {
        var virtualBounds = _displayService.GetVirtualDesktopInfo().VirtualBounds;
        var (dx, dy) = VirtualDesktopMapper.ToAbsolute(virtualBounds, point);

        SendInputs(
        [
            CreateMouseInput(
                dx,
                dy,
                NativeMethods.MouseeventfMove | NativeMethods.MouseeventfAbsolute | NativeMethods.MouseeventfVirtualdesk)
        ]);
    }

    public void Click(ScreenPoint point, MouseButton button, int clickCount)
    {
        if (clickCount is < 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(clickCount), "clickCount must be 1 or 2.");
        }

        MoveMouse(point);

        for (var i = 0; i < clickCount; i++)
        {
            var (down, up) = GetButtonFlags(button);
            SendInputs(
            [
                CreateMouseButtonInput(down),
                CreateMouseButtonInput(up)
            ]);
        }
    }

    public void Scroll(ScreenPoint point, int deltaY, int deltaX)
    {
        MoveMouse(point);

        var inputs = new List<NativeMethods.Input>();
        if (deltaY != 0)
        {
            inputs.Add(
                new NativeMethods.Input
                {
                    Type = NativeMethods.InputMouse,
                    Data = new NativeMethods.InputUnion
                    {
                        Mi = new NativeMethods.MouseInput
                        {
                            MouseData = unchecked((uint)deltaY),
                            DwFlags = NativeMethods.MouseeventfWheel
                        }
                    }
                });
        }

        if (deltaX != 0)
        {
            inputs.Add(
                new NativeMethods.Input
                {
                    Type = NativeMethods.InputMouse,
                    Data = new NativeMethods.InputUnion
                    {
                        Mi = new NativeMethods.MouseInput
                        {
                            MouseData = unchecked((uint)deltaX),
                            DwFlags = NativeMethods.MouseeventfHWheel
                        }
                    }
                });
        }

        if (inputs.Count > 0)
        {
            SendInputs(inputs.ToArray());
        }
    }

    public void TypeText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var inputs = new List<NativeMethods.Input>(text.Length * 2);
        foreach (var ch in text)
        {
            inputs.Add(CreateUnicodeInput(ch, keyUp: false));
            inputs.Add(CreateUnicodeInput(ch, keyUp: true));
        }

        if (inputs.Count > 0)
        {
            SendInputs(inputs.ToArray());
        }
    }

    public void PressHotkey(IReadOnlyList<string> keys)
    {
        if (keys.Count == 0)
        {
            throw new ArgumentException("At least one key is required.", nameof(keys));
        }

        var parsed = keys.Select(ParseVirtualKey).ToArray();
        var inputs = new List<NativeMethods.Input>(parsed.Length * 2);

        inputs.AddRange(parsed.Select(k => CreateVirtualKeyInput(k, keyUp: false)));
        inputs.AddRange(parsed.Reverse().Select(k => CreateVirtualKeyInput(k, keyUp: true)));

        SendInputs(inputs.ToArray());
    }

    public async Task DragDropAsync(
        ScreenPoint from,
        ScreenPoint to,
        MouseButton button,
        int durationMs,
        CancellationToken cancellationToken)
    {
        durationMs = Math.Clamp(durationMs, 100, 5000);
        var virtualBounds = _displayService.GetVirtualDesktopInfo().VirtualBounds;
        var (fromDx, fromDy) = VirtualDesktopMapper.ToAbsolute(virtualBounds, from);
        var (toDx, toDy) = VirtualDesktopMapper.ToAbsolute(virtualBounds, to);

        var (down, up) = GetButtonFlags(button);
        // Move to start and press button once; hold state is maintained by OS until the matching UP event.
        SendInputs(
        [
            CreateMouseInput(
                fromDx,
                fromDy,
                NativeMethods.MouseeventfMove | NativeMethods.MouseeventfAbsolute | NativeMethods.MouseeventfVirtualdesk),
            CreateMouseInput(
                0,
                0,
                down)
        ]);
        await Task.Delay(20, cancellationToken).ConfigureAwait(false);

        var steps = Math.Max(4, durationMs / 16);
        for (var step = 1; step <= steps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var t = step / (double)steps;
            var x = (int)Math.Round(from.X + (to.X - from.X) * t);
            var y = (int)Math.Round(from.Y + (to.Y - from.Y) * t);
            var (dx, dy) = VirtualDesktopMapper.ToAbsolute(virtualBounds, new ScreenPoint(x, y));
            SendInputs(
            [
                CreateMouseInput(
                    dx,
                    dy,
                    NativeMethods.MouseeventfMove | NativeMethods.MouseeventfAbsolute | NativeMethods.MouseeventfVirtualdesk)
            ]);
            await Task.Delay(Math.Max(1, durationMs / steps), cancellationToken).ConfigureAwait(false);
        }

        SendInputs(
        [
            CreateMouseInput(
                toDx,
                toDy,
                NativeMethods.MouseeventfMove | NativeMethods.MouseeventfAbsolute | NativeMethods.MouseeventfVirtualdesk),
            CreateMouseInput(
                0,
                0,
                up)
        ]);
    }

    public ScreenPoint GetCursorPosition()
    {
        if (!NativeMethods.GetCursorPos(out var pt))
        {
            throw new Win32Exception();
        }

        return new ScreenPoint(pt.X, pt.Y);
    }

    private static NativeMethods.Input CreateMouseButtonInput(uint flags)
    {
        return new NativeMethods.Input
        {
            Type = NativeMethods.InputMouse,
            Data = new NativeMethods.InputUnion
            {
                Mi = new NativeMethods.MouseInput
                {
                    DwFlags = flags
                }
            }
        };
    }

    private static NativeMethods.Input CreateMouseInput(int dx, int dy, uint flags, uint mouseData = 0)
    {
        return new NativeMethods.Input
        {
            Type = NativeMethods.InputMouse,
            Data = new NativeMethods.InputUnion
            {
                Mi = new NativeMethods.MouseInput
                {
                    Dx = dx,
                    Dy = dy,
                    MouseData = mouseData,
                    DwFlags = flags
                }
            }
        };
    }

    private static NativeMethods.Input CreateUnicodeInput(char ch, bool keyUp)
    {
        return new NativeMethods.Input
        {
            Type = NativeMethods.InputKeyboard,
            Data = new NativeMethods.InputUnion
            {
                Ki = new NativeMethods.KeyboardInput
                {
                    WVk = 0,
                    WScan = ch,
                    DwFlags = NativeMethods.KeyeventfUnicode | (keyUp ? NativeMethods.KeyeventfKeyup : 0)
                }
            }
        };
    }

    private static NativeMethods.Input CreateVirtualKeyInput(ushort vk, bool keyUp)
    {
        return new NativeMethods.Input
        {
            Type = NativeMethods.InputKeyboard,
            Data = new NativeMethods.InputUnion
            {
                Ki = new NativeMethods.KeyboardInput
                {
                    WVk = vk,
                    WScan = 0,
                    DwFlags = keyUp ? NativeMethods.KeyeventfKeyup : 0
                }
            }
        };
    }

    private static (uint down, uint up) GetButtonFlags(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => (NativeMethods.MouseeventfLeftdown, NativeMethods.MouseeventfLeftup),
            MouseButton.Right => (NativeMethods.MouseeventfRightdown, NativeMethods.MouseeventfRightup),
            MouseButton.Middle => (NativeMethods.MouseeventfMiddledown, NativeMethods.MouseeventfMiddleup),
            _ => throw new ArgumentOutOfRangeException(nameof(button))
        };
    }

    private static ushort ParseVirtualKey(string rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            throw new ArgumentException("Key cannot be empty.");
        }

        var key = rawKey.Trim();
        if (KeyMap.TryGetValue(key, out var mapped))
        {
            return mapped;
        }

        if (key.Length == 1)
        {
            var ch = key[0];
            if (char.IsLetter(ch))
            {
                return (ushort)char.ToUpperInvariant(ch);
            }

            if (char.IsDigit(ch))
            {
                return (ushort)ch;
            }

            var vk = NativeMethods.VkKeyScanW(ch);
            if (vk != -1)
            {
                return (ushort)(vk & 0xFF);
            }
        }

        if (key.StartsWith("F", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(key[1..], out var fn) &&
            fn is >= 1 and <= 24)
        {
            return (ushort)(0x6F + fn);
        }

        throw new ArgumentException($"Unsupported key: {rawKey}");
    }

    private static void SendInputs(NativeMethods.Input[] inputs)
    {
        if (inputs.Length == 0)
        {
            return;
        }

        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.Input>());
        if (sent != inputs.Length)
        {
            throw new Win32Exception();
        }
    }
}
