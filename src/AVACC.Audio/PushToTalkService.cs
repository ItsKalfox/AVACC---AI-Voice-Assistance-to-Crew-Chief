using System.Diagnostics;
using System.Runtime.InteropServices;
using AVACC.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AVACC.Audio;

public class PushToTalkService : IPushToTalkService
{
    private readonly ILogger<PushToTalkService>? _logger;
    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _proc;
    private bool _isActive;
    private string _configuredKey = "RightCtrl";
    private bool _isGlobal = true;
    private bool _disableWhenInactive = false;
    private bool _isKeyPressed;
    private bool _isCapturingBinding;

    public bool IsActive => _isActive;
    public string ConfiguredKey => _configuredKey;
    public bool IsCapturingBinding => _isCapturingBinding;

    public event EventHandler? PttPressed;
    public event EventHandler? PttReleased;
    public event EventHandler<string>? KeyDetectedForBinding;

    public PushToTalkService(ILogger<PushToTalkService>? logger = null)
    {
        _logger = logger;
    }

    public void Initialize(string keyBinding, bool isGlobal, bool disableWhenInactive)
    {
        _configuredKey = string.IsNullOrWhiteSpace(keyBinding) ? "RightCtrl" : keyBinding;
        _isGlobal = isGlobal;
        _disableWhenInactive = disableWhenInactive;

        if (_isGlobal)
        {
            RegisterHook();
        }
    }

    public void UpdateConfiguration(string keyBinding, bool isGlobal, bool disableWhenInactive)
    {
        _configuredKey = string.IsNullOrWhiteSpace(keyBinding) ? "RightCtrl" : keyBinding;
        _isGlobal = isGlobal;
        _disableWhenInactive = disableWhenInactive;

        _logger?.LogInformation("Updating PTT configuration: Key={Key}, Global={Global}, DisableWhenInactive={Disable}", _configuredKey, _isGlobal, _disableWhenInactive);

        UnregisterHook();
        if (_isGlobal)
        {
            RegisterHook();
        }
    }

    public void StartCapturingBinding()
    {
        _isCapturingBinding = true;
        _logger?.LogInformation("Started capturing key for PTT binding...");
        RegisterHook(); // Ensure hook is active to intercept the key
    }

    public void StopCapturingBinding()
    {
        _isCapturingBinding = false;
        _logger?.LogInformation("Stopped capturing key for PTT binding.");
        if (!_isGlobal)
        {
            UnregisterHook();
        }
    }

    private void RegisterHook()
    {
        if (_hookId != IntPtr.Zero) return;

        try
        {
            _proc = HookCallback;
            var moduleHandle = GetModuleHandle(null);
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, moduleHandle, 0);
            _isActive = _hookId != IntPtr.Zero;
            if (!_isActive)
            {
                var err = Marshal.GetLastWin32Error();
                _logger?.LogWarning("SetWindowsHookEx failed with Win32 error: {Error}", err);
            }
            else
            {
                _logger?.LogInformation("Registered global PTT keyboard hook for key: {Key}", _configuredKey);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to register global keyboard hook");
            _isActive = false;
        }
    }

    private void UnregisterHook()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            _isActive = false;
            _proc = null;
            _logger?.LogInformation("Unregistered global PTT keyboard hook");
        }
    }

    public void TriggerPttDown()
    {
        if (!_isKeyPressed)
        {
            _isKeyPressed = true;
            _logger?.LogDebug("PTT Down triggered manually");
            PttPressed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void TriggerPttUp()
    {
        if (_isKeyPressed)
        {
            _isKeyPressed = false;
            _logger?.LogDebug("PTT Up triggered manually");
            PttReleased?.Invoke(this, EventArgs.Empty);
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var flags = Marshal.ReadInt32(lParam, 8);
            var msg = (int)wParam;

            // 1. Interactive Key Capture for Binding
            if (_isCapturingBinding)
            {
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    var detectedName = VkCodeToKeyName(vkCode, flags);
                    _isCapturingBinding = false;
                    _logger?.LogInformation("Detected key for binding: {Key} (VK: 0x{VkCode:X})", detectedName, vkCode);

                    // Raise on background / thread pool
                    Task.Run(() => KeyDetectedForBinding?.Invoke(this, detectedName));

                    return (IntPtr)1; // Consume key down event during binding capture
                }
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            // 2. Normal PTT Execution
            var isTargetKey = MatchesKey(vkCode, flags, _configuredKey);

            if (isTargetKey)
            {
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    if (!_isKeyPressed)
                    {
                        _isKeyPressed = true;
                        _logger?.LogInformation("PTT key down detected: {Key} (VK: 0x{VkCode:X})", _configuredKey, vkCode);
                        PttPressed?.Invoke(this, EventArgs.Empty);
                    }
                }
                else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                {
                    if (_isKeyPressed)
                    {
                        _isKeyPressed = false;
                        _logger?.LogInformation("PTT key up detected: {Key} (VK: 0x{VkCode:X})", _configuredKey, vkCode);
                        PttReleased?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public static bool MatchesKey(int vkCode, int flags, string configuredKey)
    {
        if (string.IsNullOrWhiteSpace(configuredKey)) return false;

        var isExtended = (flags & 1) != 0;
        var key = configuredKey.Trim().ToUpperInvariant();

        // Direct numeric VK codes (e.g. "0x21" or "33")
        if (key.StartsWith("0X") && int.TryParse(key.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out var hexVk))
        {
            return vkCode == hexVk;
        }
        if (int.TryParse(key, out var decVk))
        {
            return vkCode == decVk;
        }

        switch (key)
        {
            // Control
            case "RIGHTCTRL" or "RCONTROL" or "RCTRL":
                return vkCode == VK_RCONTROL || (vkCode == VK_CONTROL && isExtended);
            case "LEFTCTRL" or "LCONTROL" or "LCTRL":
                return vkCode == VK_LCONTROL || (vkCode == VK_CONTROL && !isExtended);
            case "CTRL" or "CONTROL":
                return vkCode == VK_CONTROL || vkCode == VK_LCONTROL || vkCode == VK_RCONTROL;

            // Alt
            case "RIGHTALT" or "RALT" or "ALTGR":
                return vkCode == 0xA5 || (vkCode == 0x12 && isExtended);
            case "LEFTALT" or "LALT":
                return vkCode == 0xA4 || (vkCode == 0x12 && !isExtended);
            case "ALT":
                return vkCode == 0x12 || vkCode == 0xA4 || vkCode == 0xA5;

            // Shift
            case "LEFTSHIFT" or "LSHIFT":
                return vkCode == 0xA0;
            case "RIGHTSHIFT" or "RSHIFT":
                return vkCode == 0xA1;
            case "SHIFT":
                return vkCode == 0x10 || vkCode == 0xA0 || vkCode == 0xA1;

            // Navigation & Page
            case "PAGEUP" or "PGUP" or "PRIOR":
                return vkCode == 0x21;
            case "PAGEDOWN" or "PGDN" or "NEXT":
                return vkCode == 0x22;
            case "END":
                return vkCode == 0x23;
            case "HOME":
                return vkCode == 0x24;
            case "LEFT" or "LEFTARROW":
                return vkCode == 0x25;
            case "UP" or "UPARROW":
                return vkCode == 0x26;
            case "RIGHT" or "RIGHTARROW":
                return vkCode == 0x27;
            case "DOWN" or "DOWNARROW":
                return vkCode == 0x28;
            case "INSERT" or "INS":
                return vkCode == 0x2D;
            case "DELETE" or "DEL":
                return vkCode == 0x2E;

            // Common keys
            case "SPACE" or "SPACEBAR":
                return vkCode == VK_SPACE;
            case "TAB":
                return vkCode == 0x09;
            case "RETURN" or "ENTER":
                return vkCode == 0x0D;
            case "BACKSPACE" or "BACK":
                return vkCode == 0x08;
            case "ESCAPE" or "ESC":
                return vkCode == 0x1B;
            case "CAPSLOCK" or "CAPS" or "CAPITAL":
                return vkCode == 0x14;
            case "SCROLLLOCK" or "SCROLL":
                return vkCode == 0x91;
            case "PAUSE" or "BREAK":
                return vkCode == 0x13;
            case "PRINTSCREEN" or "SNAPSHOT":
                return vkCode == 0x2C;
            case "NUMLOCK":
                return vkCode == 0x90;

            // Function keys F1 - F24
            case "F1": return vkCode == 0x70;
            case "F2": return vkCode == 0x71;
            case "F3": return vkCode == 0x72;
            case "F4": return vkCode == 0x73;
            case "F5": return vkCode == 0x74;
            case "F6": return vkCode == 0x75;
            case "F7": return vkCode == 0x76;
            case "F8": return vkCode == 0x77;
            case "F9": return vkCode == 0x78;
            case "F10": return vkCode == 0x79;
            case "F11": return vkCode == 0x7A;
            case "F12": return vkCode == 0x7B;
            case "F13": return vkCode == 0x7C;
            case "F14": return vkCode == 0x7D;
            case "F15": return vkCode == 0x7E;
            case "F16": return vkCode == 0x7F;

            // NumPad keys
            case "NUMPAD0" or "NUM0": return vkCode == 0x60;
            case "NUMPAD1" or "NUM1": return vkCode == 0x61;
            case "NUMPAD2" or "NUM2": return vkCode == 0x62;
            case "NUMPAD3" or "NUM3": return vkCode == 0x63;
            case "NUMPAD4" or "NUM4": return vkCode == 0x64;
            case "NUMPAD5" or "NUM5": return vkCode == 0x65;
            case "NUMPAD6" or "NUM6": return vkCode == 0x66;
            case "NUMPAD7" or "NUM7": return vkCode == 0x67;
            case "NUMPAD8" or "NUM8": return vkCode == 0x68;
            case "NUMPAD9" or "NUM9": return vkCode == 0x69;
            case "MULTIPLY": return vkCode == 0x6A;
            case "ADD": return vkCode == 0x6B;
            case "SUBTRACT": return vkCode == 0x6D;
            case "DECIMAL": return vkCode == 0x6E;
            case "DIVIDE": return vkCode == 0x6F;

            // Single letters A-Z and digits 0-9
            default:
                if (key.Length == 1)
                {
                    char c = key[0];
                    if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                    {
                        return vkCode == (int)c;
                    }
                }
                return false;
        }
    }

    public static string VkCodeToKeyName(int vkCode, int flags)
    {
        var isExtended = (flags & 1) != 0;

        // Modifiers with extended differentiation
        if (vkCode == 0x11 || vkCode == 0xA2 || vkCode == 0xA3)
        {
            return (vkCode == 0xA3 || isExtended) ? "RightCtrl" : "LeftCtrl";
        }
        if (vkCode == 0x12 || vkCode == 0xA4 || vkCode == 0xA5)
        {
            return (vkCode == 0xA5 || isExtended) ? "RightAlt" : "LeftAlt";
        }
        if (vkCode == 0x10 || vkCode == 0xA0 || vkCode == 0xA1)
        {
            return (vkCode == 0xA1) ? "RightShift" : "LeftShift";
        }

        return vkCode switch
        {
            0x20 => "Space",
            0x09 => "Tab",
            0x0D => "Enter",
            0x08 => "Backspace",
            0x1B => "Escape",
            0x14 => "CapsLock",
            0x13 => "Pause",
            0x91 => "ScrollLock",
            0x90 => "NumLock",
            0x2C => "PrintScreen",
            0x21 => "PageUp",
            0x22 => "PageDown",
            0x23 => "End",
            0x24 => "Home",
            0x25 => "Left",
            0x26 => "Up",
            0x27 => "Right",
            0x28 => "Down",
            0x2D => "Insert",
            0x2E => "Delete",
            >= 0x30 and <= 0x39 => ((char)vkCode).ToString(),
            >= 0x41 and <= 0x5A => ((char)vkCode).ToString(),
            >= 0x60 and <= 0x69 => $"NumPad{vkCode - 0x60}",
            0x6A => "Multiply",
            0x6B => "Add",
            0x6D => "Subtract",
            0x6E => "Decimal",
            0x6F => "Divide",
            >= 0x70 and <= 0x87 => $"F{vkCode - 0x70 + 1}",
            0xBA => "Semicolon",
            0xBB => "Equals",
            0xBC => "Comma",
            0xBD => "Minus",
            0xBE => "Period",
            0xBF => "Slash",
            0xC0 => "Tilde",
            0xDB => "LeftBracket",
            0xDC => "Backslash",
            0xDD => "RightBracket",
            0xDE => "Quote",
            _ => $"0x{vkCode:X2}"
        };
    }

    public void Dispose()
    {
        UnregisterHook();
        GC.SuppressFinalize(this);
    }

    #region Win32 Native
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private const int VK_CONTROL = 0x11;
    private const int VK_LCONTROL = 0xA2;
    private const int VK_RCONTROL = 0xA3;
    private const int VK_SPACE = 0x20;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
    #endregion
}
