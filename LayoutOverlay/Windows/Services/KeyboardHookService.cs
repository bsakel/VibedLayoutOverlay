using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LayoutOverlay.Windows.Services;

public class KeyboardHookService : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private LowLevelKeyboardProc _proc = null!;
    private IntPtr _hookID = IntPtr.Zero;

    public event EventHandler<KeyPressedEventArgs>? KeyPressed;
    public event EventHandler<KeyReleasedEventArgs>? KeyReleased;

    // Win32 API imports
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    public void StartHook()
    {
        _proc = HookCallback;
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule? curModule = curProcess.MainModule)
        {
            if (curModule?.ModuleName != null)
            {
                _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
    }

    public void StopHook()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            
            if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                KeyPressed?.Invoke(this, new KeyPressedEventArgs(vkCode));
            }
            else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
            {
                KeyReleased?.Invoke(this, new KeyReleasedEventArgs(vkCode));
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        StopHook();
        GC.SuppressFinalize(this);
    }
}

public class KeyPressedEventArgs : EventArgs
{
    public int VirtualKeyCode { get; }
    public Keys Key { get; }

    public KeyPressedEventArgs(int virtualKeyCode)
    {
        VirtualKeyCode = virtualKeyCode;
        Key = (Keys)virtualKeyCode;
    }
}

public class KeyReleasedEventArgs : EventArgs
{
    public int VirtualKeyCode { get; }
    public Keys Key { get; }

    public KeyReleasedEventArgs(int virtualKeyCode)
    {
        VirtualKeyCode = virtualKeyCode;
        Key = (Keys)virtualKeyCode;
    }
}