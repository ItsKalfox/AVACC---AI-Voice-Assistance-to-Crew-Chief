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

    public bool IsActive => _isActive;
    public string ConfiguredKey => _configuredKey;

    public event EventHandler? PttPressed;
    public event EventHandler? PttReleased;

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

        UnregisterHook();
        if (_isGlobal)
        {
            RegisterHook();
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
            var isTargetKey = IsTargetKey(vkCode, flags);

            if (isTargetKey)
            {
                var msg = (int)wParam;
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    if (!_isKeyPressed)
                    {
                        _isKeyPressed = true;
                        _logger?.LogInformation("PTT key down detected (VK: 0x{VkCode:X}, Flags: 0x{Flags:X})", vkCode, flags);
                        PttPressed?.Invoke(this, EventArgs.Empty);
                    }
                }
                else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                {
                    if (_isKeyPressed)
                    {
                        _isKeyPressed = false;
                        _logger?.LogInformation("PTT key up detected (VK: 0x{VkCode:X}, Flags: 0x{Flags:X})", vkCode, flags);
                        PttReleased?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private bool IsTargetKey(int vkCode, int flags)
    {
        var isExtended = (flags & 1) != 0;
        var key = _configuredKey.Trim().ToUpperInvariant();

        switch (key)
        {
            case "RIGHTCTRL" or "RCONTROL" or "RCTRL":
                return vkCode == VK_RCONTROL || (vkCode == VK_CONTROL && isExtended);

            case "LEFTCTRL" or "LCONTROL" or "LCTRL":
                return vkCode == VK_LCONTROL || (vkCode == VK_CONTROL && !isExtended);

            case "CTRL" or "CONTROL":
                return vkCode == VK_CONTROL || vkCode == VK_LCONTROL || vkCode == VK_RCONTROL;

            case "RIGHTALT" or "RALT" or "ALTGR":
                return vkCode == 0xA5 || (vkCode == 0x12 && isExtended);

            case "LEFTALT" or "LALT":
                return vkCode == 0xA4 || (vkCode == 0x12 && !isExtended);

            case "ALT":
                return vkCode == 0x12 || vkCode == 0xA4 || vkCode == 0xA5;

            case "SPACE":
                return vkCode == VK_SPACE;

            case "CAPSLOCK" or "CAPS":
                return vkCode == 0x14;

            case "TAB":
                return vkCode == 0x09;

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

            default:
                if (key.Length == 1 && ((key[0] >= 'A' && key[0] <= 'Z') || (key[0] >= '0' && key[0] <= '9')))
                {
                    return vkCode == (int)key[0];
                }
                return vkCode == VK_RCONTROL || (vkCode == VK_CONTROL && isExtended);
        }
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

