using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;

namespace Hotkeys
{
    public class GlobalHotkey
    {

        Window mainWindow;
        private HwndSource _source;
        Func<int, int> hotkeyHandler;

        public GlobalHotkey(Window mainWindow, Func<int, int> hotkeyHandler)
        {
            this.mainWindow = mainWindow;
            this.hotkeyHandler = hotkeyHandler;
        }

        public void Initialize(EventArgs e)
        {
            var helper = new WindowInteropHelper(mainWindow);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);
            RegisterHotKey();
        }

        public void destroyHooks()
        {
            _source.RemoveHook(HwndHook);
            _source = null;
            UnregisterHotKey();
        }

        private void RegisterHotKey()
        {
            var helper = new WindowInteropHelper(mainWindow);

            const uint VK_F7 = 0x76;
            const uint VK_F8 = 0x77;
            const uint MOD_CTRL = 0x0002;


            if (!RegisterHotKey(helper.Handle, Constants.HOTKEY_BRIGHTNESS_UP_ID, MOD_CTRL, VK_F8))
            {
                // handle error
            }

            if (!RegisterHotKey(helper.Handle, Constants.HOTKEY_BRIGHTNESS_DOWN_ID, MOD_CTRL, VK_F7))
            {
                // handle error
            }
        }

        private void UnregisterHotKey()
        {
            var helper = new WindowInteropHelper(mainWindow);
            UnregisterHotKey(helper.Handle, Constants.HOTKEY_BRIGHTNESS_UP_ID);
            UnregisterHotKey(helper.Handle, Constants.HOTKEY_BRIGHTNESS_DOWN_ID);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    if (wParam.ToInt32() >= Constants.HOTKEY_FIRST && wParam.ToInt32() <= Constants.HOTKEY_LAST)
                    {
                        OnHotKeyPressed(wParam.ToInt32());
                        handled = true;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private void OnHotKeyPressed(int hotKeyId)
        {
            hotkeyHandler(hotKeyId);
        }

        [DllImport("User32.dll")]
        private static extern bool RegisterHotKey(
            [In] IntPtr hWnd,
            [In] int id,
            [In] uint fsModifiers,
            [In] uint vk);

        [DllImport("User32.dll")]
        private static extern bool UnregisterHotKey(
            [In] IntPtr hWnd,
            [In] int id);
    }
}