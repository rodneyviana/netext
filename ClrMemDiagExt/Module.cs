using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace NetExt.Shim
{
    public struct LANGANDCODEPAGE
    {
        public uint wLanguage;
        public uint wCodePage;

        public void SetValue(byte[] Bytes, int Index = 0)
        {
            wLanguage = Bytes[Index] + ((uint)(Bytes[Index + 1]) << 8);
            wCodePage = Bytes[Index+2] + ((uint)(Bytes[Index + 3]) << 8);
        }

        public string VarQueryValue(string SubBlock)
        {
            string query = String.Format("\\StringFileInfo\\{0:x04}{1:x04}\\{2}",
                wLanguage,
                wCodePage,
                SubBlock);
            return query;
        }
    }

    public enum ImageType : ushort
    {
        None = 0x0,
        Pe32bit = 0x10B,
        Pe64bit = 0x20B,
    }

    [StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct IMAGE_IMPORT_MODULE_DIRECTORY
    {

        /// DWORD->unsigned int
        public uint dwRVAFunctionNameList;

        /// DWORD->unsigned int
        public uint dwUseless1;

        /// DWORD->unsigned int
        public uint dwUseless2;

        /// DWORD->unsigned int
        public uint dwRVAModuleName;

        /// DWORD->unsigned int
        public uint dwRVAFunctionAddressList;
    }


    public class Module
    {
        protected static List<Module> modules = null;

        static IDebugSymbols5 symbol = null;

        public static IDebugSymbols5 Symbol
        {
            get
            {
                if (symbol == null)
                    symbol = (IDebugSymbols5)DebugApi.Client;
                return symbol;
            }
        }

        public static uint Count
        {
            get
            {
                uint loaded;
                uint unloaded;
                Symbol.GetNumberModules(out loaded, out unloaded);
                return loaded;
            }
        }

        public static uint UnloadedCount
        {
            get
            {
                uint loaded;
                uint unloaded;
                Symbol.GetNumberModules(out loaded, out unloaded);
                return unloaded;
            }
        }
        public static List<Module> Modules
        {
            get
            {
                if (modules == null || modules.Count == 0)
                {
                    modules = new List<Module>();

                    for (int i = 0; i < Count; i++)
                    {
                        ulong baseAddr;
                        if (Symbol.GetModuleByIndex((uint)i, out baseAddr) == (int)HRESULT.S_OK)
                        {
                            Module mod = new Module(baseAddr);
                            if (mod.IsValid)
                                modules.Add(mod);
                        }
                        else
                        {
                            DebugApi.WriteLine("Unable to get module at index {0}", i);
                        }
                    }
                }
                return modules;
            }
        }


        public static void Clear()
        {
            modules = null;
            symbol = null;
        }

        public string FullPath
        {
            protected set;
            get;
        }
        public string Name
        {
            get
            {
                if (String.IsNullOrWhiteSpace(FullPath))
                    return "";
                
                string[] parts = FullPath.Split('\\');

                return parts[parts.Length - 1];
            }

        }

        public IMAGE_DOS_HEADER DOSHeader
        {
            get
            {
                IMAGE_DOS_HEADER dosHeader;
                if (!DebugApi.ReadMemory<IMAGE_DOS_HEADER>(BaseAddress, out dosHeader))
                {
                    DebugApi.WriteLine("[IMAGE_DOS_HEADER] Unable to read memory at %p", BaseAddress);
                }
                return dosHeader;
            }
        }

        protected ImageType imageType = ImageType.None;

        public ImageType FileImageType
        {
            get
            {
                if (imageType == ImageType.None)
                {
                    var dosHeader = DOSHeader;

                    if (!DebugApi.ReadMemory<IMAGE_NT_HEADERS32>(BaseAddress + (ulong)dosHeader.e_lfanew, out ntHeader32))
                    {
                        DebugApi.WriteLine("[IMAGE_NT_HEADERS] Unable to read memory at %p", BaseAddress + (ulong)dosHeader.e_lfanew);
                        return imageType;
                    }
                    if (ntHeader32.Signature != 0x4550)
                    {
                        DebugApi.WriteLine("[IMAGE_NT_HEADERS] Invalid header signature at %p", BaseAddress + (ulong)dosHeader.e_lfanew);
                        return imageType;
                    }
                    imageType = (ImageType)ntHeader32.OptionalHeader.Magic;
                }
                return imageType;
            }
        }

        protected ulong sectionStart = 0;

        protected IMAGE_NT_HEADERS32 ntHeader32 = new IMAGE_NT_HEADERS32();
        protected IMAGE_NT_HEADERS64 ntHeader = new IMAGE_NT_HEADERS64();

        public ulong SectionStart
        {
            get
            {
                if (sectionStart == 0)
                {
                    sectionStart = BaseAddress + (ulong)DOSHeader.e_lfanew;
                }
                return sectionStart;
            }
        }

        public ulong RvaToOffset(ulong Rva)
        {

            var sectionId = SectionStart;
            uint sections = 0;
            MEMORY_BASIC_INFORMATION64 mbi = new MEMORY_BASIC_INFORMATION64();

            mbi = DebugApi.AddressType(BaseAddress);
            if (!DebugApi.IsIDNA && mbi.Protect == PAGE.NOACCESS)
            {
                DebugApi.WriteLine("Unable to read memory at %p", BaseAddress);
                return 0;
            }

            bool bIsImage = (DebugApi.IsIDNA || mbi.Type == MEM.IMAGE || mbi.Type == MEM.PRIVATE);

            if (FileImageType == ImageType.Pe32bit)
            {
                sections = NTHeader32.FileHeader.NumberOfSections;
                sectionId += (ulong)Marshal.OffsetOf(typeof(IMAGE_NT_HEADERS32), "OptionalHeader") +
                     NTHeader32.FileHeader.SizeOfOptionalHeader;
            }
            if (FileImageType == ImageType.Pe64bit)
            {
                sections = NTHeader.FileHeader.NumberOfSections;
                sectionId += (ulong)Marshal.OffsetOf(typeof(IMAGE_NT_HEADERS64), "OptionalHeader") +
                     NTHeader.FileHeader.SizeOfOptionalHeader;
            }
            for (int i = 0; i < sections; i++)
            {
                IMAGE_SECTION_HEADER section = new IMAGE_SECTION_HEADER();
                if (!DebugApi.ReadMemory<IMAGE_SECTION_HEADER>(sectionId, out section))
                {
                    Debug.WriteLine("[RVA] Unable to read IMAGE_SECTION_HEADER");
                    return 0;
                }

                if ((Rva >= section.VirtualAddress) &&
                    (Rva < (section.VirtualAddress + section.SizeOfRawData)))
                {
                    // RVA is in this section, we can now resolve it.

                    ulong start = 0;
                    if (bIsImage)
                        start = section.VirtualAddress;
                    else
                        start = section.PointerToRawData;

                    ulong address = BaseAddress + (ulong)Rva - (ulong)section.VirtualAddress + start; // (ulong)section.PointerToRawData;


                    return address;
                }

                // Next section
                sectionId += (uint)Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER));

            }

            // RVA could not be found
            return 0;
        }

        public IMAGE_NT_HEADERS32 NTHeader32
        {
            get
            {
                if (FileImageType != ImageType.Pe32bit)
                {
                    ntHeader32.Signature = 0;
                    return ntHeader32;
                }
                if (ntHeader32.Signature == 0x00004550)
                {
                    return ntHeader32;
                }

                

                var dosHeader = DOSHeader;
                if (!dosHeader.isValid)
                {
                    ntHeader32.Signature = 0;
                    return ntHeader32;
                }


                if (FileImageType == ImageType.Pe32bit)
                {
                    if (!DebugApi.ReadMemory<IMAGE_NT_HEADERS32>(BaseAddress + (ulong)dosHeader.e_lfanew, out ntHeader32))
                    {
                        DebugApi.WriteLine("[IMAGE_NT_HEADERS32] Unable to read memory at %p", BaseAddress + (ulong)dosHeader.e_lfanew);
                        ntHeader32.Signature = 0;
                    }
                }
                else
                {
                    ntHeader32.Signature = 0;
                }

                return ntHeader32;
            }
        }
        
        public IMAGE_NT_HEADERS64 NTHeader
        {
            get
            {
                if (FileImageType != ImageType.Pe64bit)
                {
                    ntHeader.Signature = 0;
                    return ntHeader;
                }

                if (ntHeader.Signature == 0x00004550)
                {
                    return ntHeader;
                }


                var dosHeader = DOSHeader;
                if(!dosHeader.isValid)
                {
                    ntHeader.Signature = 0;
                    return ntHeader;
                }


                if (!DebugApi.ReadMemory<IMAGE_NT_HEADERS64>(BaseAddress + (ulong)dosHeader.e_lfanew, out ntHeader))
                {
                    DebugApi.WriteLine("[IMAGE_NT_HEADERS] Unable to read memory at %p", BaseAddress + (ulong)dosHeader.e_lfanew);

                }
                return ntHeader;
            }
        }

        public IMAGE_OPTIONAL_HEADER32 OptionalHeader32
        {
            get
            {
                ulong sectionAddr = BaseAddress + (ulong)DOSHeader.e_lfanew +
                    (ulong)Marshal.OffsetOf(typeof(IMAGE_NT_HEADERS64), "OptionalHeader");
                var opHeader = new IMAGE_OPTIONAL_HEADER32();
                if (!DebugApi.ReadMemory<IMAGE_OPTIONAL_HEADER32>(sectionAddr, out opHeader))
                {
                    DebugApi.WriteLine("Fail to read PE section info\n");
                    opHeader.ImageBase = 0;
                    opHeader.CLRRuntimeHeader = new IMAGE_DATA_DIRECTORY();
                }
                return opHeader;
            }
        }

        public IMAGE_OPTIONAL_HEADER64 OptionalHeader
        {
            get
            {
                ulong sectionAddr = BaseAddress + (ulong)DOSHeader.e_lfanew +
                    (ulong)Marshal.OffsetOf(typeof(IMAGE_NT_HEADERS64), "OptionalHeader");
                var opHeader = new IMAGE_OPTIONAL_HEADER64();
                if (!DebugApi.ReadMemory<IMAGE_OPTIONAL_HEADER64>(sectionAddr, out opHeader))
                {
                    DebugApi.WriteLine("Fail to read PE section info\n");
                    opHeader.ImageBase = 0;
                    opHeader.CLRRuntimeHeader = new IMAGE_DATA_DIRECTORY();
                }
                return opHeader;
            }
        }

        public IMAGE_COR20_HEADER CorHeader
        {
            get
            {
                IMAGE_COR20_HEADER corHeader = new IMAGE_COR20_HEADER();
                ulong VirtualAddress = 0;

                
                if (FileImageType == ImageType.Pe64bit)
                {
                    var opHeader = OptionalHeader;
                    VirtualAddress = opHeader.CLRRuntimeHeader.VirtualAddress;
                }
                else
                {
                    var opHeader = OptionalHeader32;
                    VirtualAddress = opHeader.CLRRuntimeHeader.VirtualAddress;
                }

                if (VirtualAddress != 0)
                {

                    ulong sectionAddr = RvaToOffset(VirtualAddress);

                    if (!DebugApi.ReadMemory<IMAGE_COR20_HEADER>(sectionAddr, out corHeader))
                    {

                        DebugApi.WriteLine("[CORHEADER] Fail to read PE section info at %p\n", sectionAddr);
                    }
                    else
                    {
                        return corHeader;
                    }
                }

                corHeader.MajorRuntimeVersion = 0;
                corHeader.MinorRuntimeVersion = 0;
                return corHeader;

            }
        }

        public IMAGE_DEBUG_DIRECTORY DebugInfo
        {
            get
            {
                IMAGE_DEBUG_DIRECTORY debugInfo = new IMAGE_DEBUG_DIRECTORY();

                ulong sectionAddr = 0;
                ulong limit = 0;
                ulong virtualAddress = 0;
                if (FileImageType == ImageType.Pe64bit)
                {
                    var opHeader = OptionalHeader;
                    sectionAddr = RvaToOffset(opHeader.Debug.VirtualAddress);
                    limit = sectionAddr + opHeader.Debug.Size;
                    virtualAddress = opHeader.Debug.VirtualAddress;
                }

                if (FileImageType == ImageType.Pe32bit)
                {
                    var opHeader32 = OptionalHeader32;
                    sectionAddr = RvaToOffset(opHeader32.Debug.VirtualAddress);
                    limit = sectionAddr + opHeader32.Debug.Size;
                    virtualAddress = opHeader32.Debug.VirtualAddress;
                }

                if (virtualAddress != 0)
                {


                    while (sectionAddr < limit)
                    {
                        if (!DebugApi.ReadMemory<IMAGE_DEBUG_DIRECTORY>(sectionAddr, out debugInfo))
                        {

                            DebugApi.WriteLine("Fail to read PE section info\n");
                        }
                        else
                        {
                            if(debugInfo.Type != IMAGE_DEBUG_TYPE.UNKNOWN)
                                return debugInfo;
                        }
                        sectionAddr += (ulong)Marshal.SizeOf(debugInfo);
                    }
                }
                
                debugInfo.Type = IMAGE_DEBUG_TYPE.UNKNOWN;
                return debugInfo;
            }
        }

        public IMAGE_DEBUG_TYPE DebugType
        {
            get
            {
                
                return DebugInfo.Type;
            }
        }

        public DebuggableAttribute.DebuggingModes ClrDebugType
        {
            get
            {
                if(!IsClr)
                {
                    return DebuggableAttribute.DebuggingModes.None;
                }
                
                foreach(var module in DebugApi.Runtime.Modules)
                {
                    if(BaseAddress == module.ImageBase)
                    {
                        return module.DebuggingMode;
                    }
                }
                return DebuggableAttribute.DebuggingModes.None;
            }
        }
        public Version DotNetVersion
        {
            get
            {
                var corHeader = CorHeader;
                return new Version(corHeader.MajorRuntimeVersion, corHeader.MinorRuntimeVersion);
            }
        }

        public bool IsClr
        {
            get
            {
                return DotNetVersion.Major != 0;
            }
        }

        public CorFlags CLRFlags
        {
            get
            {
                return (CorFlags)CorHeader.Flags;
            }
        }

        public bool SaveToStream(Stream ModuleStream)
        {
            MEMORY_BASIC_INFORMATION64 mbi = new MEMORY_BASIC_INFORMATION64();

            mbi = DebugApi.AddressType(BaseAddress);
            if (mbi.Protect == PAGE.NOACCESS && !DebugApi.IsIDNA)
            {
                DebugApi.WriteLine("Unable to read memory at %p", BaseAddress);
                return false;
            }

            bool bIsImage = DebugApi.IsIDNA || (mbi.Type == MEM.IMAGE || mbi.Type == MEM.PRIVATE);

            IMAGE_DOS_HEADER dosHeader = DOSHeader;


            if (!dosHeader.isValid)
            {
                return false;
            }

            // NT PE Headers
            ulong dwAddr = BaseAddress;
            ulong dwEnd = 0;  
            uint nRead;

            IMAGE_SECTION_HEADER section = new IMAGE_SECTION_HEADER();
            ulong sectionAddr = 0;
            int nSection = 0;  
            if (FileImageType == ImageType.Pe64bit)
            {
                sectionAddr = BaseAddress + (ulong)dosHeader.e_lfanew +
                    (ulong)Marshal.OffsetOf(typeof(IMAGE_NT_HEADERS64), "OptionalHeader") +
                     NTHeader.FileHeader.SizeOfOptionalHeader;
                nSection = NTHeader.FileHeader.NumberOfSections;
                dwEnd = BaseAddress + NTHeader.OptionalHeader.SizeOfHeaders;

            }

            if (FileImageType == ImageType.Pe32bit)
            {
                sectionAddr = BaseAddress + (ulong)dosHeader.e_lfanew +
                    (ulong)Marshal.OffsetOf(typeof(IMAGE_NT_HEADERS32), "OptionalHeader") +
                     NTHeader32.FileHeader.SizeOfOptionalHeader;
                nSection = NTHeader32.FileHeader.NumberOfSections;
                dwEnd = BaseAddress + NTHeader32.OptionalHeader.SizeOfHeaders;

            }


            MemLocation[] memLoc = new MemLocation[nSection];

            int indxSec = -1;
            int slot;

            for (int n = 0; n < nSection; n++)
            {
                if (!DebugApi.ReadMemory<IMAGE_SECTION_HEADER>(sectionAddr, out section))
                {
                    DebugApi.WriteLine("[IMAGE_SECTION_HEADER] Fail to read PE section info at %p\n", sectionAddr);
                    return false;
                }


                for (slot = 0; slot <= indxSec; slot++)
                {
                    if ((ulong)section.PointerToRawData < (ulong)memLoc[slot].FileAddr)
                        break;
                }
                for (int k = indxSec; k >= slot; k--)
                    memLoc[k + 1] = memLoc[k];

                memLoc[slot].VAAddr = (IntPtr)section.VirtualAddress;
                memLoc[slot].VASize = (IntPtr)section.VirtualSize;
                memLoc[slot].FileAddr = (IntPtr)section.PointerToRawData;
                memLoc[slot].FileSize = (IntPtr)section.SizeOfRawData;

                indxSec++;
                sectionAddr += (ulong)Marshal.SizeOf(section);

            }

            uint pageSize;
            DebugApi.Control.GetPageSize(out pageSize);

            byte[] buffer = new byte[pageSize];




            IDebugDataSpaces3 data = (IDebugDataSpaces3)DebugApi.Client;

            while (dwAddr < dwEnd)
            {
                nRead = pageSize;
                if (dwEnd - dwAddr < nRead)
                    nRead = (uint)(dwEnd - dwAddr);

                if (data.ReadVirtual(dwAddr, buffer, nRead, out nRead) == (int)HRESULT.S_OK)
                    ModuleStream.Write(buffer, 0, (int)nRead);
                else
                {
                    DebugApi.WriteLine("Fail to read memory\n");
                    return false;
                }

                dwAddr += nRead;
            }


            for (slot = 0; slot <= indxSec; slot++)
            {
                //if (!DebugApi.IsTaget64Bits)
                //{
                    if (bIsImage)
                        dwAddr = BaseAddress + (ulong)memLoc[slot].VAAddr;
                    else
                        dwAddr = BaseAddress + (ulong)memLoc[slot].FileAddr;
                //}
                //else
                //    dwAddr = BaseAddress + (ulong)memLoc[slot].FileAddr;
                dwEnd = (ulong)memLoc[slot].FileSize + dwAddr - 1;

                while (dwAddr <= dwEnd)
                {
                    nRead = pageSize;
                    if (dwEnd - dwAddr + 1 < pageSize)
                        nRead = (uint)(dwEnd - dwAddr + 1);

                    if (data.ReadVirtual(dwAddr, buffer, nRead, out nRead) == (int)HRESULT.S_OK)
                        ModuleStream.Write(buffer, 0, (int)nRead);
                    else
                    {
                        DebugApi.WriteLine("Fail to read memory\n");
                        return false;
                    }

                    dwAddr += pageSize;
                }
            }


            return true;

        }
        public ulong BaseAddress
        {
            protected set;
            get;
        }

        public bool IsValid
        {
            get
            {
                return BaseAddress != 0;
            }
        }

        public uint Index
        {
            protected set;
            get;
        }

        public string Version
        {
            get
            {
                return GetVersionInfo("FileVersion");
            }
            //FileVersion
        }

        public string ProductVersion
        {
            get
            {
                return GetVersionInfo("ProductVersion");
            }
        }

        public string CompanyName
        {
            get
            {
                return GetVersionInfo("CompanyName");
            }
        }

        public string LegalCopyright
        {
            get
            {
                return GetVersionInfo("LegalCopyright");
            }
        }

        public string ProductName
        {
            get
            {
                return GetVersionInfo("ProductName");
            }
        }
        public string OriginalFilename
        {
            get
            {
                return GetVersionInfo("OriginalFilename");
            }
        }

        public Version VersionInfo
        {
            get
            {
                try
                {
                    Version ver = new System.Version(ProductVersion);
                    return ver;
                }
                catch
                {
                    return new System.Version();
                }

                /*
                Version ver = new System.Version(ProductVersion);


                Regex reg = new Regex(@"?<major>[0-9]+)\.(?<minor>[0-9]+)\.(?<release>[0-9]+)(?:\.(?<build>[0-9]+))?");
                var match = reg.Match(ProductVersion);
                if(match.Success)
                {
                    Version ver = new System.Version()
                    ver.Major = match.Groups["major"];

                }
                */
            }
        }

        public string GetVersionInfo(string Property)
        {
            uint size = 1000;
            byte[] buffer = new byte[size];


            if (Symbol.GetModuleVersionInformation(Index, 0, @"\VarFileInfo\Translation", buffer, size, out size) == (int)HRESULT.S_OK)
            {
                LANGANDCODEPAGE cp = new LANGANDCODEPAGE();
                cp.SetValue(buffer);
                unsafe
                {
                    fixed (byte* p = buffer)
                    {
                        IntPtr ptr = (IntPtr)p;
                        Symbol.GetModuleVersionInformationWide(Index, 0, cp.VarQueryValue(Property), ptr, 1000, out size);
                    }
                }
                if (size < 2)
                    return "";
                string str = System.Text.ASCIIEncoding.Unicode.GetString(buffer, 0, (int)size - 2);
                return str.Replace("\0",""); // Resolves an issue with empty strings

            }

            return null;
            //https://msdn.microsoft.com/en-us/library/windows/desktop/ms647464(v=vs.85).aspx
            //https://msdn.microsoft.com/en-us/library/windows/hardware/ff547170(v=vs.85).aspx
        }

        public Module(string ModuleName)
        {
            uint index = 0;
            ulong addr = 0;
            if (Symbol.GetModuleByModuleName2(ModuleName, 0, DEBUG_GETMOD.NO_UNLOADED_MODULES, out index, out addr) == (int)HRESULT.S_OK)
            {
                Index = index;
                uint size = 1000;
                StringBuilder buffer = new StringBuilder((int)size);
                BaseAddress = addr;
                if (Symbol.GetModuleNameString(DEBUG_MODNAME.IMAGE, index, 0, buffer, size, out size) == (int)HRESULT.S_OK)
                {
                    FullPath = buffer.ToString();
                }
            }
            else
            {
                FullPath = null;
                BaseAddress = 0;
            }
        }

        public Module(ulong Offset)
        {
            uint index = 0;
            ulong addr = 0;
            if (Symbol.GetModuleByOffset2(Offset, 0, DEBUG_GETMOD.NO_UNLOADED_MODULES, out index, out addr) == (int)HRESULT.S_OK)
            {
                Index = index;
                uint size = 1000;
                StringBuilder buffer = new StringBuilder((int)size);
                BaseAddress = addr;
                if (Symbol.GetModuleNameString(DEBUG_MODNAME.IMAGE, index, 0, buffer, size, out size) == (int)HRESULT.S_OK)
                {
                    FullPath = buffer.ToString();
                }
            }
            else
            {
                FullPath = null;
                BaseAddress = 0;
            }
        }
    }
}
