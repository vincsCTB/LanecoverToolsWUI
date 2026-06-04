using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LanecoverToolsWUI.Services
{
    class KeyboardHook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static nint _hookID = nint.Zero;
        private static MainWindow _mainWindow;
        public static bool IsF2Pressed;

        public static UserSettings Settings => UserSettingsService.Current;

        public static nint SetHook(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public delegate nint LowLevelKeyboardProc(
            int nCode, nint wParam, nint lParam);

        private static nint HookCallback(
            int nCode, nint wParam, nint lParam)
        {
            if (nCode >= 0 && wParam == WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (vkCode == Settings.GenerationHotkey[2])
                {
                    bool shiftPressed = (GetAsyncKeyState(Settings.GenerationHotkey[1]) & 0x8000) != 0;
                    bool ctrlPressed = (GetAsyncKeyState(Settings.GenerationHotkey[0]) & 0x8000) != 0;

                    if (shiftPressed && ctrlPressed)
                    {
                        _mainWindow.HandleGenerationMacroPress();
                    }
                }

                if (vkCode == Settings.RevertHotkey[2])
                {
                    bool shiftPressed = (GetAsyncKeyState(Settings.RevertHotkey[1]) & 0x8000) != 0;
                    bool ctrlPressed = (GetAsyncKeyState(Settings.RevertHotkey[0]) & 0x8000) != 0;

                    if (shiftPressed && ctrlPressed)
                    {
                        _mainWindow.HandleRevertMacroPress();
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern nint SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(nint hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern nint CallNextHookEx(nint hhk, int nCode,
            nint wParam, nint lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern nint GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
    }
}
