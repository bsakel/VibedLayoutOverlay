using System.Runtime.InteropServices;
using LayoutOverlay.Windows.Models;

namespace LayoutOverlay.Windows.Services;

public class GlobalHotkeyService
{
    private readonly LayoutService _layoutService;
    private readonly Dictionary<int, LayoutType> _hotkeyLayerMap = new();
    private IntPtr _windowHandle = IntPtr.Zero;
    private const int WM_HOTKEY = 0x0312;

    // Win32 API imports
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // Virtual key codes for F13-F24
    private const uint VK_F13 = 0x7C;
    private const uint VK_F24 = 0x87;

    // Keys A-Z
    private const uint VK_Q = 0x51;
    private const uint VK_X = 0x58;

    public GlobalHotkeyService(LayoutService layoutService)
    {
        _layoutService = layoutService;
        InitializeHotkeyMappings();
    }

    private void InitializeHotkeyMappings()
    {
        _hotkeyLayerMap[1] = LayoutType.Qwerty;   // F13+Q
        _hotkeyLayerMap[2] = LayoutType.Custom;   // F13+X
    }

    public void Initialize(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        RegisterAllHotkeys();
    }

    private void RegisterAllHotkeys()
    {
        try
        {
            // F24 for overlay toggle (hotkey ID 0)
            RegisterHotKey(_windowHandle, 0, 0, VK_F24);

            // F13+ combinations for layer switching
            RegisterHotKey(_windowHandle, 1, 0, VK_F13 | (VK_Q << 8));  // F13+Q (QWERTY)
            RegisterHotKey(_windowHandle, 2, 0, VK_F13 | (VK_X << 8));  // F13+X (Custom)

            Console.WriteLine("Global hotkeys registered successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to register hotkeys: {ex.Message}");
        }
    }

    public bool ProcessHotkey(int hotkeyId)
    {
        if (hotkeyId == 0)
        {
            // F24 - Toggle overlay
            _layoutService.ToggleOverlay();
            return true;
        }

        if (_hotkeyLayerMap.ContainsKey(hotkeyId))
        {
            // F13+ combinations - Switch layer
            _layoutService.SetActiveLayout(_hotkeyLayerMap[hotkeyId]);
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            for (int i = 0; i <= 2; i++)
            {
                UnregisterHotKey(_windowHandle, i);
            }
        }
    }
}