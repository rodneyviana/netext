using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace NetExt.Shim
{
    public class WinApi
    {
        public const int VK_RETURN = 0x0D;

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, Int32 wParam, Int32 lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, GetWindowType uCmd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetWindowText(IntPtr hwnd, String lpString);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, IntPtr windowTitle);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll",
         CallingConvention = CallingConvention.Winapi,
         SetLastError = true)]

        public static extern bool FileTimeToSystemTime([In] ref FILETIME lpFileTime,
            out SYSTEMTIME lpSystemTime);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, ref IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, ref IntPtr lParam);

        private enum GetWindowType : uint
        {
            /// <summary>
            /// The retrieved handle identifies the window of the same type that is highest in the Z order.
            /// <para/>
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDFIRST = 0,
            /// <summary>
            /// The retrieved handle identifies the window of the same type that is lowest in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDLAST = 1,
            /// <summary>
            /// The retrieved handle identifies the window below the specified window in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDNEXT = 2,
            /// <summary>
            /// The retrieved handle identifies the window above the specified window in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            GW_HWNDPREV = 3,
            /// <summary>
            /// The retrieved handle identifies the specified window's owner window, if any.
            /// </summary>
            GW_OWNER = 4,
            /// <summary>
            /// The retrieved handle identifies the child window at the top of the Z order,
            /// if the specified window is a parent window; otherwise, the retrieved handle is NULL.
            /// The function examines only child windows of the specified window. It does not examine descendant windows.
            /// </summary>
            GW_CHILD = 5,
            /// <summary>
            /// The retrieved handle identifies the enabled popup window owned by the specified window (the
            /// search uses the first such window found using GW_HWNDNEXT); otherwise, if there are no enabled
            /// popup windows, the retrieved handle is that of the specified window.
            /// </summary>
            GW_ENABLEDPOPUP = 6
        }

        public struct WM
        {
            public const uint WM_KEYUP = 0x0101;
            public const uint WM_KEYDOWN = 0x0100;
        }

        public struct WinMessages
        {
            public const int SB_SETTEXT = 1035;
            public const int SB_SETPARTS = 1028;
            public const int SB_GETPARTS = 1030;

        }

        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {

            /// WORD->unsigned short
            public ushort wYear;

            /// WORD->unsigned short
            public ushort wMonth;

            /// WORD->unsigned short
            public ushort wDayOfWeek;

            /// WORD->unsigned short
            public ushort wDay;

            /// WORD->unsigned short
            public ushort wHour;

            /// WORD->unsigned short
            public ushort wMinute;

            /// WORD->unsigned short
            public ushort wSecond;

            /// WORD->unsigned short
            public ushort wMilliseconds;
        }


        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct FILETIME
        {

            /// DWORD->unsigned int
            public uint dwLowDateTime;

            /// DWORD->unsigned int
            public int dwHighDateTime;

            public static bool operator ==(FILETIME t1, FILETIME t2)
            {
                return (t1.dwHighDateTime == t2.dwHighDateTime && t1.dwLowDateTime == t2.dwLowDateTime);
            }
            public static bool operator !=(FILETIME t1, FILETIME t2)
            {
                return (!(t1 == t2));
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return ((long)(dwHighDateTime << 32) + (long)dwLowDateTime).GetHashCode();
            }

        }

        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct XSTATE_FEATURE
        {

            /// ULONG->unsigned int
            public uint Offset;

            /// ULONG->unsigned int
            public uint Size;
        }

        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct XSTATE_CONFIGURATION
        {

            /// ULONG64->unsigned __int64
            public ulong EnabledFeatures;

            /// ULONG64->unsigned __int64
            public ulong EnabledVolatileFeatures;

            /// ULONG->unsigned int
            public uint Size;

            /// OptimizedSave : 1
            public uint bitvector1;

            /// XSTATE_FEATURE[]
            public XSTATE_FEATURE[] Features;

            public uint OptimizedSave
            {
                get
                {
                    return ((uint)((this.bitvector1 & 1u)));
                }
                set
                {
                    this.bitvector1 = ((uint)((value | this.bitvector1)));
                }
            }
        }

        public enum ALTERNATIVE_ARCHITECTURE_TYPE
        {

            StandardDesign,

            NEC98x86,

            EndAlternatives,
        }

        public enum NT_PRODUCT_TYPE
        {

            /// NtProductWinNt -> 1
            NtProductWinNt = 1,

            NtProductLanManNt,

            NtProductServer,
        }

        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct KSYSTEM_TIME
        {

            /// ULONG->unsigned int
            public uint LowPart;

            /// LONG->int
            public int High1Time;

            /// LONG->int
            public int High2Time;

            public long ToLong()
            {

                ulong u = ((ulong)High1Time << 32) | LowPart;
                return BitConverter.ToInt64(BitConverter.GetBytes(u), 0);
            }

        }



        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public struct KUSER_SHARED_DATA
        {

            /// ULONG->unsigned int
            public uint TickCountLowDeprecated;

            /// ULONG->unsigned int
            public uint TickCountMultiplier;

            /// KSYSTEM_TIME->_KSYSTEM_TIME
            public KSYSTEM_TIME InterruptTime;

            /// KSYSTEM_TIME->_KSYSTEM_TIME
            public KSYSTEM_TIME SystemTime;

            /// KSYSTEM_TIME->_KSYSTEM_TIME
            public KSYSTEM_TIME TimeZoneBias;

            /// USHORT->unsigned short
            public ushort ImageNumberLow;

            /// USHORT->unsigned short
            public ushort ImageNumberHigh;

            /// WCHAR[260]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 260)]
            public string NtSystemRoot;

            /// ULONG->unsigned int
            public uint MaxStackTraceDepth;

            /// ULONG->unsigned int
            public uint CryptoExponent;

            /// ULONG->unsigned int
            public uint TimeZoneId;

            /// ULONG->unsigned int
            public uint LargePageMinimum;

            /// ULONG->unsigned int
            public uint AitSamplingValue;

            /// ULONG->unsigned int
            public uint AppCompatFlag;

            /// ULONGLONG->unsigned __int64
            public ulong RNGSeedVersion;

            /// ULONG->unsigned int
            public uint GlobalValidationRunlevel;

            /// LONG->int
            public int TimeZoneBiasStamp;

            /// ULONG->unsigned int
            public uint Reserved2;

            /// NT_PRODUCT_TYPE->_NT_PRODUCT_TYPE
            public NT_PRODUCT_TYPE NtProductType;

            /// BOOLEAN->BYTE->unsigned char
            public byte ProductTypeIsValid;

            /// BOOLEAN[1]
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = System.Runtime.InteropServices.UnmanagedType.I1)]
            public byte[] Reserved0;

            /// USHORT->unsigned short
            public ushort NativeProcessorArchitecture;

            /// ULONG->unsigned int
            public uint NtMajorVersion;

            /// ULONG->unsigned int
            public uint NtMinorVersion;

        }

        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Explicit)]
        public struct LARGE_INTEGER
        {

            /// Anonymous_9320654f_2227_43bf_a385_74cc8c562686
            [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
            public Anonymous_9320654f_2227_43bf_a385_74cc8c562686 Struct1;

            /// Anonymous_947eb392_1446_4e25_bbd4_10e98165f3a9
            [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
            public Anonymous_947eb392_1446_4e25_bbd4_10e98165f3a9 u;

            /// LONGLONG->__int64
            [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
            public long QuadPart;
        }

        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct Anonymous_9320654f_2227_43bf_a385_74cc8c562686
        {

            /// DWORD->unsigned int
            public uint LowPart;

            /// LONG->int
            public int HighPart;
        }

        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct Anonymous_947eb392_1446_4e25_bbd4_10e98165f3a9
        {

            /// DWORD->unsigned int
            public uint LowPart;

            /// LONG->int
            public int HighPart;
        }

        public static IntPtr GetMainWindow()
        {
            return Process.GetCurrentProcess().MainWindowHandle;
        }

        public static IntPtr GetStatusBar()
        {
            IntPtr mainWin = GetMainWindow();
            if (mainWin == IntPtr.Zero)
            {
                return mainWin;
            }

            return FindWindowEx(mainWin, IntPtr.Zero, "msctls_statusbar32", IntPtr.Zero);
        }

        protected static IntPtr statusWin = IntPtr.Zero;

        public static void SetStatusBarMessage(string Message)
        {
            if (statusWin == IntPtr.Zero)
            {
                statusWin = GetStatusBar();
            }

            if (statusWin != IntPtr.Zero)
            {
                IntPtr glbText = Marshal.StringToHGlobalAuto(Message);
                SendMessage(statusWin, WinMessages.SB_SETTEXT, 0, glbText);
                Marshal.FreeHGlobal(glbText);
            }
        }

        public static string GetTitle(IntPtr hwnd)
        {
            int len = GetWindowTextLength(hwnd);
            StringBuilder sb = new StringBuilder(len + 1);
            GetWindowText(hwnd, sb, sb.Capacity);
            return sb.ToString();

        }
        protected static bool FindCmdWin_CallBack(IntPtr hwnd, ref IntPtr lparam)
        {

            string title = GetTitle(hwnd);
            if (title.StartsWith("Command - Dump"))
            {
                lparam = hwnd;
            }
            else if (title == "Command")
            {
                if (lparam == IntPtr.Zero)
                {
                    lparam = hwnd;

                }
            }


            return true;
        }

        public static IntPtr FindCmdWin()
        {
            IntPtr cmd = IntPtr.Zero;
            EnumChildWindows(GetMainWindow(), FindCmdWin_CallBack, ref cmd);
            return cmd;
        }

        public static void SendCommand(string Command)
        {
            IntPtr cmd = FindCmdWin();
            if (cmd != IntPtr.Zero)
            {
                var label = FindWindowEx(cmd, IntPtr.Zero, "Static", IntPtr.Zero);
                var input = GetWindow(label, GetWindowType.GW_HWNDNEXT);
                SetWindowText(input, Command);
                PostMessage(input, WM.WM_KEYDOWN, VK_RETURN, 1);
                PostMessage(input, WM.WM_KEYUP, VK_RETURN, 1);
                Application.DoEvents(); // Refresh
            }
        }

    }
}
