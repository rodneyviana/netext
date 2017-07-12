using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace NetExt.Shim
{

    public delegate bool BreakPointCallBack(IDebugBreakpoint Bp, string BpExpression);

    [StructLayout(LayoutKind.Sequential)]
    public struct MemLocation
    {
        public IntPtr VAAddr;
        public IntPtr VASize;
        public IntPtr FileAddr;
        public IntPtr FileSize;
    };

    public struct IMAGE_DEBUG_DIRECTORY
    {
        public int Characteristics;
        public int TimeDateStamp;
        public short MajorVersion;
        public short MinorVersion;
        public IMAGE_DEBUG_TYPE Type;
        public int SizeOfData;
        public int AddressOfRawData;
        public int PointerToRawData;
    };

    public enum IMAGE_DEBUG_TYPE
    {
        UNKNOWN = 0,
        COFF = 1,
        CODEVIEW = 2,
        FPO = 3,
        MISC = 4,
        BBT = 10,
    };

    [Flags]
    public enum FileShare : uint
    {
        FILE_SHARE_NONE = 0,
        FILE_SHARE_READ = 0x00000001,
        FILE_SHARE_WRITE = 0x00000002,
        FILE_SHARE_DELETE = 0x00000004,

    }
    [Flags]
    public enum FileAccess : uint
    {
        NONE = 0,
        FILE_READ_DATA = 0x0001,    // file & pipe
        FILE_LIST_DIRECTORY = 0x0001,    // directory

        FILE_WRITE_DATA = 0x0002,    // file & pipe
        FILE_ADD_FILE = 0x0002,    // directory

        FILE_APPEND_DATA = 0x0004,    // file
        FILE_ADD_SUBDIRECTORY = 0x0004,    // directory
        FILE_CREATE_PIPE_INSTANCE = 0x0004,    // named pipe


        FILE_READ_EA = 0x0008,    // file & directory

        FILE_WRITE_EA = 0x0010,    // file & directory

        FILE_EXECUTE = 0x0020,    // file
        FILE_TRAVERSE = 0x0020,    // directory

        FILE_DELETE_CHILD = 0x0040,    // directory

        FILE_READ_ATTRIBUTES = 0x0080,    // all

        FILE_WRITE_ATTRIBUTES = 0x0100,    // all


        GENERIC_READ = 0x80000000,
        GENERIC_WRITE = 0x40000000,
        GENERIC_EXECUTE = 0x20000000,
        GENERIC_ALL = 0x10000000
    }

    public enum HRESULT : uint
    {
        S_OK = 0,
        E_ABORT = 4,
        E_ACCESSDENIED = 0x80070005,
        E_FAIL = 0x80004005,
        E_HANDLE = 0x80070006,
        E_INVALIDARG = 0x80070057,
        E_NOINTERFACE = 0x80004002,
        E_NOTIMPL = 0x80004001,
        E_OUTOFMEMORY = 0x8007000E,
        E_POINTER = 0x80004003,
        E_UNEXPECTED = 0x8000FFFF

    }

    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_SECTION_HEADER
    {
        [FieldOffset(0)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public char[] Name;

        [FieldOffset(8)]
        public UInt32 VirtualSize;

        [FieldOffset(12)]
        public UInt32 VirtualAddress;

        [FieldOffset(16)]
        public UInt32 SizeOfRawData;

        [FieldOffset(20)]
        public UInt32 PointerToRawData;

        [FieldOffset(24)]
        public UInt32 PointerToRelocations;

        [FieldOffset(28)]
        public UInt32 PointerToLinenumbers;

        [FieldOffset(32)]
        public UInt16 NumberOfRelocations;

        [FieldOffset(34)]
        public UInt16 NumberOfLinenumbers;

        [FieldOffset(36)]
        public DataSectionFlags Characteristics;

        public string Section
        {
            get { return new string(Name); }
        }
    }
    [Flags]
    public enum DataSectionFlags : uint
    {
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeReg = 0x00000000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeDsect = 0x00000001,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeNoLoad = 0x00000002,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeGroup = 0x00000004,
        /// <summary>
        /// The section should not be padded to the next boundary. This flag is obsolete and is replaced by IMAGE_SCN_ALIGN_1BYTES. This is valid only for object files.
        /// </summary>
        TypeNoPadded = 0x00000008,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeCopy = 0x00000010,
        /// <summary>
        /// The section contains executable code.
        /// </summary>
        ContentCode = 0x00000020,
        /// <summary>
        /// The section contains initialized data.
        /// </summary>
        ContentInitializedData = 0x00000040,
        /// <summary>
        /// The section contains uninitialized data.
        /// </summary>
        ContentUninitializedData = 0x00000080,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        LinkOther = 0x00000100,
        /// <summary>
        /// The section contains comments or other information. The .drectve section has this type. This is valid for object files only.
        /// </summary>
        LinkInfo = 0x00000200,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        TypeOver = 0x00000400,
        /// <summary>
        /// The section will not become part of the image. This is valid only for object files.
        /// </summary>
        LinkRemove = 0x00000800,
        /// <summary>
        /// The section contains COMDAT data. For more information, see section 5.5.6, COMDAT Sections (Object Only). This is valid only for object files.
        /// </summary>
        LinkComDat = 0x00001000,
        /// <summary>
        /// Reset speculative exceptions handling bits in the TLB entries for this section.
        /// </summary>
        NoDeferSpecExceptions = 0x00004000,
        /// <summary>
        /// The section contains data referenced through the global pointer (GP).
        /// </summary>
        RelativeGP = 0x00008000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        MemPurgeable = 0x00020000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        Memory16Bit = 0x00020000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        MemoryLocked = 0x00040000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        MemoryPreload = 0x00080000,
        /// <summary>
        /// Align data on a 1-byte boundary. Valid only for object files.
        /// </summary>
        Align1Bytes = 0x00100000,
        /// <summary>
        /// Align data on a 2-byte boundary. Valid only for object files.
        /// </summary>
        Align2Bytes = 0x00200000,
        /// <summary>
        /// Align data on a 4-byte boundary. Valid only for object files.
        /// </summary>
        Align4Bytes = 0x00300000,
        /// <summary>
        /// Align data on an 8-byte boundary. Valid only for object files.
        /// </summary>
        Align8Bytes = 0x00400000,
        /// <summary>
        /// Align data on a 16-byte boundary. Valid only for object files.
        /// </summary>
        Align16Bytes = 0x00500000,
        /// <summary>
        /// Align data on a 32-byte boundary. Valid only for object files.
        /// </summary>
        Align32Bytes = 0x00600000,
        /// <summary>
        /// Align data on a 64-byte boundary. Valid only for object files.
        /// </summary>
        Align64Bytes = 0x00700000,
        /// <summary>
        /// Align data on a 128-byte boundary. Valid only for object files.
        /// </summary>
        Align128Bytes = 0x00800000,
        /// <summary>
        /// Align data on a 256-byte boundary. Valid only for object files.
        /// </summary>
        Align256Bytes = 0x00900000,
        /// <summary>
        /// Align data on a 512-byte boundary. Valid only for object files.
        /// </summary>
        Align512Bytes = 0x00A00000,
        /// <summary>
        /// Align data on a 1024-byte boundary. Valid only for object files.
        /// </summary>
        Align1024Bytes = 0x00B00000,
        /// <summary>
        /// Align data on a 2048-byte boundary. Valid only for object files.
        /// </summary>
        Align2048Bytes = 0x00C00000,
        /// <summary>
        /// Align data on a 4096-byte boundary. Valid only for object files.
        /// </summary>
        Align4096Bytes = 0x00D00000,
        /// <summary>
        /// Align data on an 8192-byte boundary. Valid only for object files.
        /// </summary>
        Align8192Bytes = 0x00E00000,
        /// <summary>
        /// The section contains extended relocations.
        /// </summary>
        LinkExtendedRelocationOverflow = 0x01000000,
        /// <summary>
        /// The section can be discarded as needed.
        /// </summary>
        MemoryDiscardable = 0x02000000,
        /// <summary>
        /// The section cannot be cached.
        /// </summary>
        MemoryNotCached = 0x04000000,
        /// <summary>
        /// The section is not pageable.
        /// </summary>
        MemoryNotPaged = 0x08000000,
        /// <summary>
        /// The section can be shared in memory.
        /// </summary>
        MemoryShared = 0x10000000,
        /// <summary>
        /// The section can be executed as code.
        /// </summary>
        MemoryExecute = 0x20000000,
        /// <summary>
        /// The section can be read.
        /// </summary>
        MemoryRead = 0x40000000,
        /// <summary>
        /// The section can be written to.
        /// </summary>
        MemoryWrite = 0x80000000
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_DOS_HEADER
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public char[] e_magic;       // Magic number
        public UInt16 e_cblp;    // Bytes on last page of file
        public UInt16 e_cp;      // Pages in file
        public UInt16 e_crlc;    // Relocations
        public UInt16 e_cparhdr;     // Size of header in paragraphs
        public UInt16 e_minalloc;    // Minimum extra paragraphs needed
        public UInt16 e_maxalloc;    // Maximum extra paragraphs needed
        public UInt16 e_ss;      // Initial (relative) SS value
        public UInt16 e_sp;      // Initial SP value
        public UInt16 e_csum;    // Checksum
        public UInt16 e_ip;      // Initial IP value
        public UInt16 e_cs;      // Initial (relative) CS value
        public UInt16 e_lfarlc;      // File address of relocation table
        public UInt16 e_ovno;    // Overlay number
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public UInt16[] e_res1;    // Reserved words
        public UInt16 e_oemid;       // OEM identifier (for e_oeminfo)
        public UInt16 e_oeminfo;     // OEM information; e_oemid specific
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public UInt16[] e_res2;    // Reserved words
        public Int32 e_lfanew;      // File address of new exe header

        private string _e_magic
        {
            get { return new string(e_magic); }
        }

        public bool isValid
        {
            get { return _e_magic == "MZ"; }
        }
    }

    public enum MachineType : ushort
    {
        Native = 0,
        I386 = 0x014c,
        Itanium = 0x0200,
        x64 = 0x8664
    }

    public enum MagicType : ushort
    {
        IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10b,
        IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20b
    }

    public enum SubSystemType : ushort
    {
        IMAGE_SUBSYSTEM_UNKNOWN = 0,
        IMAGE_SUBSYSTEM_NATIVE = 1,
        IMAGE_SUBSYSTEM_WINDOWS_GUI = 2,
        IMAGE_SUBSYSTEM_WINDOWS_CUI = 3,
        IMAGE_SUBSYSTEM_POSIX_CUI = 7,
        IMAGE_SUBSYSTEM_WINDOWS_CE_GUI = 9,
        IMAGE_SUBSYSTEM_EFI_APPLICATION = 10,
        IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER = 11,
        IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER = 12,
        IMAGE_SUBSYSTEM_EFI_ROM = 13,
        IMAGE_SUBSYSTEM_XBOX = 14

    }

    public enum DllCharacteristicsType : ushort
    {
        RES_0 = 0x0001,
        RES_1 = 0x0002,
        RES_2 = 0x0004,
        RES_3 = 0x0008,
        IMAGE_DLL_CHARACTERISTICS_DYNAMIC_BASE = 0x0040,
        IMAGE_DLL_CHARACTERISTICS_FORCE_INTEGRITY = 0x0080,
        IMAGE_DLL_CHARACTERISTICS_NX_COMPAT = 0x0100,
        IMAGE_DLLCHARACTERISTICS_NO_ISOLATION = 0x0200,
        IMAGE_DLLCHARACTERISTICS_NO_SEH = 0x0400,
        IMAGE_DLLCHARACTERISTICS_NO_BIND = 0x0800,
        RES_4 = 0x1000,
        IMAGE_DLLCHARACTERISTICS_WDM_DRIVER = 0x2000,
        IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE = 0x8000
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_DATA_DIRECTORY
    {
        public UInt32 VirtualAddress;
        public UInt32 Size;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_OPTIONAL_HEADER32
    {
        [FieldOffset(0)]
        public MagicType Magic;

        [FieldOffset(2)]
        public byte MajorLinkerVersion;

        [FieldOffset(3)]
        public byte MinorLinkerVersion;

        [FieldOffset(4)]
        public uint SizeOfCode;

        [FieldOffset(8)]
        public uint SizeOfInitializedData;

        [FieldOffset(12)]
        public uint SizeOfUninitializedData;

        [FieldOffset(16)]
        public uint AddressOfEntryPoint;

        [FieldOffset(20)]
        public uint BaseOfCode;

        // PE32 contains this additional field
        [FieldOffset(24)]
        public uint BaseOfData;

        [FieldOffset(28)]
        public uint ImageBase;

        [FieldOffset(32)]
        public uint SectionAlignment;

        [FieldOffset(36)]
        public uint FileAlignment;

        [FieldOffset(40)]
        public ushort MajorOperatingSystemVersion;

        [FieldOffset(42)]
        public ushort MinorOperatingSystemVersion;

        [FieldOffset(44)]
        public ushort MajorImageVersion;

        [FieldOffset(46)]
        public ushort MinorImageVersion;

        [FieldOffset(48)]
        public ushort MajorSubsystemVersion;

        [FieldOffset(50)]
        public ushort MinorSubsystemVersion;

        [FieldOffset(52)]
        public uint Win32VersionValue;

        [FieldOffset(56)]
        public uint SizeOfImage;

        [FieldOffset(60)]
        public uint SizeOfHeaders;

        [FieldOffset(64)]
        public uint CheckSum;

        [FieldOffset(68)]
        public SubSystemType Subsystem;

        [FieldOffset(70)]
        public DllCharacteristicsType DllCharacteristics;

        [FieldOffset(72)]
        public uint SizeOfStackReserve;

        [FieldOffset(76)]
        public uint SizeOfStackCommit;

        [FieldOffset(80)]
        public uint SizeOfHeapReserve;

        [FieldOffset(84)]
        public uint SizeOfHeapCommit;

        [FieldOffset(88)]
        public uint LoaderFlags;

        [FieldOffset(92)]
        public uint NumberOfRvaAndSizes;

        [FieldOffset(96)]
        public IMAGE_DATA_DIRECTORY ExportTable;

        [FieldOffset(104)]
        public IMAGE_DATA_DIRECTORY ImportTable;

        [FieldOffset(112)]
        public IMAGE_DATA_DIRECTORY ResourceTable;

        [FieldOffset(120)]
        public IMAGE_DATA_DIRECTORY ExceptionTable;

        [FieldOffset(128)]
        public IMAGE_DATA_DIRECTORY CertificateTable;

        [FieldOffset(136)]
        public IMAGE_DATA_DIRECTORY BaseRelocationTable;

        [FieldOffset(144)]
        public IMAGE_DATA_DIRECTORY Debug;

        [FieldOffset(152)]
        public IMAGE_DATA_DIRECTORY Architecture;

        [FieldOffset(160)]
        public IMAGE_DATA_DIRECTORY GlobalPtr;

        [FieldOffset(168)]
        public IMAGE_DATA_DIRECTORY TLSTable;

        [FieldOffset(176)]
        public IMAGE_DATA_DIRECTORY LoadConfigTable;

        [FieldOffset(184)]
        public IMAGE_DATA_DIRECTORY BoundImport;

        [FieldOffset(192)]
        public IMAGE_DATA_DIRECTORY IAT;

        [FieldOffset(200)]
        public IMAGE_DATA_DIRECTORY DelayImportDescriptor;

        [FieldOffset(208)]
        public IMAGE_DATA_DIRECTORY CLRRuntimeHeader;

        [FieldOffset(216)]
        public IMAGE_DATA_DIRECTORY Reserved;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_OPTIONAL_HEADER64
    {
        [FieldOffset(0)]
        public MagicType Magic;

        [FieldOffset(2)]
        public byte MajorLinkerVersion;

        [FieldOffset(3)]
        public byte MinorLinkerVersion;

        [FieldOffset(4)]
        public uint SizeOfCode;

        [FieldOffset(8)]
        public uint SizeOfInitializedData;

        [FieldOffset(12)]
        public uint SizeOfUninitializedData;

        [FieldOffset(16)]
        public uint AddressOfEntryPoint;

        [FieldOffset(20)]
        public uint BaseOfCode;

        [FieldOffset(24)]
        public ulong ImageBase;

        [FieldOffset(32)]
        public uint SectionAlignment;

        [FieldOffset(36)]
        public uint FileAlignment;

        [FieldOffset(40)]
        public ushort MajorOperatingSystemVersion;

        [FieldOffset(42)]
        public ushort MinorOperatingSystemVersion;

        [FieldOffset(44)]
        public ushort MajorImageVersion;

        [FieldOffset(46)]
        public ushort MinorImageVersion;

        [FieldOffset(48)]
        public ushort MajorSubsystemVersion;

        [FieldOffset(50)]
        public ushort MinorSubsystemVersion;

        [FieldOffset(52)]
        public uint Win32VersionValue;

        [FieldOffset(56)]
        public uint SizeOfImage;

        [FieldOffset(60)]
        public uint SizeOfHeaders;

        [FieldOffset(64)]
        public uint CheckSum;

        [FieldOffset(68)]
        public SubSystemType Subsystem;

        [FieldOffset(70)]
        public DllCharacteristicsType DllCharacteristics;

        [FieldOffset(72)]
        public ulong SizeOfStackReserve;

        [FieldOffset(80)]
        public ulong SizeOfStackCommit;

        [FieldOffset(88)]
        public ulong SizeOfHeapReserve;

        [FieldOffset(96)]
        public ulong SizeOfHeapCommit;

        [FieldOffset(104)]
        public uint LoaderFlags;

        [FieldOffset(108)]
        public uint NumberOfRvaAndSizes;

        [FieldOffset(112)]
        public IMAGE_DATA_DIRECTORY ExportTable;

        [FieldOffset(120)]
        public IMAGE_DATA_DIRECTORY ImportTable;

        [FieldOffset(128)]
        public IMAGE_DATA_DIRECTORY ResourceTable;

        [FieldOffset(136)]
        public IMAGE_DATA_DIRECTORY ExceptionTable;

        [FieldOffset(144)]
        public IMAGE_DATA_DIRECTORY CertificateTable;

        [FieldOffset(152)]
        public IMAGE_DATA_DIRECTORY BaseRelocationTable;

        [FieldOffset(160)]
        public IMAGE_DATA_DIRECTORY Debug;

        [FieldOffset(168)]
        public IMAGE_DATA_DIRECTORY Architecture;

        [FieldOffset(176)]
        public IMAGE_DATA_DIRECTORY GlobalPtr;

        [FieldOffset(184)]
        public IMAGE_DATA_DIRECTORY TLSTable;

        [FieldOffset(192)]
        public IMAGE_DATA_DIRECTORY LoadConfigTable;

        [FieldOffset(200)]
        public IMAGE_DATA_DIRECTORY BoundImport;

        [FieldOffset(208)]
        public IMAGE_DATA_DIRECTORY IAT;

        [FieldOffset(216)]
        public IMAGE_DATA_DIRECTORY DelayImportDescriptor;

        [FieldOffset(224)]
        public IMAGE_DATA_DIRECTORY CLRRuntimeHeader;

        [FieldOffset(232)]
        public IMAGE_DATA_DIRECTORY Reserved;
    }

    /// <summary>
    /// COR20Flags
    /// </summary>
    [ComVisible(true)]
    [Flags]
    public enum CorFlags
    {
        None = 0,
        ILOnly = 0x00000001,
        Requires32Bit = 0x00000002,
        ILLibrary = 0x00000004,
        StrongNameSigned = 0x00000008,
        NativeEntryPoint = 0x00000010,
        TrackDebugData = 0x00010000,
        Prefers32Bit = 0x00020000,
    }


    public enum EVENTNOTIFY
    {
        // A debuggee has been discovered for the session.  It
        // is not necessarily halted.
        DEBUG_NOTIFY_SESSION_ACTIVE = 0x00000000,
        // The session no longer has a debuggee.
        DEBUG_NOTIFY_SESSION_INACTIVE = 0x00000001,
        // The debuggee is halted and accessible.
        DEBUG_NOTIFY_SESSION_ACCESSIBLE = 0x00000002,
        // The debuggee is running or inaccessible.
        DEBUG_NOTIFY_SESSION_INACCESSIBLE = 0x00000003

    }

    public class DebugApi
    {
        public const uint DEBUG_ANY_ID = 0xffffffff;
        public const uint DEBUG_DATA_SharedUserData = 100008;
        [DllImport("dbgeng.dll")]
        internal static extern uint DebugCreate(ref Guid InterfaceId, [MarshalAs(UnmanagedType.IUnknown)] out object Interface);

        internal static EventsCallback eventsCallBack = null;


        internal static HRESULT LastHR;
        private static string processName = null;

        public static string ProcessName
        {
            get
            {
                if (String.IsNullOrEmpty(processName))
                {
                    INIT_API();
                    IDebugSystemObjects sysObj = (IDebugSystemObjects)Client;
                    StringBuilder process = new StringBuilder(1000);
                    uint size = 0;
                    sysObj.GetCurrentProcessExecutableName(process, 1000, out size);

                    string[] names = process.ToString().Split('\\');
                    processName = names[names.Length - 1].ToUpper();
                }
                return processName;
            }
        }



        public static DateTime ConvertDateTime(WinApi.FILETIME Time)
        {
            WinApi.SYSTEMTIME sysTime;
            WinApi.FileTimeToSystemTime(ref Time, out sysTime);
            return new DateTime(sysTime.wYear, sysTime.wMonth, sysTime.wDay, sysTime.wHour, sysTime.wMinute, sysTime.wSecond,
                sysTime.wMilliseconds);


        }

        public static DateTime ConvertDateTime(WinApi.KSYSTEM_TIME Time)
        {
            WinApi.FILETIME time;
            time.dwLowDateTime = Time.LowPart;
            time.dwHighDateTime = Time.High1Time;
            return ConvertDateTime(time);
        }

        public static DateTime ConvertDateTime(Int64 Time)
        {
            WinApi.FILETIME time;
            time.dwLowDateTime = (uint)(Time & UInt32.MaxValue);
            time.dwHighDateTime = (int)(Time >> 32);
            return ConvertDateTime(time);
        }

        /*
0:071> dt KUSER_SHARED_DATA  0x7FFE0000
kernel32!KUSER_SHARED_DATA
   +0x000 TickCountLowDeprecated : 0
   +0x004 TickCountMultiplier : 0xfa00000
   +0x008 InterruptTime    : _KSYSTEM_TIME
   +0x014 SystemTime       : _KSYSTEM_TIME
   +0x020 TimeZoneBias     : _KSYSTEM_TIME
   +0x02c ImageNumberLow   : 0x8664
   +0x02e ImageNumberHigh  : 0x8664
   +0x030 NtSystemRoot     : [260]  "C:\Windows"
   +0x238 MaxStackTraceDepth : 0
   +0x23c CryptoExponent   : 0
   +0x240 TimeZoneId       : 2
   +0x244 LargePageMinimum : 0x200000
   +0x248 Reserved2        : [7] 0
   +0x264 NtProductType    : 3 ( NtProductServer )
   +0x268 ProductTypeIsValid : 0x1 ''
   +0x26c NtMajorVersion   : 6
   +0x270 NtMinorVersion   : 1
   +0x274 ProcessorFeatures : [64]  ""
   +0x2b4 Reserved1        : 0x7ffeffff
   +0x2b8 Reserved3        : 0x80000000
   +0x2bc TimeSlip         : 0
   +0x2c0 AlternativeArchitecture : 0 ( StandardDesign )
   +0x2c4 AltArchitecturePad : [1] 0
   +0x2c8 SystemExpirationDate :  0x0
   +0x2d0 SuiteMask        : 0x112
   +0x2d4 KdDebuggerEnabled : 0 ''
   +0x2d5 NXSupportPolicy  : 0x3 ''
   +0x2d8 ActiveConsoleId  : 1
   +0x2dc DismountCount    : 0

         */
        public static WinApi.KUSER_SHARED_DATA SharedData
        {
            get
            {
                INIT_API();

                IDebugDataSpaces4 ds = (IDebugDataSpaces4)Client;

                // Allocate and Zero Memory
                IntPtr sharedData = Marshal.AllocHGlobal(sizeof(ulong));
                Marshal.WriteInt64(sharedData, 0);
                try
                {


                    uint dwSize = sizeof(ulong);
                    int hr = ds.ReadDebuggerData(DEBUG_DATA_SharedUserData,sharedData, dwSize, out dwSize);

                    ulong Address = 0;
                    if (hr != 0)
                    {

                        Address = 0x7FFE0000;
                    } else
                    {
                        // It should never get here, but if it does, get the default address as per SDK
                        Address = (ulong)Marshal.ReadInt64(sharedData);
                    }
                    WinApi.KUSER_SHARED_DATA SHARED_DATA;
                    if (!ReadMemory<WinApi.KUSER_SHARED_DATA>(Address, out SHARED_DATA, true))
                    {
                        
                        WriteLine("WARNING: SHARED DATA at {0:%p} is invalid", Address);
                        WriteLine("  Reverting to debug time which is imprecise");
                    }
                    return SHARED_DATA;
                } finally
                {
                    // No matter what happens, free memory
                    Marshal.FreeHGlobal(sharedData);
                }
            }
        }
        public static DateTime CurrentTimeDate
        {
            get
            {

                WinApi.KUSER_SHARED_DATA SHARED_DATA = SharedData;
                if (String.IsNullOrWhiteSpace(SharedData.NtSystemRoot) || SharedData.NtSystemRoot[1] != ':') // Bad SharedData
                {
                    uint ticks = 0;
                    control.GetCurrentTimeDate(out ticks);

                    DateTime targetTime = new DateTime(1970, 1, 1);
                    targetTime += new TimeSpan(ticks * (new TimeSpan(0, 0, 1)).Ticks);

                    return targetTime;

                }

                return ConvertDateTime(SHARED_DATA.SystemTime);
            }

        }

        public static DateTime CurrentTimeDateLocal
        {
            get
            {

                WinApi.KUSER_SHARED_DATA SHARED_DATA = SharedData;
                if (String.IsNullOrWhiteSpace(SharedData.NtSystemRoot) || SHARED_DATA.NtSystemRoot[1] != ':') // Bad SharedData
                {
                    uint ticks = 0;
                    control.GetCurrentTimeDate(out ticks);

                    DateTime targetTime = new DateTime(1970, 1, 1);
                    targetTime += new TimeSpan(ticks * (new TimeSpan(0, 0, 1)).Ticks);

                    return targetTime;

                }

                return ConvertDateTime(SHARED_DATA.SystemTime.ToLong() - SHARED_DATA.TimeZoneBias.ToLong());
            }

        }

        /// <summary>
        /// Return trues if it is an iDNA trace
        /// </summary>
        public static bool IsIDNA
        {
            get
            {
                INIT_API();
                DEBUG_CLASS cls;
                DEBUG_CLASS_QUALIFIER qlf;
                Control.GetDebuggeeType(out cls, out qlf);
                if (qlf == DEBUG_CLASS_QUALIFIER.KERNEL_IDNA || qlf == DEBUG_CLASS_QUALIFIER.USER_WINDOWS_IDNA)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Return trues if it is a dump (full or not)
        /// </summary>
        public static bool IsDump
        {
            get
            {
                INIT_API();
                DEBUG_CLASS cls;
                DEBUG_CLASS_QUALIFIER qlf;
                Control.GetDebuggeeType(out cls, out qlf);
                if (qlf == DEBUG_CLASS_QUALIFIER.KERNEL_FULL_DUMP || qlf == DEBUG_CLASS_QUALIFIER.KERNEL_DUMP
                    || qlf == DEBUG_CLASS_QUALIFIER.USER_WINDOWS_DUMP || qlf == DEBUG_CLASS_QUALIFIER.USER_WINDOWS_SMALL_DUMP)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Return trues if it is a full dump
        /// </summary>
        public static bool IsFullDump
        {
            get
            {
                INIT_API();
                DEBUG_CLASS cls;
                DEBUG_CLASS_QUALIFIER qlf;
                Control.GetDebuggeeType(out cls, out qlf);
                if (qlf == DEBUG_CLASS_QUALIFIER.KERNEL_FULL_DUMP || qlf == DEBUG_CLASS_QUALIFIER.USER_WINDOWS_DUMP)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Return trues if it is a mini dump
        /// </summary>
        public static bool IsMiniDump
        {
            get
            {
                INIT_API();
                DEBUG_CLASS cls;
                DEBUG_CLASS_QUALIFIER qlf;
                Control.GetDebuggeeType(out cls, out qlf);
                if (qlf == DEBUG_CLASS_QUALIFIER.KERNEL_DUMP || qlf == DEBUG_CLASS_QUALIFIER.USER_WINDOWS_SMALL_DUMP)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Return the string with the qualifiers
        /// </summary>
        public static string DumpTypeString
        {
            get
            {
                INIT_API();
                DEBUG_CLASS cls;
                DEBUG_CLASS_QUALIFIER qlf;
                Control.GetDebuggeeType(out cls, out qlf);
                return qlf.ToString();

            }
        }
        public static uint ThreadId
        {
            get
            {
                INIT_API();
                IDebugSystemObjects sysObj = (IDebugSystemObjects)Client;
                uint id = 0;
                sysObj.GetCurrentThreadId(out id);
                return id;
            }
        }

        public static uint SystemThreadId
        {
            get
            {
                INIT_API();
                IDebugSystemObjects sysObj = (IDebugSystemObjects)Client;
                uint id = 0;
                sysObj.GetCurrentThreadSystemId(out id);
                return id;
            }
        }

        public static uint ProcessSystemId
        {
            get
            {
                INIT_API();
                IDebugSystemObjects sysObj = (IDebugSystemObjects)Client;
                uint id = 0;
                sysObj.GetCurrentProcessSystemId(out id);
                return id;
            }
        }

        internal delegate uint IoctlDelegate(IG IoctlType, ref WDBGEXTS_CLR_DATA_INTERFACE lpvData, int cbSizeOfContext);
        internal delegate ulong GetExpressionDelegate([MarshalAs(UnmanagedType.LPStr)] string Expression);
        internal delegate uint CheckControlCDelegate();

        internal static IDebugClient5 client = null;
        internal static IDebugControl6 control = null;
        internal static DataTarget target = null;
        internal static ClrRuntime runtime = null;

        public static IDebugClient5 Client
        {
            get
            {
                if (client == null)
                {
                    INIT_API();

                }
                return client;
            }

        }

        public static IDebugControl6 Control
        {
            get
            {
                if (control == null)
                {
                    INIT_API();

                }
                return control;
            }
        }

        public static DataTarget Target
        {
            get
            {
                if (target == null)
                {
                    INIT_CLRAPI();

                }
                return target;
            }
        }

        public static ClrRuntime Runtime
        {
            get
            {
                if (runtime == null)
                {
                    INIT_CLRAPI();

                }
                return runtime;
            }
        }

        public static HRESULT Int2HResult(int Result)
        {
            // Convert to Uint
            uint value = BitConverter.ToUInt32(BitConverter.GetBytes(Result), 0);

            return Int2HResult(value);
        }

        public static HRESULT Int2HResult(uint Result)
        {
            HRESULT hr = HRESULT.E_UNEXPECTED;
            try
            {
                hr = (HRESULT)Result;

            }
            catch
            {

            }
            return hr;
        }
        private static IDebugClient CreateIDebugClient()
        {

            Guid guid = new Guid("27fe5639-8407-4f47-8364-ee118fb08ac8");
            object obj;
            var hr = DebugCreate(ref guid, out obj);
            if (hr < 0)
            {
                LastHR = Int2HResult(hr);
                WriteLine("SourceFix: Unable to acquire client interface");
                return null;
            }
            IDebugClient client = (IDebugClient5)obj;

            if (eventsCallBack == null)
                eventsCallBack = new EventsCallback();
            client.SetEventCallbacks(eventsCallBack);
            return client;
        }

        internal static void INIT_API()
        {
            LastHR = HRESULT.S_OK;
            if (client == null)
            {
                try
                {

                    client = (IDebugClient5)CreateIDebugClient();
                    control = (IDebugControl6)client;

                }
                catch
                {
                    LastHR = HRESULT.E_UNEXPECTED;
                }

            }
        }

        static IoctlDelegate ioctl = null;
        static GetExpressionDelegate getexpression = null;
        static CheckControlCDelegate checkcontrolc = null;


        internal static void FillApis()
        {
            WINDBG_EXTENSION_APIS apis = new WINDBG_EXTENSION_APIS();
            apis.nSize = (uint)Marshal.SizeOf(apis);

            if (Marshal.SizeOf(IntPtr.Zero) == 8)
            {
                LastHR = Int2HResult(Control.GetWindbgExtensionApis64(ref apis));
            }
            else
            {
                LastHR = Int2HResult(Control.GetWindbgExtensionApis32(ref apis));
            }


            if (LastHR != HRESULT.S_OK)
                throw new NullReferenceException("ERROR: Debug Apis failed");

            ioctl = (IoctlDelegate)Marshal.GetDelegateForFunctionPointer((IntPtr)apis.lpIoctlRoutine, typeof(IoctlDelegate));
            getexpression = (GetExpressionDelegate)Marshal.GetDelegateForFunctionPointer((IntPtr)apis.lpGetExpressionRoutine, typeof(GetExpressionDelegate));
            checkcontrolc = (CheckControlCDelegate)Marshal.GetDelegateForFunctionPointer((IntPtr)apis.lpCheckControlCRoutine, typeof(CheckControlCDelegate));
        }

        public static uint Ioctl(IG IoctlType, ref WDBGEXTS_CLR_DATA_INTERFACE lpvData, int cbSizeOfContext)
        {
            if (ioctl == null)
                FillApis();
            return ioctl(IoctlType, ref lpvData, cbSizeOfContext);
        }

        public static ulong GetExpression(string Expression)
        {
            if (getexpression == null)
                FillApis();
            return getexpression(Expression);
        }

        public static bool CheckControlC()
        {

            if (checkcontrolc == null)
                FillApis();
            Application.DoEvents(); // process messages
            if (checkcontrolc() == 0 && Control.GetInterrupt() != 0)
                return false;
            WriteLine("Interrupted by user.");
            throw new Exception("Control-C was pressed");
            return true;

        }

        public static void InitApi()
        {
            INIT_API();
        }

        public static void InitClr(ClrRuntime Runtime)
        {
            InitApi();
            runtime = Runtime;
        }

        internal static void INIT_CLRAPI()
        {
            INIT_API();
            if (LastHR != HRESULT.S_OK)
            {
                runtime = null;
                target = null;
                return;
            }
            if (runtime != null)
            {
                return;
            }

            Guid IXCLRData = new Guid("5c552ab6-fc09-4cb3-8e36-22fa03c798b7");



            WDBGEXTS_CLR_DATA_INTERFACE clr = new WDBGEXTS_CLR_DATA_INTERFACE();
            unsafe
            {
                clr.Iid = &IXCLRData;
            }

            if (Ioctl(IG.GET_CLR_DATA_INTERFACE, ref clr, Marshal.SizeOf(clr)) == 0)
            {
                WriteLine("ERROR: Unable to load .NET interface");
                WriteLine("Run the command below to check if .NET Interface can be loaded:");
                WriteLine(".cordll -u -ve -l");
                client = null;
                WriteLine("SourceFix: Unable to acquire .NET interface");
                LastHR = HRESULT.E_NOINTERFACE;
                target = null;
                runtime = null;

            }
            else
            {
                target = DataTarget.CreateFromDebuggerInterface(client);


                runtime = target.CreateRuntime(clr.Interface); //target.ClrVersions.Single().CreateRuntime(clr.Interface);
            }


        }

        public static bool IsTaget64Bits
        {
            get
            {
                IMAGE_FILE_MACHINE ifm = new IMAGE_FILE_MACHINE();
                Control.GetActualProcessorType(out ifm);
                return
                    ifm == IMAGE_FILE_MACHINE.ALPHA64 ||
                    ifm == IMAGE_FILE_MACHINE.AMD64 ||
                    ifm == IMAGE_FILE_MACHINE.AXP64;
            }
        }

        public static string SymPath
        {
            get
            {
                IDebugSymbols4 symbols = (IDebugSymbols4)Client;
                if (symbols != null)
                {
                    uint size = 1500;
                    StringBuilder path = new StringBuilder((int)size);
                    if (symbols.GetSymbolPath(path, (int)size, out size) == 0)
                    {
                        return path.ToString();
                    };
                }
                return "";
            }
            set
            {
                IDebugSymbols4 symbols = (IDebugSymbols4)Client;
                if (symbols != null)
                {
                    if (symbols.SetSymbolPath(value) == 0)
                    {
                        DebugWriteLine("Symbol Path Set to: {0}", value);
                    }
                    else
                    {
                        DebugWriteLine("Symbol Path Failed to be Set to: {0}", value);

                    };
                }
            }
        }



        public static Module ModuleFromScope
        {
            get
            {

                DEBUG_STACK_FRAME frame;
                IDebugSymbols4 symbols = (IDebugSymbols4)Client;
                ulong IP;

                if (symbols.GetScope(out IP, out frame, IntPtr.Zero, 0) == 0)
                {
                    StringBuilder modName = new StringBuilder(1500);
                    uint size;
                    ulong displ;
                    if (symbols.GetNameByOffset(IP, modName, 1500, out size, out displ) != 0)
                    {
                        return null;
                    }
                    Module mod = new Module(modName.ToString().Split('!')[0]);
                    if (mod.BaseAddress == 0)
                        return null;
                    return mod;
                }
                return null;
            }
        }

        public static void IgnoreSymbolMismatch()
        {
            IDebugSymbols4 symbols = (IDebugSymbols4)Client;
            SYMOPT opts;
            symbols.GetSymbolOptions(out opts);
            opts |= SYMOPT.LOAD_ANYTHING;
            symbols.SetSymbolOptions(opts);
        }

        public static string SourcePath
        {
            get
            {
                IDebugSymbols4 source = (IDebugSymbols4)Client;
                if (source != null)
                {
                    uint size = 1500;
                    StringBuilder path = new StringBuilder((int)size);
                    if (source.GetSourcePath(path, (int)size, out size) == 0)
                    {
                        return path.ToString();
                    };
                }
                return "";
            }
            set
            {
                IDebugSymbols4 symbols = (IDebugSymbols4)Client;
                if (symbols != null)
                {
                    if (symbols.SetSourcePath(value) == 0)
                    {
                        DebugWriteLine("Source Path Set to: {0}", value);
                    }
                    else
                    {
                        DebugWriteLine("Source Path Failed to be Set to: {0}", value);

                    };
                }
            }
        }

        public static void AddToSourcePath(string NewPath)
        {
            if (!SourcePath.Contains(NewPath))
            {
                SourcePath = SourcePath + ";" + NewPath;
            }
        }

        public static void AddToSymPath(string NewPath)
        {
            if (!SymPath.Contains(NewPath))
            {
                SymPath = SymPath + ";" + NewPath;
            }
        }
        public static void ReleaseAPI()
        {
            //client.SetEventCallbacks
            if (client != null)
                Marshal.ReleaseComObject(client);
            if (control != null)
                Marshal.ReleaseComObject(control);
        }

        public static string Execute(string Command)
        {
            OutputCallBack callBack = new OutputCallBack();

            string result = callBack.Execute(Client, Control, Command);
            return result;
        }

        public static UInt64 GetIdnaPosition()
        {
            Regex reg = new Regex(@"\s([0-9a-fA-F]+)\s");
            var matches = reg.Matches(Execute("!idna.position -c"));
            if (matches.Count != 1)
                return 0;
            return UInt64.Parse(matches[0].Value, NumberStyles.HexNumber);

        }

        public static UInt64 MoveToIdnaPosition(UInt64 Position)
        {
            if (Position == 100)
                Position = 99; // iDNA bug
            string cmd = Position < 101 ? String.Format("!idna.tt {0}", Position) : String.Format("!idna.tt {0:x8}", Position);

            var res = Execute(cmd);
            if (Position < 101)
            {
                if (res.Contains("Success"))
                {
                    return GetIdnaPosition();
                }
                else
                {
                    return 0; // It did not work.
                }
            }
            Regex reg = new Regex(@"Time\sTravel\sPosition:\s([0-9a-fA-F]+)\s");
            var matches = reg.Matches(res);
            if (matches.Count != 1)
                return 0;
            return UInt64.Parse(matches[0].Value, NumberStyles.HexNumber);
        }
        internal static bool CreateBreakpointFromExpression(string Expression, BreakPointCallBack CallBack, out uint Id)
        {
            IDebugBreakpoint br = null;
            Id = DEBUG_ANY_ID;
            if (Control.AddBreakpoint(DEBUG_BREAKPOINT_TYPE.CODE, DEBUG_ANY_ID, out br) == 0)
            {
                eventsCallBack.AddEvent(Expression, CallBack);

                br.GetId(out Id);
                br.SetOffsetExpression(Expression);
                DEBUG_BREAKPOINT_FLAG flag;
                br.GetFlags(out flag);

                if (!flag.HasFlag(DEBUG_BREAKPOINT_FLAG.DEFERRED) && !flag.HasFlag(DEBUG_BREAKPOINT_FLAG.ENABLED))
                {
                    br.AddFlags(DEBUG_BREAKPOINT_FLAG.ENABLED | DEBUG_BREAKPOINT_FLAG.ADDER_ONLY);
                }
                return true;
            }
            return false;
        }

        #region Helpers

        private static string pFormat = String.Format(":x{0}", Marshal.SizeOf(IntPtr.Zero) * 2);

        public static MEMORY_BASIC_INFORMATION64 AddressType(ulong Address)
        {
            IDebugDataSpaces2 data = (IDebugDataSpaces2)Client;
            MEMORY_BASIC_INFORMATION64 mbi = new MEMORY_BASIC_INFORMATION64();
            int result = data.QueryVirtual(Address,out mbi);
            if(result == (int)HRESULT.S_OK)
            {
                return mbi;
            }
            mbi.Protect = PAGE.NOACCESS;
            mbi.State = MEM.FREE;
            return mbi;
        }


        public static unsafe bool ReadMemory<T>(ulong Address, out T Target, bool Force = false)
        {

            if(!Force && AddressType(Address).Protect == PAGE.NOACCESS)
            {
                Target = default(T);
                return false;
            }

            IDebugDataSpaces3 data = (IDebugDataSpaces3)Client;
            uint read = (uint)Marshal.SizeOf(typeof(T));
            
            byte[] buffer = new byte[read];
            Target = default(T);
            if (data.ReadVirtual(Address, buffer, read, out read ) != (int)HRESULT.S_OK)
            {

                return false;
            }

            GCHandle pinnedTarget = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Target = (T)Marshal.PtrToStructure(pinnedTarget.AddrOfPinnedObject(), typeof(T));
            pinnedTarget.Free();


            
            return true;


        }

        public static string pointerFormat(string Message)
        {

            return Message.Replace(":%p", pFormat);

        }



        public static void DebugWrite(string Message, params object[] Params)
        {
#if DEBUG
            Write("[DEBUG] ");
            Write(Message, Params);
#endif

        }

        public static void DebugWriteLine(string Message, params object[] Params)
        {
#if DEBUG
            Write("[DEBUG] ");
            WriteLine(Message, Params);
#endif

        }
        public static void Write(string Message, params object[] Params)
        {
            if (Params == null)
                Out(Message);
            else
                Out(String.Format(pointerFormat(Message), Params));
        }

        public static void WriteLine(string Message, params object[] Params)
        {
            if (Params == null)
                Out(Message);
            else
                Out(String.Format(pointerFormat(Message), Params));
            Out("\n");
        }

        public static void WriteDml(string Message, params object[] Params)
        {
            if (Params == null)
                OutDml(Message);
            else
                OutDml(String.Format(pointerFormat(Message), Params));
        }

        public static void WriteDmlLine(string Message, params object[] Params)
        {
            if (Params == null)
                OutDml(Message);
            else
                OutDml(String.Format(pointerFormat(Message), Params));
            Out("\n");
        }
        public static void Out(string Message)
        {
            Control.ControlledOutput(DEBUG_OUTCTL.ALL_CLIENTS, DEBUG_OUTPUT.NORMAL, Message);
        }

        public static void OutDml(string Message)
        {
            Control.ControlledOutput(DEBUG_OUTCTL.ALL_CLIENTS | DEBUG_OUTCTL.DML, DEBUG_OUTPUT.NORMAL, Message);
        }



        public static bool WriteModuleToDisk(ulong DllBase, string FileName)
        {
            MEMORY_BASIC_INFORMATION64 mbi = new MEMORY_BASIC_INFORMATION64();
            
            if(!ReadMemory<MEMORY_BASIC_INFORMATION64>(DllBase, out mbi))
            {
                WriteLine("Unable to read memory at %p", DllBase);
                return false;
            }
            
            IMAGE_DOS_HEADER dosHeader;
            if (!ReadMemory<IMAGE_DOS_HEADER>(DllBase, out dosHeader))
            {
                WriteLine("[IMAGE_DOS_HEADER] Unable to read memory at %p", DllBase);
                return false;
            }

            if(!dosHeader.isValid )
            {
                WriteLine("Invalid image at %p", DllBase);
                return false;
            }

            IMAGE_NT_HEADERS64 Header;
            if (!ReadMemory<IMAGE_NT_HEADERS64>(DllBase + (ulong)dosHeader.e_lfanew, out Header))
            {
                WriteLine("[MAGE_NT_HEADERS] Unable to read memory at %p", DllBase + (ulong)dosHeader.e_lfanew);
                return false;
            }

            ulong sectionAddr = DllBase + (ulong)dosHeader.e_lfanew + 
                (ulong)Marshal.OffsetOf(typeof(IMAGE_NT_HEADERS64), "OptionalHeader") +
                 Header.FileHeader.SizeOfOptionalHeader;

            IMAGE_SECTION_HEADER section = new IMAGE_SECTION_HEADER();


            int nSection = Header.FileHeader.NumberOfSections;
            MemLocation[] memLoc = new MemLocation[nSection];

            int indxSec = -1;
            int slot;

            for (int n = 0; n < nSection; n++)
            {
                if (!ReadMemory<IMAGE_SECTION_HEADER>(sectionAddr, out section))
                {
                    WriteLine("Fail to read PE section info\n");
                    return false;
                }


                for (slot = 0; slot <= indxSec; slot++)
                {
                    if ((ulong)section.PointerToRawData < (ulong)memLoc[slot].FileAddr)
                        break;
                }
                for (int k = indxSec; k >= slot; k--)
                    memLoc[k] = memLoc[k + 1];

                memLoc[slot].VAAddr = (IntPtr)section.VirtualAddress;
                memLoc[slot].VASize = (IntPtr)section.VirtualSize;
                memLoc[slot].FileAddr = (IntPtr)section.PointerToRawData;
                memLoc[slot].FileSize = (IntPtr)section.SizeOfRawData;

                indxSec++;
                sectionAddr += (ulong)Marshal.SizeOf(section);
                //PEFile file = new PEFile()
            }
            using (FileStream file = new FileStream(FileName, FileMode.CreateNew))
            {

                uint pageSize;
                Control.GetPageSize(out pageSize);

                byte[] buffer = new byte[pageSize];

                // NT PE Headers
                ulong dwAddr = DllBase;
                ulong dwEnd = DllBase + Header.OptionalHeader.SizeOfHeaders;
                uint nRead;


                IDebugDataSpaces3 data = (IDebugDataSpaces3)client;

                while (dwAddr < dwEnd)
                {
                    nRead = pageSize;
                    if (dwEnd - dwAddr < nRead)
                        nRead = (uint)(dwEnd - dwAddr);

                    if (data.ReadVirtual(dwAddr, buffer, nRead, out nRead) == (int)HRESULT.S_OK)
                        file.Write(buffer, 0, (int)nRead);
                    else
                    {
                        WriteLine("Fail to read memory\n");
                        return false;
                    }

                    dwAddr += nRead;
                }


                for (slot = 0; slot <= indxSec; slot++)
                {
                    if (!IsTaget64Bits)
                    {
                        if ( true /*bIsImage*/)
                            dwAddr = DllBase + (ulong)memLoc[slot].VAAddr;
                        else
                            dwAddr = DllBase + (ulong)memLoc[slot].FileAddr;
                    } else
                        dwAddr = DllBase + (ulong)memLoc[slot].VAAddr;
                    dwEnd = (ulong)memLoc[slot].FileSize + dwAddr - 1;

                    while (dwAddr <= dwEnd)
                    {
                        nRead = pageSize;
                        if (dwEnd - dwAddr + 1 < pageSize)
                            nRead = (uint)(dwEnd - dwAddr + 1);

                        if (data.ReadVirtual(dwAddr, buffer, nRead, out nRead) == (int)HRESULT.S_OK)
                            file.Write(buffer, 0, (int)nRead);
                        else
                        {
                            WriteLine("Fail to read memory\n");
                            return false;
                        }

                        dwAddr += pageSize;
                    }
                }

            }
            return true;
        }


        #endregion

    }
}
