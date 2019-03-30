/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
  
  Small portion of the code implementation in this file is based on a public proof-of-concept sample application from
    Microsoft.Diagnostics.Runtime's developer Lee Culver
============================================================================================================*/

#define _X86X64     // DLLEXPORT DOES NOT WORK WITH MSIL  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using RGiesecke.DllExport;
using NetExt.HeapCacheUtil;
using System.Net;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace NetExt.Shim
{
#if _X86X64
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void DebugPrint([MarshalAs(UnmanagedType.LPWStr)] string Message);
#else
    public delegate void DebugPrint(string Message);
#endif


#if _X86X64
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
#endif
    public delegate int EnumObjects(ulong Address);


#if _X86X64
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
#endif
    public delegate int ShouldStop();
    public static class Exports
    {

        private static DebugPrint callBack = null;
        private static DebugPrint callBackDml = null;
        private static EnumObjects enumCallBack = null;
        private static ShouldStop stopCallBack = null;

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static void SetEnumCallBack(EnumObjects EnumCallBack)
        {
            enumCallBack = EnumCallBack;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static void SetStopCallBack(ShouldStop StopCallBack)
        {
            stopCallBack = StopCallBack;
        }

        public static bool isInterrupted()
        {
            if (stopCallBack != null)
            {
                return stopCallBack() == 1;
            }
            return false;
        }

        public static bool MoveNext(ulong Address)
        {
            if(enumCallBack != null)
                return enumCallBack(Address) == 1;
            return false;

        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static void SetOutputCallBack(DebugPrint CallBack, DebugPrint CallBackDml)
        {
            callBack = CallBack;
            callBackDml = CallBackDml;
        }

        private static string pFormat = String.Format(":x{0}", Marshal.SizeOf(IntPtr.Zero) * 2);
        public static string pointerFormat(string Message)
        {

            return Message.Replace(":%p", pFormat);

        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static void Echo([MarshalAs(UnmanagedType.LPWStr)]string Message)
        {
            Out(Message);
        }

        public static void Write(string Message, params object[] Params)
        {
            if(Params == null)
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
            if (callBack == null)
                return;
            if (Message.Length < 16000)
                callBack(Message);
            else
            {
                int start = 0;
                while (start + 16000 < Message.Length)
                {
                    callBack(Message.Substring(start, 16000));
                    start += 16000;
                }
                if (start < Message.Length)
                    callBack(Message.Substring(start));
            }
        }

        public static void OutDml(string Message)
        {
            if (callBackDml == null)
                return;
            if (Message.Length < 16000)
                callBackDml(Message);
            else
            {
                int start = 0;
                while (start + 16000 < Message.Length)
                {
                    callBack(Message.Substring(start, 16000));
                    start += 16000;
                }
                if (start < Message.Length)
                    callBackDml(Message.Substring(start));
            }
  

        }

        private static string netExtPath = null;
        private static bool isInit = false;

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static void CreateFromIDebugClient([MarshalAs(UnmanagedType.LPWStr)]string DllPath, [MarshalAs((UnmanagedType)25)] Object iDebugClient,
            [Out] [MarshalAs((UnmanagedType)28)] out IMDTarget ppTarget)
        {
            string path = Path.GetDirectoryName(typeof(Exports).Module.FullyQualifiedName);
            //
            // Changes to enable NetExt dlls to be anywhere
            //
            if(String.IsNullOrEmpty(netExtPath))
            {
                netExtPath = DllPath[DllPath.Length - 1] != '\\' ? DllPath + "\\" : DllPath;

            }

            if (!isInit)
            {
                AppDomain.CurrentDomain.UnhandledException += ad_UnhandledException;
                AppDomain.CurrentDomain.AssemblyResolve += ad_AssemblyResolve;
            }
#if DEBUG
            WriteLine("Base folder = {0}\n", path);
#endif
            

            try
            {
                CLRMDActivator clrMD = new CLRMDActivator();
                DebugApi.INIT_API(iDebugClient);
                if (clrMD.CreateFromIDebugClient(iDebugClient, out ppTarget) != HRESULTS.S_OK)
                    throw new Exception("Unable to create activator");
            }
            catch
            {
                WriteLine("ERROR: Unable to create type CLRMDActivator. More information below");
                WriteLine("Base Path: {0}", path);
                WriteLine("Private Path: {0}", DllPath);
                ppTarget = null;
            }
        }

        static object _lock = new object();

        static System.Reflection.Assembly ad_AssemblyResolve(object sender, ResolveEventArgs args)
        {

            lock (_lock)
            {
                if (args.Name.ToLower().Contains("icsharpcode.avalonedit"))
                {
                    /*
                    string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                    Debug.WriteLine("CodeBase: {0}", codeBase);
                    UriBuilder uri = new UriBuilder(codeBase);

                    string path = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
                    Debug.WriteLine("Path: {0}", path);

                     */

                    //Debug.WriteLine(Assembly.GetExecutingAssembly().CodeBase);
                    string fileName = args.Name.Split(',')[0];
                    MDTarget.CopyTools();
                    //
                    // Sorry, files were not created
                    //
                    if (MDTarget.ToolsPath == null)
                    {
                        Exports.WriteLine("Unable to expand DLL for source window");
                        return null;
                    }
                    string fullPath = Path.Combine(MDTarget.ToolsPath, fileName + ".dll");
#if DEBUG
                    Exports.WriteLine("Full Path {0}\n", fullPath);
#endif
                    return Assembly.LoadFile(fullPath);
                }
                return null;
                /*
                                if (args.Name.Contains("Microsoft.Diagnostics.Runtime"))
                                {
                                    string assembly = String.Format("{0}{1}.dll", netExtPath, args.Name.Substring(0, args.Name.IndexOf(",")));
                #if DEBUG
                                    WriteLine("Trying to load: {0}", assembly);
                #endif
                                    return System.Reflection.Assembly.LoadFrom(assembly);
                                }
                #if _DEBUG
                                else
                                {

                                    WriteLine("Trying to load: {0}", args.Name);
                                }
                #endif
                            }
            
                            return null;
                 */
            }
        }

        static void ad_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e != null && e.IsTerminating)
            {
                Exports.WriteLine("\nFatal Exception in NetExtShim");
            }
            if(e != null && e.ExceptionObject != null)
            {
                Exports.WriteLine("\nUnhandled exception in NetExtShim: {0}",e.ExceptionObject.GetType().ToString());

                Exception ex = e.ExceptionObject as Exception;
                if (ex != null)
                {
                    Exports.WriteLine("Message : {0}", ex.Message);
                    Exports.WriteLine("at {0}", ex.StackTrace);
                }
            }
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static void Remove()
        {
            callBack = null;
            callBackDml = null;
            
        }

    }

    [ComVisible(true)]
    [Guid("7505BB76-73B1-11E1-BAD9-E6174924019B")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDActivator))]
    public class CLRMDActivator : IMDActivator
    {
        public int CreateFromCrashDump(string crashdump, out IMDTarget ppTarget)
        {
            try
            {
                ppTarget = new MDTarget(crashdump);
            }
            catch
            {
                ppTarget = null;
                return HRESULTS.E_FAIL;
            }
            return HRESULTS.S_OK;
        }

        public int CreateFromIDebugClient(object iDebugClient, out IMDTarget ppTarget)
        {
            try
            {
                ppTarget = new MDTarget(iDebugClient);
            }
            catch
            {
                ppTarget = null;
                return HRESULTS.E_FAIL;
            }
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("408F60EE-B68E-4759-957D-D807068D7AAD")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDInterface))]
    public class MDInterface : IMDInterface
    {
        ClrInterface m_heapint;
        public MDInterface(ClrInterface heapint)
        {
            m_heapint = heapint;
        }

        public int GetName(out string pName)
        {
            if (m_heapint == null)
            {
                pName = null;
                return HRESULTS.E_FAIL;
            }
            pName = m_heapint.Name;
            return HRESULTS.S_OK;
        }

        public int GetBaseInterface(out IMDInterface ppBase)
        {
            if (m_heapint != null && m_heapint.BaseInterface != null)
                ppBase = new MDInterface(m_heapint.BaseInterface);
            else
            {
                ppBase = null;
                return HRESULTS.E_FAIL;
            }
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("6EA64C0B-4F5D-4F12-A2A8-DD5EF334974C")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDHandle))]
    public class MDHandle : IMDHandle
    {
        ClrHandle m_handle;
        public MDHandle(ClrHandle handle)
        {
            m_handle = handle;
        }

        public int GetHandleData(out ulong pAddr, out ulong pObjRef, out MDHandleTypes pType)
        {
            if (m_handle == null)
            {
                pAddr = 0;
                pObjRef = 0;
                pType = new MDHandleTypes();
                return HRESULTS.E_FAIL;
            }
            pAddr = m_handle.Address;
            pObjRef = m_handle.Object;
            pType = (MDHandleTypes)m_handle.HandleType;
            return HRESULTS.S_OK;
        }

        public int IsStrong(out int pStrong)
        {
            if (m_handle == null)
            {
                pStrong = 0;
                return HRESULTS.E_FAIL;
            }
            pStrong = m_handle.IsStrong ? 1 : 0;
            return HRESULTS.S_OK;
        }

        public int GetRefCount(out int pRefCount)
        {
            if (m_handle == null)
            {
                pRefCount = 0;
                return HRESULTS.E_FAIL;
            }
            pRefCount = (int)m_handle.RefCount;
            return HRESULTS.S_OK;
        }

        public int GetDependentTarget(out ulong pTarget)
        {
            if (m_handle == null)
            {
                pTarget = 0;
                return HRESULTS.E_FAIL;
            }
            pTarget = m_handle.DependentTarget;
            return HRESULTS.S_OK;
        }

        public int GetAppDomain(out IMDAppDomain ppDomain)
        {
            if (m_handle != null && m_handle.AppDomain != null)
                ppDomain = new MDAppDomain(m_handle.AppDomain);
            else
            {
                ppDomain = null;
                return HRESULTS.E_FAIL;
            }
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("EB1E21D8-B52A-43D3-8BE5-2C454463BDFD")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDRoot))]
    public class MDRoot : IMDRoot
    {
        ClrRoot m_root;
        public MDRoot(ClrRoot root)
        {
            m_root = root;
        }

        public int GetRootInfo(out ulong pAddress, out ulong pObjRef, out MDRootType pType)
        {
            if (m_root == null)
            {
                pAddress = 0;
                pObjRef = 0;
                pType = MDRootType.MDRoot_AsyncPinning;
                return HRESULTS.E_FAIL;
            }
            pAddress = m_root.Address;
            pObjRef = m_root.Object;
            pType = (MDRootType)m_root.Kind;
            return HRESULTS.S_OK;
        }

        public int GetType(out IMDType ppType)
        {
            ppType = null;
            return HRESULTS.S_FALSE;
        }

        public int GetName(out string ppName)
        {
            if (m_root == null)
            {
                ppName = null;
                return HRESULTS.E_FAIL;
            }
            ppName = m_root.Name;
            return HRESULTS.S_OK;
        }

        public int GetAppDomain(out IMDAppDomain ppDomain)
        {
            if (m_root != null && m_root.AppDomain != null)
                ppDomain = new MDAppDomain(m_root.AppDomain);
            else
            {
                ppDomain = null;
                return HRESULTS.E_FAIL;
            }
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("9A57A447-DCFA-4F35-8599-07AC18DB5CCD")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDAppDomain))]
    public class MDAppDomain : IMDAppDomain
    {
        ClrAppDomain m_appDomain;
        public MDAppDomain(ClrAppDomain ad)
        {
            m_appDomain = ad;
        }

        public int GetName(out string pName)
        {
            if (m_appDomain == null)
            {
                pName = null;
                return HRESULTS.E_FAIL;
            }
            pName = m_appDomain.Name;
            return HRESULTS.S_OK;
        }

        public int GetID(out int pID)
        {
            if (m_appDomain == null)
            {
                pID = -1;
                return HRESULTS.E_FAIL;
            }
            pID = m_appDomain.Id;
            return HRESULTS.S_OK;
        }

        public int GetAddress(out ulong pAddress)
        {
            if (m_appDomain == null)
            {
                pAddress = 0;
                return HRESULTS.E_FAIL;
            }
            pAddress = m_appDomain.Address;
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("D7A8A331-3C6F-4528-AC0A-7EDC367D77B9")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDSegment))]
    public class MDSegment : IMDSegment
    {
        ClrSegment m_seg;
        public MDSegment(ClrSegment seg)
        {
            m_seg = seg;
        }

        public int GetSegData(out MD_SegData SegData)
        {
            if (m_seg == null)
            {
                SegData = new MD_SegData();
                return HRESULTS.E_FAIL;
            }
            SegData.CommittedEnd = m_seg.CommittedEnd;
            SegData.End = m_seg.End;
            SegData.FirstObject = m_seg.FirstObject;
            SegData.Gen0Length = m_seg.Gen0Length;
            SegData.Gen0Start = m_seg.Gen0Start;
            SegData.Gen1Length = m_seg.Gen1Length;
            SegData.Gen1Start = m_seg.Gen1Start;
            SegData.Gen2Length = m_seg.Gen2Length;
            SegData.Gen2Start = m_seg.Gen2Start;
            SegData.IsEphemeral = m_seg.IsEphemeral;
            SegData.IsLarge = m_seg.IsLarge;
            SegData.Length = m_seg.Length;
            SegData.ProcessorAffinity = m_seg.ProcessorAffinity;
            SegData.ReservedEnd = m_seg.ReservedEnd;
            SegData.Start = m_seg.Start;
            return HRESULTS.S_OK;
        }

        public int GetStart(out ulong pAddress)
        {
            if(m_seg == null)
            {
                pAddress = 0;
                return HRESULTS.E_FAIL;
            }
            pAddress = m_seg.Start;
            return HRESULTS.S_OK;
        }

        public int GetEnd(out ulong pAddress)
        {
            if (m_seg == null)
            {
                pAddress = 0;
                return HRESULTS.E_FAIL;
            }
            pAddress = m_seg.End;
            return HRESULTS.S_OK;
        }

        public int GetReserveLimit(out ulong pAddress)
        {
            if (m_seg == null)
            {
                pAddress = 0;
                return HRESULTS.E_FAIL;
            }
            pAddress = m_seg.ReservedEnd;
            return HRESULTS.S_OK;
        }

        public int GetCommitLimit(out ulong pAddress)
        {
            if (m_seg == null)
            {
                pAddress = 0;
                return HRESULTS.E_FAIL;
            }
            pAddress = m_seg.CommittedEnd;
            return HRESULTS.S_OK;
        }

        public int GetLength(out ulong pLength)
        {
            if (m_seg == null)
            {
                pLength = 0;
                return HRESULTS.E_FAIL;
            }
            pLength = m_seg.Length;
            return HRESULTS.S_OK;
        }

        public int GetProcessorAffinity(out int pProcessor)
        {
            if (m_seg == null)
            {
                pProcessor = 0;
                return HRESULTS.E_FAIL;
            }
            pProcessor = m_seg.ProcessorAffinity;
            return HRESULTS.S_OK;
        }

        public int IsLarge(out int pLarge)
        {
            if (m_seg == null)
            {
                pLarge = 0;
                return HRESULTS.E_FAIL;
            }
            pLarge = m_seg.IsLarge ? 1 : 0;
            return HRESULTS.S_OK;
        }

        public int IsEphemeral(out int pEphemeral)
        {
            if (m_seg == null)
            {
                pEphemeral = 0;
                return HRESULTS.E_FAIL;
            }
            pEphemeral = m_seg.IsEphemeral ? 1 : 0;
            return HRESULTS.S_OK;
        }

        public int GetGen0Info(out ulong pStart, out ulong pLen)
        {
            if (m_seg == null)
            {
                pStart = 0;
                pLen = 0;
                return HRESULTS.E_FAIL;
            }
            pStart = m_seg.Gen0Start;
            pLen = m_seg.Gen0Length;
            return HRESULTS.S_OK;
        }

        public int GetGen1Info(out ulong pStart, out ulong pLen)
        {
            if (m_seg == null)
            {
                pStart = 0;
                pLen = 0;
                return HRESULTS.E_FAIL;
            }
            pStart = m_seg.Gen1Start;
            pLen = m_seg.Gen1Length;
            return HRESULTS.S_OK;
        }

        public int GetGen2Info(out ulong pStart, out ulong pLen)
        {
            if (m_seg == null)
            {
                pStart = 0;
                pLen = 0;
                return HRESULTS.E_FAIL;
            }
            pStart = m_seg.Gen2Start;
            pLen = m_seg.Gen2Length;
            return HRESULTS.S_OK;
        }

        public int EnumerateObjects(out IMDObjectEnum ppEnum)
        {
            List<ulong> refs = new List<ulong>();
            if (m_seg == null)
            {
                ppEnum = new MDObjectEnum(refs);
                return HRESULTS.E_FAIL;
            }
            for (ulong obj = m_seg.FirstObject; obj != 0; obj = m_seg.NextObject(obj))
                refs.Add(obj);

            ppEnum = new MDObjectEnum(refs);
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("E5438D76-8565-468C-806F-21701B2E3F04")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDMemoryRegion))]
    public class MDMemoryRegion : IMDMemoryRegion
    {
        ClrMemoryRegion m_region;
        public MDMemoryRegion(ClrMemoryRegion region)
        {
            m_region = region;
        }

        public int GetRegionInfo(out ulong pAddress, out ulong pSize, out MDMemoryRegionType pType)
        {
            if (m_region == null)
            {
                pAddress = 0;
                pSize = 0;
                pType = MDMemoryRegionType.MDRegion_CacheEntryHeap;
                return HRESULTS.E_FAIL;
            }
            pAddress = m_region.Address;
            pSize = m_region.Size;
            pType = (MDMemoryRegionType)m_region.Type;
            return HRESULTS.S_OK;
        }

        public int GetAppDomain(out IMDAppDomain ppDomain)
        {
            if (m_region != null && m_region.AppDomain != null)
                ppDomain = new MDAppDomain(m_region.AppDomain);
            else
            {
                ppDomain = null;
                return HRESULTS.E_FAIL;
            }
            return HRESULTS.S_OK;
        }

        public int GetModule(out string pModule)
        {
            if (m_region == null)
            {
                pModule = "*INVALIDMODULE*";
                return HRESULTS.E_FAIL;
            }
            pModule = m_region.Module;
            return HRESULTS.S_OK;
        }

        public int GetHeapNumber(out int pHeap)
        {
            if (m_region == null)
            {
                pHeap = 0;
                return HRESULTS.E_FAIL;
            }
            pHeap = m_region.HeapNumber;
            return HRESULTS.S_OK;
        }

        public int GetDisplayString(out string pName)
        {
            if (m_region == null)
            {
                pName = null;
                return HRESULTS.E_FAIL;
            }
            pName = m_region.ToString(true);
            return HRESULTS.S_OK;
        }

        public int GetSegmentType(out MDSegmentType pType)
        {
            if (m_region == null)
            {
                pType = new MDSegmentType();
                return HRESULTS.E_FAIL;
            }
            pType = (MDSegmentType)m_region.GCSegmentType;
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("445180E0-D300-4B0E-B815-2F9569074092")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDThread))]
    public class MDThread : IMDThread
    {
        ClrThread m_thread;
        ClrRuntime m_runtime;
        public MDThread(ClrThread thread, ClrRuntime runtime)
        {
            m_thread = thread;
            m_runtime = runtime;
        }

        public int GetThreadData([Out] out MD_ThreadData threadData)
        {
            threadData = new MD_ThreadData();
            if (m_thread == null)
            {
                return HRESULTS.E_FAIL;
            }
            threadData.Address = m_thread.Address;
            threadData.AppDomain = m_thread.AppDomain;
            threadData.CurrentException = m_thread.CurrentException == null ? 0
                : m_thread.CurrentException.Address;
            threadData.GcMode = (int)m_thread.GcMode;
            threadData.IsAborted = m_thread.IsAborted ? 1 : 0;
            threadData.IsAbortRequested = m_thread.IsAbortRequested ? 1 : 0;
            threadData.IsAlive = m_thread.IsAlive ? 1 : 0;
            threadData.IsBackground = m_thread.IsBackground ? 1 : 0;
            threadData.IsCoInitialized = m_thread.IsCoInitialized ? 1 : 0;
            threadData.IsDebuggerHelper = m_thread.IsDebuggerHelper ? 1 : 0;
            threadData.IsDebugSuspended = m_thread.IsDebugSuspended ? 1 : 0;
            threadData.IsFinalizer = m_thread.IsFinalizer ? 1 : 0;
            threadData.IsGC = m_thread.IsGC ? 1 : 0;
            threadData.IsGCSuspendPending = m_thread.IsGCSuspendPending ? 1 : 0;
            threadData.IsMTA = m_thread.IsMTA ? 1 : 0;
            threadData.IsShutdownHelper = m_thread.IsShutdownHelper ? 1 : 0;
            threadData.IsSTA = m_thread.IsSTA ? 1 : 0;
            threadData.IsSuspendingEE = m_thread.IsSuspendingEE ? 1 : 0;
            threadData.IsThreadpoolCompletionPort = m_thread.IsThreadpoolCompletionPort ? 1 : 0;
            threadData.IsThreadpoolGate = m_thread.IsThreadpoolGate ? 1 : 0;
            threadData.IsThreadpoolTimer = m_thread.IsThreadpoolTimer ? 1 : 0;
            threadData.IsThreadpoolWait = m_thread.IsThreadpoolWait ? 1 : 0;
            threadData.IsThreadpoolWorker = m_thread.IsThreadpoolWorker ? 1 : 0;
            threadData.IsUnstarted = m_thread.IsUnstarted ? 1 : 0;
            threadData.IsUserSuspended = m_thread.IsUserSuspended ? 1 : 0;
            threadData.LockCount = m_thread.LockCount;
            threadData.ManagedThreadId = m_thread.ManagedThreadId;
            threadData.OSThreadId = m_thread.OSThreadId;
            threadData.StackBase = m_thread.StackBase;
            threadData.StackLimit = m_thread.StackLimit;
            threadData.Teb = m_thread.Teb;
            threadData.BlockingObjectsCount = 0;
            // Performance Killer - Removed
            // threadData.BlockingObjectsCount = m_thread.BlockingObjects == null ? 0 : m_thread.BlockingObjects.Count;
            AdHoc.GetThreadAllocationLimits(m_runtime, m_thread.Address, out threadData.AllocationStart, out threadData.AllocationLimit);
            return HRESULTS.S_OK;
        }

        public int GetAddress(out ulong pAddress)
        {
            if (m_thread == null)
            {
                pAddress = 0;
                return HRESULTS.E_FAIL;
            }
            pAddress = m_thread.Address;
            return HRESULTS.S_OK;
        }

        public int IsFinalizer(out int pIsFinalizer)
        {
            if (m_thread == null)
            {
                pIsFinalizer = 0;
                return HRESULTS.E_FAIL;
            }
            pIsFinalizer = m_thread.IsFinalizer ? 1 : 0;
            return HRESULTS.S_OK;
        }

        public int IsAlive(out int pIsAlive)
        {
            if (m_thread == null)
            {
                pIsAlive = 0;
                return HRESULTS.E_FAIL;
            }
            pIsAlive = m_thread.IsAlive ? 1 : 0;
            return HRESULTS.S_OK;
        }

        public int GetOSThreadId(out int pOSThreadId)
        {
            if (m_thread == null)
            {
                pOSThreadId = 0;
                return HRESULTS.E_FAIL;
            }
            pOSThreadId = (int)m_thread.OSThreadId;
            return HRESULTS.S_OK;
        }

        public int GetAppDomainAddress(out ulong pAppDomain)
        {
            if (m_thread == null)
            {
                pAppDomain = 0;
                return HRESULTS.E_FAIL;
            }
            pAppDomain = m_thread.AppDomain;
            return HRESULTS.S_OK;
        }

        public int GetLockCount(out int pLockCount)
        {
            if (m_thread == null)
            {
                pLockCount = 0;
                return HRESULTS.E_FAIL;
            }
            pLockCount = (int)m_thread.LockCount;
            return HRESULTS.S_OK;
        }

        public int GetCurrentException(out IMDException ppException)
        {
            if (m_thread != null && m_thread.CurrentException != null)
                ppException = new MDException(m_thread.CurrentException);
            else
                ppException = null;
            return HRESULTS.S_OK;
        }

        public int GetTebAddress(out ulong pTeb)
        {
            if (m_thread == null)
            {
                pTeb = 0;
                return HRESULTS.E_FAIL;
            }
            pTeb = m_thread.Teb;
            return HRESULTS.S_OK;
        }

        public int GetStackLimits(out ulong pBase, out ulong pLimit)
        {                
            pBase = 0;
            pLimit = 0;
            if (m_thread == null)
            {
                return HRESULTS.E_FAIL;
            }
            try
            {
                pBase = m_thread.StackBase;
                pLimit = m_thread.StackLimit;
            }
            catch
            {
                return HRESULTS.E_FAIL;
            }
            return HRESULTS.S_OK;
        }

        public int EnumerateStackTrace(out IMDStackTraceEnum ppEnum)
        {
            ppEnum = null;
            if(m_thread == null)
                return HRESULTS.E_FAIL;
            ppEnum = new MDStackTraceEnum(m_thread.StackTrace);
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("305893A9-C8CC-4B3C-B904-38D0071BFC58")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDRCW))]
    public class MDRCW : IMDRCW
    {
        RcwData m_rcw;
        public MDRCW(RcwData rcw)
        {
            m_rcw = rcw;
        }

        public int GetIUnknown(out ulong pIUnk)
        {
            pIUnk = 0;
            if(m_rcw == null)
                return HRESULTS.E_FAIL;
            pIUnk = m_rcw.IUnknown;
            return HRESULTS.S_OK;
        }

        public int GetObject(out ulong pObject)
        {
            pObject = 0;
            if(m_rcw == null)
                return HRESULTS.E_FAIL;
            pObject = m_rcw.Object;
            return HRESULTS.S_OK;
        }

        public int GetRefCount(out int pRefCnt)
        {
            pRefCnt = 0;

            pRefCnt = m_rcw.RefCount;
            return HRESULTS.S_OK;
        }

        public int GetVTable(out ulong pHandle)
        {
            pHandle = 0;

            pHandle = m_rcw.VTablePointer;
            return HRESULTS.S_OK;
        }

        public int IsDisconnected(out int pDisconnected)
        {
            pDisconnected = 0;

            pDisconnected = m_rcw.Disconnected ? 1 : 0;
            return HRESULTS.S_OK;
        }

        public int EnumerateInterfaces(out IMDCOMInterfaceEnum ppEnum)
        {
            ppEnum = null;
            if(m_rcw == null)
                return HRESULTS.E_FAIL;

            ppEnum = new MDCOMInterfaceEnum(m_rcw.Interfaces);
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("90A71CA2-D018-4752-8B19-CB0F3EA95ECB")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDCCW))]
    public class MDCCW : IMDCCW
    {
        CcwData m_ccw;
        public MDCCW(CcwData ccw)
        {
            m_ccw = ccw;
        }

        public int GetIUnknown(out ulong pIUnk)
        {
            pIUnk = m_ccw.IUnknown;
            return HRESULTS.S_OK;
        }

        public int GetObject(out ulong pObject)
        {
            pObject = m_ccw.Object;
            return HRESULTS.S_OK;
        }

        public int GetHandle(out ulong pHandle)
        {
            pHandle = m_ccw.Handle;
            return HRESULTS.S_OK;
        }

        public int GetRefCount(out int pRefCnt)
        {
            pRefCnt = m_ccw.RefCount;
            return HRESULTS.S_OK;
        }

        public int EnumerateInterfaces(out IMDCOMInterfaceEnum ppEnum)
        {
            ppEnum = new MDCOMInterfaceEnum(m_ccw.Interfaces);
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("312D2F5F-2736-4E96-BA64-2B6766F9C1AA")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDStaticField))]
    public class MDStaticField : IMDStaticField
    {
        ClrStaticField m_field;
        public MDStaticField(ClrStaticField field)
        {
            m_field = field;
        }

        public int GetName(out string pName)
        {
            pName = null;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            pName = m_field.Name;
            return HRESULTS.S_OK;
        }

        public int GetType(out IMDType ppType)
        {
            ppType = null;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            ppType = MDType.Construct(m_field.Type);
            return HRESULTS.S_OK;
        }

        public int GetElementType(out int pCET)
        {
            pCET = 0;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            pCET = (int)m_field.ElementType;
            return HRESULTS.S_OK;
        }

        public int GetSize(out int pSize)
        {
            pSize = 0;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            
            pSize = m_field.Size;
            return HRESULTS.S_OK;
        }

        public int GetFieldValue(IMDAppDomain appDomain, out IMDValue ppValue)
        {
            appDomain = null;
            ppValue = null;

            if (m_field == null)
                return HRESULTS.S_OK;

            object value = m_field.GetValue((ClrAppDomain)appDomain);
            ppValue = new MDValue(value, m_field.ElementType);
            return HRESULTS.S_OK;
        }

        public int GetFieldAddress(IMDAppDomain appDomain, out ulong pAddress)
        {
            appDomain = null;
            pAddress = 0;
            if(m_field == null)
                return HRESULTS.E_FAIL;

            ulong addr = m_field.GetAddress((ClrAppDomain)appDomain);
            pAddress = addr;
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("F51DBA0D-AD2A-408F-875D-009AB7D4002C")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDThreadStaticField))]
    public class MDThreadStaticField : IMDThreadStaticField
    {
        ClrThreadStaticField m_field;
        public MDThreadStaticField(ClrThreadStaticField field)
        {
            m_field = field;
        }

        public int GetName(out string pName)
        {
            pName = null;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            pName = m_field.Name;
            return HRESULTS.S_OK;
        }

        public int GetType(out IMDType ppType)
        {
            ppType = MDType.Construct(m_field.Type);
            if (m_field == null)
                return HRESULTS.E_FAIL;
            return HRESULTS.S_OK;
        }

        public int GetElementType(out int pCET)
        {
            pCET = (int)m_field.ElementType;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            return HRESULTS.S_OK;
        }

        public int GetSize(out int pSize)
        {
            pSize = 0;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            pSize = m_field.Size;
            return HRESULTS.S_OK;
        }

        public int GetFieldValue(IMDAppDomain appDomain, IMDThread thread, out IMDValue ppValue)
        {
            ppValue = null;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            object value = m_field.GetValue((ClrAppDomain)appDomain, (ClrThread)thread);
            ppValue = new MDValue(value, m_field.ElementType);
            return HRESULTS.S_OK;
        }

        public int GetFieldAddress(IMDAppDomain appDomain, IMDThread thread, out ulong pAddress)
        {
            pAddress = 0;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            pAddress = m_field.GetAddress((ClrAppDomain)appDomain, (ClrThread)thread);
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("47AA4220-77A7-4735-A651-B3A6D887369A")]    
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDField))]
    public class MDField : IMDField
    {
        ClrInstanceField m_field;
        public MDField(ClrInstanceField field)
        {
            m_field = field;
        }

        public int GetName(out string pName)
        {
            pName = null;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            pName = m_field.Name;
            return HRESULTS.S_OK;
        }

        public int GetType(out IMDType ppType)
        {
            ppType = null;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            ppType = MDType.Construct(m_field.Type);
            return HRESULTS.S_OK;
        }

        public int GetElementType(out int pCET)
        {
            pCET = 0;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            pCET = (int)m_field.ElementType;
            return HRESULTS.S_OK;
        }

        public int GetSize(out int pSize)
        {
            pSize = 0;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            pSize = m_field.Size;
            return HRESULTS.S_OK;
        }

        public int GetOffset(out int pOffset)
        {
            pOffset = 0;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            pOffset = m_field.Offset;
            return HRESULTS.S_OK;
        }

        public int GetFieldValue(ulong objRef, int interior, out IMDValue ppValue)
        {
            ppValue = null;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            object value = m_field.GetValue(objRef, interior != 0);
            ppValue = new MDValue(value, m_field.ElementType);
            return HRESULTS.S_OK;
        }

        public int GetFieldAddress(ulong objRef, int interior, out ulong pAddress)
        {
            pAddress = 0;
            if (m_field == null)
                return HRESULTS.E_FAIL;
            pAddress = m_field.GetAddress(objRef, interior != 0);
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("E7796988-D9B7-41B3-B43C-1B85A0955DD0")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDType))]
    public class MDType : IMDType
    {
        public static IMDType Construct(ClrType type)
        {
            if (type == null)
                return null;

            return new MDType(type);
        }

        ClrType m_type;
        internal bool IsValid { get; set; }
        public MDType(ClrType type)
        {
            m_type = type;

            
            if (type == null)
                IsValid = false;
             
        }

        public int GetIMetadata([Out] [MarshalAs((UnmanagedType)25)] out object IMetadata)
        {
            IMetadata = m_type.Module.MetadataImport;
            return HRESULTS.S_OK;
        }


        public int GetHeader(ulong objRef, out MD_TypeData typeData)
        {
            typeData = new MD_TypeData();
            if (m_type == null)
                return HRESULTS.E_FAIL;
            if (m_type.BaseType != null)
                typeData.parentMT = HeapStatItem.GetMTOfType(m_type.BaseType);
            else
                typeData.parentMT = 0;
            typeData.BaseSize = (ulong)m_type.BaseSize;
            if (objRef != 0)
            {
                typeData.size = m_type.GetSize(objRef);
                if(!m_type.IsObjectReference || !m_type.Heap.Runtime.ReadPointer(objRef, out typeData.MethodTable))
                    typeData.MethodTable = HeapStatItem.GetMTOfType(m_type);
                var seg = m_type.Heap.GetSegmentByAddress(objRef);
                if (seg != null)
                {
                    typeData.heap = seg.ProcessorAffinity;
                    typeData.generation = seg.GetGeneration(objRef);
                }
                else
                {
                    typeData.heap = 0;
                    typeData.generation = 0;
                }
                typeData.isCCW = m_type.IsCCW(objRef);
                typeData.isRCW = m_type.IsRCW(objRef);
                typeData.appDomain = AdHoc.GetDomainFromMT(m_type.Heap.Runtime, typeData.MethodTable);
            }
            else
            {
                typeData.MethodTable = HeapStatItem.GetMTOfType(m_type);
            }
            typeData.isFree = m_type.IsFree;
            typeData.instanceFieldsCount = m_type.Fields.Count;
            typeData.staticFieldsCount = m_type.StaticFields.Count
                + m_type.ThreadStaticFields.Count;
            typeData.isRuntimeType = m_type.IsRuntimeType;

            if (m_type.Module != null)
            {
                typeData.module = m_type.Module.ImageBase;
                typeData.assembly = m_type.Module.AssemblyId;
                typeData.debuggingFlag = (int)m_type.Module.DebuggingMode;
            }
            else
            {
                typeData.module = 0;
                typeData.assembly = 0;
                typeData.debuggingFlag = 0;
            }
            typeData.token = m_type.MetadataToken;
            if (!m_type.IsArray)
            {
                typeData.rank = 0;
                typeData.arraySize = 0;

            }
            else
            {
                string[] parts = m_type.Name.Split('[');
                if (parts.Length > 1)
                    typeData.rank = parts[1].Split(',').Length;
                else
                    typeData.rank = 0; // it should never get here
                typeData.elementSize = m_type.ElementSize;

                if (objRef != 0)
                {
                    
                    ulong[] arrayData = AdHoc.GetArrayData(m_type.Heap.Runtime, objRef);
                    if (m_type.ComponentType == null)
                        typeData.arrayCorType = (int)arrayData[AdHoc.ARRAYCORTYPE];
                    else
                        typeData.arrayCorType = (int)m_type.ComponentType.ElementType;
                    typeData.arrayElementMT = arrayData[AdHoc.ARRAYELEMENTMT];
                    typeData.arrayStart = arrayData[AdHoc.ARRAYSTART];

                    typeData.arraySize = m_type.GetArrayLength(objRef);
                }
            }
            typeData.corElementType = (int)m_type.ElementType;
            typeData.isArray = m_type.IsArray;
            typeData.isGeneric = String.IsNullOrEmpty(m_type.Name) ? false : m_type.Name.Contains('<') && m_type.Name.Contains('>') && m_type.Name[0] != '<';
            typeData.isString = m_type.IsString;
            typeData.EEClass = AdHoc.GetEEFromMT(m_type.Heap.Runtime, typeData.MethodTable);
            typeData.isValueType = m_type.IsValueClass;
            typeData.module = m_type.Module.ImageBase;
            typeData.assembly = m_type.Module.AssemblyId;
            typeData.containPointers = m_type.ContainsPointers;
            typeData.MethodTable &= ~(ulong)3;
            typeData.parentMT &= ~(ulong)3;

            return HRESULTS.S_OK;
        }

        public int GetString(ulong ObjRef, [Out] [MarshalAs((UnmanagedType)19)] out string strValue)
        {
            if(ObjRef == 0)
            {
                strValue = null;
                return HRESULTS.E_FAIL;
            }
            
            ClrType strType = m_type.Heap.GetObjectType(ObjRef);
            
            
            if (strType != null && strType.IsString)
                strValue = strType.GetValue(ObjRef) as String;
            else
                strValue = null;
            return HRESULTS.S_OK;
        }

        public int GetFilename([Out] [MarshalAs((UnmanagedType)19)] out string fileName)
        {
            if (!m_type.Module.IsFile)
            {
                fileName = "(dynamic)";
                return HRESULTS.S_OK;
            }
            fileName = m_type.Module.FileName;
            return HRESULTS.S_OK;
        }
    

        public int GetName(out string pName)
        {
            pName = null;
            if(m_type == null)
                return HRESULTS.E_FAIL;
            pName = m_type.Name == null ? "<UNKNOWN>"+m_type.MethodTable.ToString("x16") : m_type.Name.Replace('+','_');
            return HRESULTS.S_OK;
        }

        public int GetRuntimeName(ulong objRef, out string pName)
        {
            if (m_type == null || !m_type.IsRuntimeType)
            {
                pName = null;
                return HRESULTS.E_FAIL;
            }
            ClrType runtimeType = m_type.GetRuntimeType(objRef);
            if (runtimeType != null)
            {
                pName = runtimeType.Name == null ? "<UNKNOWN>" + runtimeType.MethodTable.ToString("x16") : runtimeType.Name.Replace('+', '_');
                return HRESULTS.S_OK;
            }

            pName = null;
            return HRESULTS.E_FAIL;
        }

        public int GetSize(ulong objRef, out ulong pSize)
        {
            pSize = 0;
            if (m_type == null)
                return HRESULTS.S_OK;
            pSize = m_type.GetSize(objRef);
            return HRESULTS.S_OK;
        }

        public int ContainsPointers(out int pContainsPointers)
        {
            pContainsPointers = m_type.ContainsPointers ? 1 : 0;
            return HRESULTS.S_OK;
        }

        public int GetCorElementType(out int pCET)
        {
            pCET = (int)m_type.ElementType;
            return HRESULTS.S_OK;
        }

        public int GetBaseType(out IMDType ppBaseType)
        {
            ppBaseType = Construct(m_type.BaseType);
            return HRESULTS.S_OK;
        }

        public int GetArrayComponentType(out IMDType ppArrayComponentType)
        {
            ppArrayComponentType = Construct(m_type.ComponentType);
            return HRESULTS.S_OK;
        }

        public int GetCCW(ulong addr, out IMDCCW ppCCW)
        {
            if (m_type != null && m_type.IsCCW(addr))
                ppCCW = new MDCCW(m_type.GetCCWData(addr));
            else
            {
                ppCCW = null;
                return HRESULTS.E_FAIL;
            }
            return HRESULTS.S_OK;
        }

        public int GetRCW(ulong addr, out IMDRCW ppRCW)
        {
            if (m_type != null && m_type.IsRCW(addr))
                ppRCW = new MDRCW(m_type.GetRCWData(addr));
            else
            {
                ppRCW = null;
                return HRESULTS.E_FAIL;
            }
            return HRESULTS.S_OK;
        }

        public int IsArray(out int pIsArray)
        {
            pIsArray = m_type.IsArray ? 1 : 0;
            return HRESULTS.S_OK;
        }

        public int IsFree(out int pIsFree)
        {
            pIsFree = m_type.IsFree ? 1 : 0;
            return HRESULTS.S_OK;
        }

        public int IsException(out int pIsException)
        {
            pIsException = m_type.IsException ? 1 : 0;
            return HRESULTS.S_OK;
        }

        public int IsEnum(out int pIsEnum)
        {
            pIsEnum = m_type.IsEnum ? 1 : 0;
            return HRESULTS.S_OK;
        }

        public int GetEnumElementType(out int pValue)
        {
            pValue = (int)m_type.GetEnumElementType();
            return HRESULTS.S_OK;
        }

        public int GetEnumNames(out IMDStringEnum ppEnum)
        {
            ppEnum = new MDStringEnum(m_type.GetEnumNames().ToArray());
            return HRESULTS.S_OK;
        }

        public int GetEnumValueInt32(string name, out int pValue)
        {
            pValue = 0;
            if (m_type == null || !m_type.TryGetEnumValue(name, out pValue))
                return HRESULTS.E_FAIL;
            return HRESULTS.S_OK;
        }

        public int GetAllFieldsDataRawCount(out int pCount)
        {
            pCount = m_type.Fields.Count + m_type.StaticFields.Count
                + m_type.ThreadStaticFields.Count;
            return HRESULTS.S_OK;
        }

        public int GetAllFieldsDataRaw(int valueType, int count, MD_FieldData[] fields, out int pNeeded)
        {
            int total =  m_type.Fields.Count + m_type.StaticFields.Count
                + m_type.ThreadStaticFields.Count;
            if (fields == null || count == 0 || count < total)
            {
                pNeeded = total;
                return HRESULTS.S_FALSE;
            }

            int i=0;
            foreach (var field in CacheFieldInfo.Fields(m_type))
            {
                fields[i].corElementType = field.CorElementType;
                fields[i].index = field.Index;
                fields[i].isArray = field.IsArray;
                fields[i].isGeneric = field.IsGeneric;
                fields[i].isStatic = field.IsStatic;
                fields[i].isThreadStatic = field.IsThreadStatic;
                fields[i].isValueType = field.IsValueType;
                fields[i].MethodTable = field.MethodTable;
                fields[i].size = field.Size;
                fields[i].token = field.Token;
                fields[i].offset = field.Offset;
                fields[i].module = field.Module;
                fields[i].value = 0;
                fields[i].isEnum = field.IsEnum;
                fields[i].isString = field.IsString;
                fields[i].generalIndex = i;
                i++;
            }

            pNeeded = total;

            return HRESULTS.S_OK;
        
        }

        public int GetFieldCount(out int pCount)
        {
            pCount = m_type.Fields.Count;
            return HRESULTS.S_OK;
        }

        public int GetField(int index, out IMDField ppField)
        {
            ppField = new MDField(m_type.Fields[index]);
            return HRESULTS.S_OK;
        }

        public int GetRawFieldAddress(ulong obj, int interior, int index, [Out] out ulong address)
        {
            address = 0;
            if (m_type == null)
                return HRESULTS.S_FALSE;
            var fields = CacheFieldInfo.Fields(m_type);
            
            if (index < fields.Count)
            {
                if (!fields[index].IsStatic && !fields[index].IsThreadStatic)
                {
                    ClrInstanceField instField = fields[index].BackField as ClrInstanceField;
                    if (instField != null)
                    {
                        address = instField.GetAddress(obj, interior != 0);
                        return HRESULTS.S_OK;
                    }
                    return HRESULTS.S_FALSE;

                }
                if (fields[index].IsThreadStatic)
                {
                    ClrThreadStaticField threadStat = fields[index].BackField as ClrThreadStaticField;
                    if (threadStat != null)
                    {
                        foreach (var thread in m_type.Heap.Runtime.Threads)
                        {
                            foreach (var domain in AdHoc.GetDomains(m_type.Heap.Runtime))
                            {
                                try
                                {
                                    address = threadStat.GetAddress(domain, thread);
                                    if (address != 0)
                                    {
                                        return HRESULTS.S_OK;
                                    }
                                }
                                catch
                                {
                                    // this may be expected
                                }
                            }
                        }
                    }
                    return HRESULTS.S_FALSE;
                }

                if (fields[index].IsStatic)
                {
                    ClrStaticField stat = fields[index].BackField as ClrStaticField;
                    if (stat != null)
                    {
                        ulong mt=0;
                        ClrAppDomain domain = null;
                        if (m_type.IsObjectReference)
                            m_type.Heap.ReadPointer(obj, out mt);
                        if(mt==0)
                            mt = HeapStatItem.GetMTOfType(m_type);
                        ulong domainAddr = AdHoc.GetDomainFromMT(m_type.Heap.Runtime, mt);
                        if (domainAddr != 0)
                        {
                            domain = AdHoc.GetDomainByAddress(m_type.Heap.Runtime, domainAddr);
                            if (domain != null)
                            {
                                address = stat.GetAddress(domain);
                                if (address != 0)
                                    return HRESULTS.S_OK;
                            }
                            foreach(var d in AdHoc.GetDomains(m_type.Heap.Runtime))
                            {
                                address = stat.GetAddress(d);
                                if (address != 0)
                                {
                                    //Exports.WriteLine("\nFrom Managed: {0:x16}={1} \n\n", address,
                                    //    stat.GetValue(d));
                                    if(stat.GetValue(d) != null)
                                        return HRESULTS.S_OK;
                                }
                            }
                        }

                    }


                    return HRESULTS.S_FALSE;
                }

                return HRESULTS.S_FALSE;
            }



            address = 0;
            return HRESULTS.S_FALSE;
        }

        public int GetRawFieldTypeAndName(int index, out string pType, out string pName)
        {
            pType = null;
            pName = null;
            if(m_type == null)
                return HRESULTS.E_FAIL;

            var fields = CacheFieldInfo.Fields(m_type);

            if (index < fields.Count)
            {
                pType = fields[index].TypeName;
                pName = fields[index].Name;
                return HRESULTS.S_OK;
            }

            return HRESULTS.E_FAIL;
        }

        public int GetEnumName(ulong value, out string enumString)
        {
            enumString = AdHoc.GetEnumName(m_type, value);
            return HRESULTS.S_OK;
        }

        public int GetFieldData(ulong obj, int interior, int count, MD_FieldData[] fields, out int pNeeded)
        {
            int total = m_type.Fields.Count;
            if (fields == null || count == 0)
            {
                pNeeded = total;
                return 1; // S_FALSE
            }

            for (int i = 0; i < count && i < total; ++i)
            {
                var field = m_type.Fields[i];
                //fields[i].name = field.Name;
                //fields[i].type = field.Type.Name;
                fields[i].offset = field.Offset;
                fields[i].size = field.Size;
                fields[i].corElementType = (int)field.ElementType;

                if (field.ElementType == ClrElementType.Struct ||
                    field.ElementType == ClrElementType.String ||
                    field.ElementType == ClrElementType.Float ||
                    field.ElementType == ClrElementType.Double)
                {
                    fields[i].value = field.GetAddress(obj, interior != 0);
                }
                else
                {
                    object value = field.GetValue(obj, interior != 0);

                    if (value == null)
                    {
                        fields[i].value = 0;
                    }
                    else
                    {
                        if (value is int)
                            fields[i].value = (ulong)(int)value;
                        else if (value is uint)
                            fields[i].value = (uint)value;
                        else if (value is long)
                            fields[i].value = (ulong)(long)value;
                        else if (value is ulong)
                            fields[i].value = (ulong)value;
                        else if (value is byte)
                            fields[i].value = (ulong)(byte)value;
                        else if (value is sbyte)
                            fields[i].value = (ulong)(sbyte)value;
                        else if (value is ushort)
                            fields[i].value = (ulong)(ushort)value;
                        else if (value is short)
                            fields[i].value = (ulong)(short)value;
                        else if (value is bool)
                            fields[i].value = ((bool)value) ? (ulong)1 : (ulong)0;

                    }
                }
            }

            if (count < total)
            {
                pNeeded = count;
                return 1; // S_FALSE
            }

            pNeeded = total;
            return 0; // S_OK;
        }

        public int GetStaticFieldCount(out int pCount)
        {
            pCount = m_type.StaticFields.Count;
            return HRESULTS.S_OK;
        }

        public int GetStaticField(int index, out IMDStaticField ppStaticField)
        {
            ppStaticField = new MDStaticField(m_type.StaticFields[index]);
            return HRESULTS.S_OK;
        }

        public int GetThreadStaticFieldCount(out int pCount)
        {
            pCount = m_type.ThreadStaticFields.Count;
            return HRESULTS.S_OK;
        }

        public int GetThreadStaticField(int index, out IMDThreadStaticField ppThreadStaticField)
        {
            ppThreadStaticField = new MDThreadStaticField(m_type.ThreadStaticFields[index]);
            return HRESULTS.S_OK;
        }

        public int GetArrayLength(ulong objRef, out int pLength)
        {
            pLength = m_type.GetArrayLength(objRef);
            return HRESULTS.S_OK;
        }

        public int GetArrayElementAddress(ulong objRef, int index, out ulong pAddr)
        {
            pAddr = m_type.GetArrayElementAddress(objRef, index);
            return HRESULTS.S_OK;
        }

        public int GetArrayElementValue(ulong objRef, int index, out IMDValue ppValue)
        {
            object value = m_type.GetArrayElementValue(objRef, index);
            ClrElementType elementType = m_type.ComponentType != null ? m_type.ComponentType.ElementType : ClrElementType.Unknown;
            ppValue = new MDValue(value, elementType);
            return HRESULTS.S_OK;
        }


        public int EnumerateReferences(ulong objRef, out IMDReferenceEnum ppEnum)
        {
            List<MD_Reference> refs = new List<MD_Reference>();
            m_type.EnumerateRefsOfObject(objRef, delegate(ulong child, int offset)
            {
                if (child != 0)
                {
                    MD_Reference r = new MD_Reference();
                    r.address = child;
                    r.offset = offset;
                    refs.Add(r);
                }
            });


            ppEnum = new ReferenceEnum(refs);
            return HRESULTS.S_OK;
        }

        public int EnumerateInterfaces(out IMDInterfaceEnum ppEnum)
        {
            ppEnum = new InterfaceEnum(m_type.Interfaces);
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDException))]
    public class MDException : IMDException
    {
        private ClrException m_ex;

        public MDException(ClrException ex)
        {
            m_ex = ex;
        }

        int IMDException.GetGCHeapType(out IMDType ppType)
        {
            ppType = null;

            if(m_ex == null)
                return HRESULTS.E_FAIL;
            ppType = new MDType(m_ex.Type);
            return HRESULTS.S_OK;
            
        }

        int IMDException.GetErrorMessage(out string pMessage)
        {
            pMessage = null;
            if (m_ex == null)
                return HRESULTS.E_FAIL;
            pMessage = m_ex.Message;
            return HRESULTS.S_OK;
        }

        int IMDException.GetObjectAddress(out ulong pAddress)
        {
            pAddress = 0;
            if (m_ex == null)
                return HRESULTS.E_FAIL;
            pAddress = m_ex.Address;
            return HRESULTS.S_OK;
        }

        int IMDException.GetInnerException(out IMDException ppException)
        {
            ppException = null;
            if (m_ex == null)
                return HRESULTS.E_FAIL;
            if (m_ex.Inner != null)
                ppException = new MDException(m_ex.Inner);
            else
                ppException = null;
            return HRESULTS.S_OK;
        }

        int IMDException.GetHRESULT(out int pHResult)
        {
            pHResult = HRESULTS.S_FALSE;
            if (m_ex == null)
                return HRESULTS.E_FAIL;
            pHResult = m_ex.HResult;
            return HRESULTS.S_OK;
        }

        int IMDException.EnumerateStackFrames(out IMDStackTraceEnum ppEnum)
        {
            ppEnum = null;
            if (m_ex == null)
                return HRESULTS.E_FAIL;
            ppEnum = new MDStackTraceEnum(m_ex.StackTrace);
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("23BB665A-34A9-4A5A-ACE5-982D45166BB7")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDHeap))]
    public class MDHeap : IMDHeap
    {
        ClrHeap m_heap;
        public MDHeap(ClrHeap heap)
        {
            m_heap = heap;
        }

        public int GetObjectType(ulong addr, out IMDType ppType)
        {
            ClrType tp = m_heap.GetObjectType(addr);
            if (tp == null)
            {
                ppType = null;
                return HRESULTS.E_FAIL;
            }
            ppType = new MDType(tp);
            return HRESULTS.S_OK;
            
        }

        public int GetExceptionObject(ulong addr, out IMDException ppExcep)
        {
            
            ppExcep = new MDException(m_heap.GetExceptionObject(addr));
            return HRESULTS.S_OK;
        }

        public int EnumerateRoots(out IMDRootEnum ppEnum)
        {
            ppEnum = new MDRootEnum(new List<ClrRoot>(m_heap.EnumerateRoots()));
            return HRESULTS.S_OK;
        }

        public int EnumerateSegments(out IMDSegmentEnum ppEnum)
        {
            ppEnum = new MDSegmentEnum(m_heap.Segments);
            return HRESULTS.S_OK;
        }

        public static string DumpStack(IList<ClrStackFrame> Stack, int WordSize, bool SkipAddress = false)
        {
            if (Stack == null || Stack.Count == 0)
            {
                return "(no managed stack found)\n";
            }
            StringBuilder sb = new StringBuilder();
            if (WordSize == 8)
                if (!SkipAddress)
                    sb.Append("SP               IP               Function\n");
                else
                    sb.Append("IP               Function\n");

            else
                if (SkipAddress)
                    sb.Append("IP       Function\n");
                else
                    sb.Append("SP       IP       Function\n");
            foreach (var frame in Stack)
            {
                if (!SkipAddress)
                    sb.AppendFormat(Exports.pointerFormat("{0:%p} "), frame.StackPointer);
                sb.AppendFormat(Exports.pointerFormat("{0:%p} "), frame.InstructionPointer);
                sb.Append(frame.DisplayString);
                sb.Append("\n");
            }
            return sb.ToString();

        }

        public int DumpException(ulong ObjRef)
        {
            var exception = m_heap.GetExceptionObject(ObjRef);
            if (exception == null)
            {
                Exports.WriteLine("No expeception found at {0:%p}", ObjRef);
                Exports.WriteLine("");
                return HRESULTS.S_OK;
            }
            Exports.WriteLine("Address: {0:%p}", ObjRef);
            Exports.WriteLine("Exception Type: {0}", exception.Type.Name);
            Exports.WriteLine("Message: {0}", exception.Message);
            Exports.Write("Inner Exception: ");
            if (exception.Inner == null)
                Exports.WriteLine("(none)");
            else
                Exports.WriteDmlLine("<link cmd=\"!wpe {0:%p}\">{0:%p}</link> {1} {2}</link>", exception.Inner.Address,
                    exception.Inner.Type.Name.Replace("<", "&lt;").Replace(">", "&gt;"), exception.Inner.Message);
            Exports.WriteLine("Stack:");
            Exports.WriteLine("{0}", DumpStack(exception.StackTrace, m_heap.Runtime.PointerSize));
            Exports.WriteLine("HResult: {0:x4}", exception.HResult);
            Exports.WriteLine("");
            return HRESULTS.S_OK;
        }


        public int DumpAllExceptions(IMDObjectEnum Exceptions)
        {
            Dictionary<string, List<ulong>> allExceptions = new Dictionary<string, List<ulong>>();
            foreach (var obj in ((MDObjectEnum)Exceptions).List)
            {
                ClrException ex = m_heap.GetExceptionObject(obj);
                if (ex != null)
                {
                    if (Exports.isInterrupted())
                        return HRESULTS.E_FAIL;
                    string key = String.Format("{0}\0{1}\0{2}", ex.Type.Name, ex.Message, DumpStack(ex.StackTrace, m_heap.Runtime.PointerSize, true));
                    if (!allExceptions.ContainsKey(key))
                    {
                        allExceptions[key] = new List<ulong>();
                    }
                    allExceptions[key].Add(obj);
                }

            }

            int exCount = 0;
            int typeCount = 0;
            foreach (var key in allExceptions.Keys)
            {
                typeCount++;
                exCount += allExceptions[key].Count;
                Exports.Write("{0,8:#,#} of Type: {1}", allExceptions[key].Count, key.Split('\0')[0]);
                for (int i = 0; i < Math.Min(3, allExceptions[key].Count); i++)
                {
                    Exports.WriteDml(" <link cmd=\"!wpe {0:%p}\">{0:%p}</link>", (allExceptions[key])[i]);
                }
                ClrException ex = m_heap.GetExceptionObject((allExceptions[key])[0]);
                Exports.WriteLine("");
                Exports.WriteLine("Message: {0}", key.Split('\0')[1]);
                Exports.WriteLine("Inner Exception: {0}", ex.Inner == null ? "(none)" : ex.Inner.Type.Name);
                Exports.WriteLine("Stack:");
                Exports.WriteLine("{0}", key.Split('\0')[2]);
                Exports.WriteLine("");

            }
            Exports.WriteLine("{0:#,#} Exceptions in {1:#,#} unique type/stack combinations (duplicate types in similar stacks may be rethrows)", exCount, typeCount);
            Exports.WriteLine("");
            return HRESULTS.S_OK;
        }

        public int DumpXml(ulong ObjRef)
        {
            InitializeCache();
            DumpXmlDoc(ObjRef, true);
            return HRESULTS.S_OK;
        }
        public int GetXmlString(ulong ObjRef, out string XmlString)
        {
            InitializeCache();
            StringBuilder sb = null;
            sb = DumpXmlDoc(ObjRef);
            if (sb == null)
            {
                XmlString = null;
                return HRESULTS.E_FAIL;
            }
            else
            {
                XmlString = sb.ToString();
            }

            return HRESULTS.S_OK;
        }

        private static HeapCache cache = null;
        public void InitializeCache()
        {
            cache = new HeapCache(m_heap.Runtime);
        }
        public StringBuilder PrintAttribute(ulong Address)
        {
            StringBuilder sb = new StringBuilder(100);
            if (Address == 0)
                return sb;
            sb.Append(" ");
            dynamic attr = cache.GetDinamicFromAddress(Address);
            sb.Append(String.IsNullOrEmpty((string)(attr.name.prefix)) ? "" : (string)(attr.name.prefix) + ":");
            sb.Append(String.IsNullOrEmpty((string)(attr.name.localName)) ? "" : (string)(attr.name.localName));
            //sb.Append(String.IsNullOrEmpty((string)(attr.name.ns)) ? "" : "=\"" + (string)(attr.name.ns) + "\"");
            sb.Append(String.IsNullOrEmpty((string)(attr.lastChild.data)) ? "" : "=\"" + System.Security.SecurityElement.Escape((string)(attr.lastChild.data)) + "\"");
            return sb;
        }

        public StringBuilder PrintXmlNode(ulong Address, int Level)
        {
            StringBuilder sb = new StringBuilder(100);
            if (Address == 0)
                return sb;
            ClrType node = m_heap.GetObjectType(Address);

            var nodeObj = cache.GetDinamicFromAddress(Address);
            if (nodeObj == null)
                return sb;
            sb.Append(' ', Level);
            if (node.Name == "System.Xml.XmlDeclaration")
            {
                sb.Append("<?xml version=\"");
                try
                {
                    sb.Append((string)(nodeObj.version));
                }
                catch
                {
                    sb.Append("1.0");
                }
                sb.Append("\" encoding=\"");
                try
                {
                    sb.Append((string)(nodeObj.encoding));
                }
                catch
                {
                    sb.Append("utf-8");
                }
                sb.Append("\" ?>");
                return sb;
            }
            if (node.Name == "System.Xml.XmlComment")
            {
                sb.Append("<!-- ");
                sb.Append((string)(nodeObj.data));
                sb.Append(" -->");
                return sb;
            }
            if (node.Name == "System.Xml.XmlCDataSection")
            {
                sb.Append("<![CData[");
                sb.Append((string)(nodeObj.data));
                sb.Append("]]>");
                return sb;
            }
            if (node.Name == "System.Xml.XmlText")
            {

                sb.Append((string)(nodeObj.data));
                return sb;
            }

            bool prefix = !String.IsNullOrEmpty((string)(nodeObj.name.prefix));

            sb.Append("<");
            if (prefix) sb.Append((string)(nodeObj.name.prefix) + ":");
            sb.Append((string)(nodeObj.name.localName));
            ulong attributes = nodeObj.attributes;
            if (attributes > 0)
            {
                ulong items = 0;
                int len = 0;
                ClrType nodes = nodeObj.attributes.nodes;

                if (nodes.Name == "System.Collections.ArrayList")
                {
                    items = nodeObj.attributes.nodes._items;
                    len = nodeObj.attributes.nodes._size;
                }
                else
                {
                    ClrType fieldType = nodeObj.attributes.nodes.field;
                    if (fieldType.Name == "System.Collections.ArrayList")
                    {
                        items = nodeObj.attributes.nodes.field._items;
                        len = nodeObj.attributes.nodes.field._size;
                    }
                }

                if (items != 0)
                {

                    ClrType arrAttr = m_heap.GetObjectType(items);


                    for (int i = 0; i < len; i++)
                    {

                        ulong addr = (ulong)arrAttr.GetArrayElementValue(items, i);

                        dynamic attr = cache.GetDinamicFromAddress(addr);
                        sb.Append(PrintAttribute(addr));

                    }
                }
                else
                {
                    sb.Append(PrintAttribute((ulong)(nodeObj.attributes.nodes.field)));
                }

            }
            if ((ulong)(nodeObj.lastChild) == 0 || (ulong)(nodeObj.lastChild) == Address)
                sb.Append(" />");
            else
                sb.Append(">");
            return sb;
        }

        public StringBuilder DumpXmlNodes(ulong Address, int Indentention = 0, StringBuilder sb = null)
        {
            if (sb == null)
                sb = new StringBuilder(100);
            if (Address == 0)
                return sb;
            List<ulong> nodes = new List<ulong>();
            ulong next = Address;
            while (next != 0)
            {
                if (next != Address) nodes.Add(next); // Add Last child last
                ClrType node = m_heap.GetObjectType(next);
                if (node.Name == "System.Xml.XmlDeclaration")
                {
                    sb.AppendFormat("{0}\n", PrintXmlNode(next, Indentention));
                }
                if (cache.IsDerivedOf(next, "System.Xml.XmlNode"))
                {

                    ClrInstanceField fNext = node.GetFieldByName("next");
                    next = (ulong)fNext.GetValue(next);
                    ulong i = nodes.FirstOrDefault(m => m == next);
                    if (i != 0 || next == Address)  // Let's avoid infinite loop
                    {
                        next = 0; // We went here
                    }
                }
                else
                {
                    sb.AppendLine("Something bad happened");
                    next = 0;
                }
            }
            // Now let's navigate in the right order
            nodes.Add(Address); // Last Child Node
            foreach (ulong node in nodes)
            {
                ClrType nodeObj = m_heap.GetObjectType(node);
                string lastName = String.Empty;
                if (nodeObj.Name != "System.Xml.XmlDeclaration")
                {
                    sb.AppendFormat("{0}\n", PrintXmlNode(node, Indentention));
                    ClrInstanceField fLastChild = nodeObj.GetFieldByName("lastChild");
                    if (fLastChild != null && (ulong)fLastChild.GetValue(node) != node)
                    {
                        dynamic nodeDyn = cache.GetDinamicFromAddress(node);

                        ulong child = (ulong)fLastChild.GetValue(node);
                        DumpXmlNodes(child, Indentention + 2, sb);
                        string str = new string(' ', Indentention);
                        if (nodeObj.Name != "System.Xml.XmlText")
                            sb.AppendFormat("{0}</{1}>\n", str, (string)(nodeDyn.name.localName));
                    }
                }

            }

            return sb;
        }

        private static int nsId = 0;

        private static Dictionary<string, string> nsToSchema;
        private static Dictionary<string, string> schemaToNs;


        private StringBuilder DumpXmlDoc(ulong Address, bool PrintOnly = false)
        {

            nsId = 0;
            nsToSchema = new Dictionary<string, string>();
            schemaToNs = new Dictionary<string, string>();
            ClrType xmlDoc = m_heap.GetObjectType(Address);
            if (xmlDoc == null || !cache.IsDerivedOf(Address, "System.Xml.XmlNode"))
            {
                if(PrintOnly) Exports.WriteLine("Not type System.Xml.XmlDocument");
                return null;
            }

            ClrInstanceField fLastChild = xmlDoc.GetFieldByName("lastChild");
            ulong next = (ulong)fLastChild.GetValue(Address);
            if (xmlDoc.Name != "System.Xml.XmlDocument" && (xmlDoc.BaseType != null && xmlDoc.BaseType.Name != "System.Xml.XmlDocument"))
            {
                next = Address;
                ClrInstanceField fParent = xmlDoc.GetFieldByName("parentNode");
                ulong parent = next;
                while (parent != 0)
                {
                    next = parent;
                    parent = (ulong)fParent.GetValue(next);
                }
                xmlDoc = m_heap.GetObjectType(next);
                if (xmlDoc.Name == "System.Xml.XmlDocument" || (xmlDoc.BaseType != null && xmlDoc.BaseType.Name == "System.Xml.XmlDocument"))
                {
                    fLastChild = xmlDoc.GetFieldByName("lastChild");
                    next = (ulong)fLastChild.GetValue(next);
                }

            }

            var sb = DumpXmlNodes(next);

            if (PrintOnly)
            {
                Exports.WriteLine("{0}", sb.ToString());
                return null;
               
            }
            return sb;
        }


    }

    [ComVisible(true)]
    [Guid("E225E0DD-BD54-4A29-A50D-EDA5E5BF5653")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDRuntime))]
    public class MDRuntime : IMDRuntime
    {
        ClrRuntime m_runtime;
        bool isFlushedCalled = false;

        public MDRuntime(ClrRuntime runtime)
        {
            m_runtime = runtime;
            m_runtime.RuntimeFlushed += m_runtime_RuntimeFlushed;
            DebugApi.InitClr(m_runtime);
        }

        void m_runtime_RuntimeFlushed(ClrRuntime runtime)
        {
            isFlushedCalled = true;
        }

        public int GetCommonMethodTable(out MD_CommonMT CommonMT)
        {
            CommonMT = new MD_CommonMT();
            if (m_runtime == null)
                return HRESULTS.E_FAIL;

            AdHoc.GetCommonMT(m_runtime, out CommonMT.StringMethodTable, out CommonMT.ArrayMethodTable,
                out CommonMT.FreeMethodTable);
            return HRESULTS.S_OK;
        }

        public int IsServerGC(out int pServerGC)
        {
            pServerGC = 0;
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            pServerGC = m_runtime.ServerGC ? 1 : 0;
            return HRESULTS.S_OK;
        }

        public int GetHeapCount(out int pHeapCount)
        {
            pHeapCount = 0;
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            pHeapCount = m_runtime.HeapCount;
            return HRESULTS.S_OK;
        }

        public int PrintFieldVisibility(ClrField field, bool isStatic = false)
        {
            Exports.Write("{0}", field.IsInternal ? "internal " : field.IsProtected ? "protected " : field.IsPrivate ? "private " : field.IsPublic ? "public " : "undefinedvisibility ");
            if (isStatic)
                Exports.Write("static ");
            Exports.Write("{0} ", field.Type == null ? "object" : field.Type.Name);
            Exports.WriteLine("{0};", field.Name);
            return HRESULTS.S_OK;
        }

        public int DumpClass(ulong MethodTable)
        {
            if (m_runtime == null)
                return HRESULTS.E_FAIL;

            ClrType type = AdHoc.GetTypeFromMT(m_runtime, MethodTable);
            if (type == null)
            {
                Exports.WriteLine("No type with Method Table {0:%p}", MethodTable);
                Exports.WriteLine("");
                return HRESULTS.E_FAIL;
            }
            string fileName = type.Module == null || !type.Module.IsFile ? "(dynamic)" : type.Module.FileName;
            Exports.WriteLine("// Method Table: {0}", MethodTable);
            Exports.WriteLine("// Module Address: {0:%p}", type.Module == null ? 0 : type.Module.ImageBase);
            Exports.WriteLine("// Debugging Mode: {0}", type.Module == null ? "(NA in Dynamic Module)" : type.Module.DebuggingMode.ToString());
            Exports.WriteLine("// Filename: {0}", fileName);
            Exports.WriteLine("namespace {0} {1}", type.Name.Substring(0, type.Name.LastIndexOf(".")), "{");

            Exports.WriteLine("");
            Exports.Write(" ");
            Exports.Write("{0}", type.IsInternal ? "internal " : type.IsProtected ? "protected " : type.IsPrivate ? "private " : type.IsPublic ? "public " : "undefinedvisibility ");
            Exports.Write("{0}", type.IsSealed ? "sealed " : "");
            Exports.Write("{0}", type.IsAbstract ? "abstract " : "");
            Exports.Write("{0}", type.IsInterface ? "interface " : "");
            Exports.Write("{0}", type.IsValueClass ? "struct " : "");
            Exports.Write("{0}", !type.IsValueClass && !type.IsInterface ? "class " : "");
            Exports.Write("{0}", type.Name.Split('.')[type.Name.Split('.').Length - 1]);
            if ((type.BaseType != null && type.BaseType.Name != "System.Object") || type.Interfaces.Count > 0)
            {

                Exports.Write(": ");
                if (type.BaseType != null && type.BaseType.Name != "System.Object")
                {
                    Exports.WriteDml("<link cmd=\"!wclass {0:%p}\">{1}</link>", HeapStatItem.GetMTOfType(type.BaseType), type.BaseType.Name);
                    if (type.Interfaces.Count > 0)
                        Exports.Write(", ");
                }
                for (int i = 0; i < type.Interfaces.Count; i++)
                {
                    Exports.Write("{0}", type.Interfaces[i].Name);
                    if (i < type.Interfaces.Count - 1)
                        Exports.Write(", ");
                }
            }
            Exports.WriteLine("");
            Exports.WriteLine("{0}", " {");
            Exports.WriteLine("\t//");
            Exports.WriteLine("\t// Fields");
            Exports.WriteLine("\t//");
            Exports.WriteLine("");
            foreach (var field in type.Fields)
            {
                if (Exports.isInterrupted())
                    return HRESULTS.S_FALSE;
                Exports.Write("\t");
                PrintFieldVisibility(field);
            }
            Exports.WriteLine("");
            Exports.WriteLine("\t//");
            Exports.WriteLine("\t// Static Fields");
            Exports.WriteLine("\t//");
            Exports.WriteLine("");
            foreach (var field in type.StaticFields)
            {
                if (Exports.isInterrupted())
                    return HRESULTS.S_FALSE;
                Exports.Write("\t");
                PrintFieldVisibility(field, true);
            }

            foreach (var field in type.ThreadStaticFields)
            {
                if (Exports.isInterrupted())
                    return HRESULTS.S_FALSE;
                Exports.Write("\t");
                PrintFieldVisibility(field, true);
            }

            var properties =
                from m in type.Methods
                where m.Name.StartsWith("get_") ||
                    m.Name.StartsWith("set_")
                orderby m.Name.Substring(4).Split('(')[0], -(int)m.Name[0]
                select m;

            Exports.WriteLine("");
            Exports.WriteLine("\t//");
            Exports.WriteLine("\t// Properties");
            Exports.WriteLine("\t//");
            Exports.WriteLine("");

            List<string> propstr = new List<string>();
            int propCount = 0;
            foreach (ClrMethod met in properties)
            {
                if (Exports.isInterrupted())
                    return HRESULTS.S_FALSE;
                string prop = met.Name.Substring(4);
                bool isFirst = propstr.IndexOf(prop) == -1;
                if (isFirst)
                {
                    if (propCount > 0)
                    {
                        Exports.WriteLine("\t{0}", "}"); // close previous
                    }
                    Exports.Write("\t");
                    if (met.Name.StartsWith("set_"))
                    {
                        Exports.Write("{0} ", met.GetFullSignature().Split('(')[1].Split(')')[0]);
                    }
                    else
                    {
                        Exports.Write("/* property * / ");
                    }
                    Exports.WriteLine("{0}", prop.Split('(')[0]);
                    Exports.WriteLine("\t{0}", "{");

                    propstr.Add(prop);
                }
                Exports.WriteLine("");
                Exports.WriteLine("\t\t// JIT MODE: {0} - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND", met.CompilationType);
                if (met.NativeCode != ulong.MaxValue)
                {
                    Exports.WriteDmlLine("\t\t// Click for breakpoint: <link cmd=\"bp {0:%p}\">{0:%p}</link>", met.NativeCode);
                }
                else
                {
                    Exports.WriteLine("\t\t// Not JITTED");
                }
                Exports.Write("\t\t{0}", met.IsInternal ? "internal " : met.IsProtected ? "protected " : met.IsPrivate ? "private " : met.IsPublic ? "public " : "");

                Exports.WriteLine("{0} {1}", met.Name.Substring(0, 3), " { } ");
                propCount++;

            }
            if (propCount > 0)
                Exports.WriteLine("\t{0}", "}"); // close previous
            Exports.WriteLine("");
            Exports.WriteLine("\t//");
            Exports.WriteLine("\t// Methods");
            Exports.WriteLine("\t//");
            Exports.WriteLine("");

            foreach (var method in type.Methods)
            {
                if (Exports.isInterrupted())
                    return HRESULTS.S_FALSE;

                if (!(method.Name.StartsWith("get_") || method.Name.StartsWith("set_")))
                {
                    Exports.WriteLine("");
                    Exports.WriteLine("\t// JIT MODE: {0} - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND", method.CompilationType);
                    if (method.NativeCode != ulong.MaxValue)
                    {
                        Exports.WriteDmlLine("\t// Click for breakpoint: <link cmd=\"bp {0:%p}\">{0:%p}</link>", method.NativeCode);
                    }
                    else
                    {
                        Exports.WriteLine("\t// Not JITTED");
                    }
                    Exports.Write("\t");

                    Exports.Write("{0}", method.IsInternal ? "internal " : method.IsProtected ? "protected " : method.IsPrivate ? "private " : method.IsPublic ? "public " : "");
                    Exports.Write("{0}", method.IsVirtual ? "virtual " : "");
                    Exports.Write("{0}", method.IsStatic ? "static " : "");

                    Exports.WriteLine("{0}({1};", method.Name, method.GetFullSignature().Split('(')[method.GetFullSignature().Split('(').Length - 1]);
                }
            }

            Exports.WriteLine("{0}", " }");
            Exports.WriteLine("{0}", "}");

            return HRESULTS.S_OK;
        }

        public int DumpDomains()
        {
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            var domains = AdHoc.GetDomains(m_runtime);
            if (domains == null || domains.Length < 2)
            {
                Exports.WriteLine("Unable to get Application Domains. This is not expected.");
                return HRESULTS.E_FAIL;
            }

            if (m_runtime.PointerSize == 8)
                Exports.WriteLine("Address          Domain Name                                                 Modules Base Path & Config");
            else
                Exports.WriteLine("Address  Domain Name                                                 Modules Base Path & Config");


            for (int i = 0; i < domains.Length; i++)
            {
                if (Exports.isInterrupted())
                    return HRESULTS.S_FALSE;

                Exports.Write("{0:%p} ", m_runtime.PointerSize == 8 ? domains[i].Address : domains[i].Address & UInt32.MaxValue);
                Exports.Write("{0, -60} ", i == 0 ? "System" : i == 1 ? "Shared" : domains[i].Name);
                Exports.Write("{0,6:#,#} ", domains[i].Modules.Count);
                if (!String.IsNullOrEmpty(domains[i].ApplicationBase)) Exports.Write("Base Path: {0} ", domains[i].ApplicationBase);
                if (!String.IsNullOrEmpty(domains[i].ConfigurationFile)) Exports.Write("Config: {0} ", domains[i].ConfigurationFile);
                Exports.WriteLine("");

            }
            return HRESULTS.S_OK;

        }

        public int DumpHandles(int GroupOnly,string filterByType, string filterByObjType)
        {
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            Dictionary<string, int> categories = new Dictionary<string, int>();
            if (m_runtime.PointerSize == 8)
            {
                Exports.WriteLine("Handle           Object           Refs Type            Object Type");
            }
            else
            {
                Exports.WriteLine("Handle   Object   Refs Type            Object Type");
            }

            int i = 0;
            int c = 0;
            foreach (var handle in m_runtime.EnumerateHandles())
            {
                if (Exports.isInterrupted())
                    return HRESULTS.S_OK;
                if (!categories.ContainsKey(handle.HandleType.ToString()))
                {
                    categories[handle.HandleType.ToString()] = 0;
                }
                categories[handle.HandleType.ToString()]++;
                ClrType obj = m_runtime.Heap.GetObjectType(handle.Object);

                if (
                    (String.IsNullOrEmpty(filterByType) || handle.HandleType.ToString().ToLowerInvariant().Contains(filterByType.ToLowerInvariant()))
                    &&
                    (String.IsNullOrEmpty(filterByObjType) || obj.Name.ToLowerInvariant().Contains(filterByObjType.ToLowerInvariant()))
                    )
                {
                    if (GroupOnly == 0)
                    {
                        Exports.Write("{0:%p} {1:%p}", handle.Address, handle.Object);
                        Exports.Write(" {0,4} {1,-15}", handle.RefCount, handle.HandleType);
                        Exports.Write(" {0}", obj.Name);
                        Exports.WriteLine("");
                    }
                    c++;
                }
                i++;
            }
            Exports.WriteLine("");
            Exports.WriteLine("{0,8:#,#} Objects Listed or met the criteria", c);
            if (c != i)
                Exports.WriteLine("{0,8:#,#} Objects Skipped by the filter(s)", i - c);
            Exports.WriteLine("");
            Exports.WriteLine("{0,8:#,#} Handle(s) found in {1} categories", i, categories.Keys.Count);
            foreach (var cat in categories.Keys)
            {
                Exports.Write("{0,8:#,#} ", categories[cat]);
                Exports.WriteDml("<link cmd=\"!wgchandle -handletype {0}\">{0}</link>", cat);
                Exports.WriteLine(" found");
            }
            Exports.WriteLine("");
            return HRESULTS.S_OK;
        }

        public static void AddIfTrue(ref StringBuilder Sb, bool IsTrue, string StrToAdd)
        {
            if (!IsTrue) return;
            if (Sb.Length > 0) Sb.Append('|');
            Sb.Append(StrToAdd);

        }

        public int DumpThreads()
        {
            if(m_runtime == null)
                return HRESULTS.E_FAIL;
            if (m_runtime.PointerSize == 8)
                Exports.WriteLine("   Id OSId Address          Domain           Allocation Start:End              COM  GC Type  Locks Type / Status             Last Exception");
            else
                Exports.WriteLine("   Id OSId Address  Domain   Alloc Start:End   COM  GC Type  Locks Type / Status             Last Exception");

            foreach (var thread in m_runtime.Threads)
            {
                if (Exports.isInterrupted())
                {
                    return HRESULTS.S_FALSE;
                }
                StringBuilder sb = new StringBuilder();
                ulong AllocStart;
                ulong AllocEnd;
                AdHoc.GetThreadAllocationLimits(m_runtime, thread.Address, out AllocStart, out AllocEnd);
                Exports.Write("{0,5}", thread.ManagedThreadId);
                if (thread.OSThreadId != 0)
                    Exports.WriteDml(" <link cmd=\"~~[{0:x4}]s\">{0:x4}</link>", thread.OSThreadId);
                else
                    Exports.Write(" ----");
                Exports.Write(" {0:%p} {1:%p} {2:%p}:{3:%p}", thread.Address, thread.AppDomain,
                    AllocStart, AllocEnd);
                Exports.Write(" {0}", thread.IsSTA ? "STA " : thread.IsMTA ? "MTA " : "NONE");
                Exports.Write(" {0,-11}", thread.GcMode.ToString());
                Exports.Write(" {0,2}", thread.LockCount > 9 ? "9+" : thread.LockCount.ToString());

                AddIfTrue(ref sb, thread.IsAbortRequested, "Aborting");
                AddIfTrue(ref sb, thread.IsBackground, "Background");
                AddIfTrue(ref sb, thread.IsDebuggerHelper, "Debugger");
                AddIfTrue(ref sb, thread.IsFinalizer, "Finalizer");
                AddIfTrue(ref sb, thread.IsGC, "GC");
                AddIfTrue(ref sb, thread.IsShutdownHelper, "Shutting down Runtime");
                AddIfTrue(ref sb, thread.IsSuspendingEE, "EESuspend");
                AddIfTrue(ref sb, thread.IsThreadpoolCompletionPort, "IOCPort");
                AddIfTrue(ref sb, thread.IsThreadpoolGate, "Gate");
                AddIfTrue(ref sb, thread.IsThreadpoolTimer, "Timer");
                AddIfTrue(ref sb, thread.IsThreadpoolWait, "Wait");
                AddIfTrue(ref sb, thread.IsThreadpoolWorker, "Worker");
                AddIfTrue(ref sb, thread.IsUnstarted, "NotStarted");
                AddIfTrue(ref sb, thread.IsUserSuspended, "Thread.Suspend()");
                AddIfTrue(ref sb, thread.IsDebugSuspended, "DbgSuspended");
                AddIfTrue(ref sb, !thread.IsAlive, "Terminated");
                AddIfTrue(ref sb, thread.IsGCSuspendPending, "PendingGC");
                Exports.Write(" {0,-25}", sb.ToString());
                if (thread.CurrentException != null)
                {
                    Exports.Write(" {0}", thread.CurrentException.Type.Name);
                }


                if (thread.IsAbortRequested) sb.Append("Aborting");
                if (thread.IsBackground)
                {
                    if (sb.Length > 0) sb.Append("|");
                    sb.Append("Background");
                }
                Exports.WriteLine("");
            }
            return HRESULTS.S_OK;
        }



        public int ReadVirtual(ulong addr, byte[] buffer, int requested, out int pRead)
        {
            int read;
            pRead = 0;
            if (m_runtime == null)
                return -1;

            bool success = false;
            try
            {
                success = m_runtime.ReadMemory(addr, buffer, requested, out read);

                pRead = (int)read;
            }
            catch
            {
                return -1;
            }
            return success ? 0 : -1;
        }

        public int ReadPtr(ulong addr, out ulong pValue)
        {
            pValue = 0;
            if (m_runtime == null)
                return 0;
            bool success = m_runtime.ReadPointer(addr, out pValue);
            return success ? 1 : 0;
        }

        public int Flush()
        {
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            m_runtime.Flush();
            return HRESULTS.S_OK;
        }

        public int IsFlush(out int isFlushed)
        {
            isFlushed = isFlushedCalled ? 1 : 0;
            return HRESULTS.S_OK;
        }

        public int GetHeap(out IMDHeap ppHeap)
        {
            isFlushedCalled = false;
            ppHeap = null;
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            ppHeap = new MDHeap(m_runtime.Heap);
            return HRESULTS.S_OK;
        }

        public int EnumerateAppDomains(out IMDAppDomainEnum ppEnum)
        {
            ppEnum = null;
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            ppEnum = new MDAppDomainEnum(AdHoc.GetDomains(m_runtime));
            return HRESULTS.S_OK;
        }

        public int EnumerateThreads(out IMDThreadEnum ppEnum)
        {
            ppEnum = null;
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            ppEnum = new MDThreadEnum(m_runtime.Threads, m_runtime);
            return HRESULTS.S_OK;
        }

        public int EnumerateFinalizerQueue(out IMDObjectEnum ppEnum)
        {
            ppEnum = null;
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            ppEnum = new MDObjectEnum(new List<ulong>(m_runtime.EnumerateFinalizerQueueObjectAddresses()));
            return HRESULTS.S_OK;
        }

        public int EnumerateGCHandles(out IMDHandleEnum ppEnum)
        {
            ppEnum = null;
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            ppEnum = new MDHandleEnum(m_runtime.EnumerateHandles());
            return HRESULTS.S_OK;
        }

        public int EnumerateMemoryRegions(out IMDMemoryRegionEnum ppEnum)
        {
            ppEnum = null;
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            ppEnum = new MDMemoryRegionEnum(new List<ClrMemoryRegion>(m_runtime.EnumerateMemoryRegions()));
            return HRESULTS.S_OK;
        }

        public int GetArraySizeByMT(ulong MethodTable, out ulong BaseSize, out ulong ComponentSize)
        {
            MethodTable = 0;
            BaseSize = 0;
            ComponentSize = 0;
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            TypeBasicInfo bi = AdHoc.GetArrayDataSimple(m_runtime, MethodTable);
            BaseSize = bi.BaseSize;
            ComponentSize = bi.ComponentSize;
            return HRESULTS.S_OK;
        }

        public int GetNameForMT(ulong MethodTable, [Out] [MarshalAs((UnmanagedType)19)] out string pTypeName)
        {
            pTypeName = null;
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            pTypeName = AdHoc.GetNameForMT(m_runtime, MethodTable);
            return HRESULTS.S_OK;
        }

        public int GetTypeByMT(ulong addr, out IMDType ppType)
        {
            ppType = null;
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            ppType = new MDType(AdHoc.GetTypeFromMT(m_runtime, addr));
            return HRESULTS.S_OK;
        }

        public int GetMethodNameByMD(ulong addr, out string pMethodName)
        {
            pMethodName = null;
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            var method = m_runtime.GetMethodByHandle(addr);
            if (method == null)
            {
                return HRESULTS.E_FAIL;
            }
            pMethodName = method.GetFullSignature();
            return HRESULTS.S_OK;
        }

        public int GetOSThreadIDByAddress(ulong ThreadAddress, [Out] out uint OSThreadID)
        {
            
            OSThreadID = 0;
            if (m_runtime == null)
                return HRESULTS.E_FAIL;
            try
            {
                OSThreadID = AdHoc.GetOSThreadIdByAddress(m_runtime, ThreadAddress);
            }
            catch
            {
                return HRESULTS.E_FAIL;
            }
            return OSThreadID == 0 ? HRESULTS.E_FAIL : HRESULTS.S_OK;
        }

    }

    [ComVisible(true)]
    [Guid("2A3840BE-9546-4A0E-8C8B-2BC8A9764D1C")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDRuntimeInfo))]
    public class MDRuntimeInfo : IMDRuntimeInfo
    {
        ClrInfo m_info;

        public MDRuntimeInfo(ClrInfo info)
        {
            m_info = info;
        }

        public int GetRuntimeVersion(out string pVersion)
        {
            Module clr = new Module("clr");
            Module mscor = new Module("mscorwks");
            if (clr.IsValid)
            {
                Version ver = clr.VersionInfo;
                if (ver.Minor == 0 && ver.Revision <= 17000)
                {
                    pVersion = String.Format("{0} ({1})", clr.Version, ".NET 4.0");
                    return HRESULTS.S_OK;
                }
                if (ver.Minor == 0 && ver.Revision <= 18400)
                {
                    pVersion = String.Format("{0} ({1})", clr.Version, ".NET 4.5");
                    return HRESULTS.S_OK;
                }

                if (ver.Minor == 0 && ver.Revision <= 34000)
                {
                    pVersion = String.Format("{0} ({1})", clr.Version, ".NET 4.5.1");
                    return HRESULTS.S_OK;
                }

                if (ver.Minor == 0)
                {
                    pVersion = String.Format("{0} ({1})", clr.Version, ".NET 4.5.2");
                    return HRESULTS.S_OK;
                }

                pVersion = ver.ToString();
                return HRESULTS.S_OK;

            }
            pVersion = null;
            if (!mscor.IsValid)
                return HRESULTS.E_FAIL;
            pVersion = mscor.Version;
            return HRESULTS.S_OK;
        }

        public int GetDacLocation(out string pLocation)
        {
            pLocation = null;
            if (m_info == null)
                return HRESULTS.E_FAIL;
            pLocation = m_info.LocalMatchingDac;
            return HRESULTS.S_OK;
        } 

        public int GetDacRequestData(out int pTimestamp, out int pFilesize)
        {
            pTimestamp = 0;
            pFilesize = 0;
            if (m_info == null || m_info.DacInfo == null)
                return HRESULTS.E_FAIL;
            pTimestamp = (int)m_info.DacInfo.TimeStamp;
            pFilesize = (int)m_info.DacInfo.FileSize;
            return HRESULTS.S_OK;
        }

        public int GetDacRequestFilename(out string pRequestFileName)
        {
            pRequestFileName = null;
            if (m_info == null)
                return HRESULTS.E_FAIL;
            pRequestFileName = m_info.DacInfo.FileName;
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("05957304-2C21-43EA-B5FE-A5D42084B5A7")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDTarget))]
    public class MDTarget : IMDTarget
    {
        DataTarget m_target = null;

        public MDTarget(string crashdump)
        {
            // TODO: Complete member initialization
            try
            {
                m_target = DataTarget.LoadCrashDump(crashdump);
            }
            catch { }
        }

        public MDTarget(object iDebugClient)
        {
            try
            {
                m_target = DataTarget.CreateFromDebuggerInterface((IDebugClient)iDebugClient);
            }
            catch { }
        }

        public int GetRuntimeCount(out int pCount)
        {
            pCount = 0;
            if(m_target == null || m_target.ClrVersions == null)
                return HRESULTS.E_FAIL;
            pCount = m_target.ClrVersions.Count;
            return HRESULTS.S_OK;
        }

        public int GetRuntimeInfo(int num, out IMDRuntimeInfo ppInfo)
        {
            ppInfo = null;
            if (m_target == null || m_target.ClrVersions == null)
                return HRESULTS.E_FAIL;
            ppInfo = new MDRuntimeInfo(m_target.ClrVersions[num]);
            return HRESULTS.S_OK;
        }

        public int GetPointerSize(out int pPointerSize)
        {
            pPointerSize = 0;
            if (m_target == null)
                return HRESULTS.E_FAIL;
            pPointerSize = (int)m_target.PointerSize;
            return HRESULTS.S_OK;
        }

        public int CreateRuntimeFromDac(string dacLocation, out IMDRuntime ppRuntime)
        {
            ppRuntime = null;
            if (m_target == null || m_target.ClrVersions == null)
                return HRESULTS.E_FAIL;
            
            ppRuntime = new MDRuntime(m_target.ClrVersions.Single().CreateRuntime(dacLocation));
            return HRESULTS.S_OK;
        }

        public int CreateRuntimeFromIXCLR(object ixCLRProcess, out IMDRuntime ppRuntime)
        {
            ppRuntime = null;
            if (m_target == null || m_target.ClrVersions == null)
                return HRESULTS.E_FAIL;
            DebugApi.Runtime = m_target.ClrVersions.Single().CreateRuntime(ixCLRProcess);
            ppRuntime = new MDRuntime(DebugApi.Runtime);
            
            return HRESULTS.S_OK;
        }

        internal const string DownloadUrl = "https://github.com/rodneyviana/netext/tree/master/Binaries/";
        internal const string versionUrl = "https://github.com/rodneyviana/netext/";

        internal static Version GetOnlineVersion()
        {
            Version codeplex = new Version(0, 0, 0, 0);

            try
            {
                //
                // GitHub is now requiring TLS 1.2, make sure it is enabled
                //
                if(!ServicePointManager.SecurityProtocol.HasFlag(SecurityProtocolType.Tls12))
                {
                    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                }
                WebClient client = new WebClient();
                string text = client.DownloadString(versionUrl);
                Regex reg = new Regex(@"VERSION:\s+(\d+)\.(\d+)\.(\d+)\.(\d+)");
                Match match = reg.Match(text);

                if (match.Groups.Count == 5)
                {
                    try
                    {
                        codeplex = new Version(Int32.Parse(match.Groups[1].Value),
                            Int32.Parse(match.Groups[2].Value),
                            Int32.Parse(match.Groups[3].Value),
                            Int32.Parse(match.Groups[4].Value));
                    }
                    catch (Exception ex)
                    {
                        Exports.WriteLine("Error parsing version: {0}", ex.Message);
                    }
                    finally
                    {
                        if (codeplex.Major == 0)
                            Exports.WriteLine("Unable to retrieve current version");
                    }
                }
            }
            catch(Exception ex)
            {
                Exports.WriteLine("Error retrieving version: {0}", ex.Message);
            }

            return codeplex;
        }

        public int CompareVersion(int Major, int Minor, int Build, int Revision)
        {
            Version curr = new Version(Major, Minor, Build, Revision);
#if DEBUG
            Exports.WriteLine("***Debug Build***");
#endif
            Version online = GetOnlineVersion();
            if (online > curr)
            {
                Exports.Write("Current Version: {0} - New Version: {1}", curr.ToString(),
                    online.ToString());
                Exports.WriteDmlLine(" download the new version here: <link cmd=\"!wopendownloadpage\">{0}</link>", DownloadUrl);
            }
            else
            {
                if (online.Major == 0)
                    Exports.WriteDmlLine("Unable to retrieve lastest version. Please visit this page to get the latest version: <link cmd=\"!wopendownloadpage\">{0}</link>", DownloadUrl);
                else
                    Exports.WriteLine("You are up to date. Your version is {0}", curr.ToString());
            }
            Exports.WriteLine("");
            return HRESULTS.S_OK;
        }

        public int OpenDownloadPage()
        {
            try
            {
                Process.Start(DownloadUrl);
                Exports.WriteLine("If the update page did not open up, please navigate manually to {0}", DownloadUrl);
            }
            catch(Exception ex)
            {
                Exports.WriteLine("Unable to open the page ({0}). Please visit this page manually to get the latest version: {1}", ex.Message, DownloadUrl);
                return HRESULTS.E_FAIL;
            }
            return HRESULTS.S_OK;
        }

        public int CreateEnum(out IMDObjectEnum ppEnum)
        {
            ppEnum = new MDObjectEnum();
            return HRESULTS.S_OK;
        }

        public int GetCurrentTime(out Int64 TargetTime, out Int64 UtcTime)
        {
            TargetTime = DebugApi.CurrentTimeDateLocal.Ticks;
            UtcTime = DebugApi.CurrentTimeDate.Ticks;
            if (String.IsNullOrWhiteSpace(DebugApi.SharedData.NtSystemRoot) || DebugApi.SharedData.NtSystemRoot[1] != ':') // Bad SharedData
                return HRESULTS.E_FAIL;
            return HRESULTS.S_OK;
        }

        public int DumpTime()
        {
            if (String.IsNullOrWhiteSpace(DebugApi.SharedData.NtSystemRoot) || DebugApi.SharedData.NtSystemRoot[1] != ':') // Bad SharedData
            {
                return HRESULTS.E_FAIL;
            }
            Exports.WriteLine("UTC Time   : {0}", DebugApi.CurrentTimeDate.ToString("MM/dd/yyyy HH:mm:ss.ff"));
            Exports.WriteLine("Target Time: {0}", DebugApi.CurrentTimeDateLocal.ToString("MM/dd/yyyy HH:mm:ss.ff"));
            return HRESULTS.S_OK;
        }

        public int GetModuleByAddress(ulong Address, [Out] IMDModule Module)
        {
            Module = new MDModule(Address);
            if(((MDModule)Module).module.IsValid)
                return HRESULTS.S_OK;
            return HRESULTS.E_FAIL;
        }

        public int GetModuleByName([MarshalAs((UnmanagedType)19)] string ModuleName, [Out] IMDModule Module)
        {
            Module = new MDModule(ModuleName);
            if (((MDModule)Module).module.IsValid)
                return HRESULTS.S_OK;
            return HRESULTS.E_FAIL;

        }

        public int GetModuleByIndex(int Index, [Out] IMDModule Module)
        {
            if (Index < NetExt.Shim.Module.Count)
            {
                Module = new MDModule(Index);
            }
            if (((MDModule)Module).module.IsValid)
                return HRESULTS.S_OK;
            return HRESULTS.E_FAIL;
        }

        public int GetContextModule([Out] IMDModule Module)
        {
            Module = new MDModule(DebugApi.ModuleFromScope);
            if (((MDModule)Module).module.IsValid)
                return HRESULTS.S_OK;
            return HRESULTS.E_FAIL;
        }

        public int GetModuleCount([Out] int Count)
        {
            Count = (int)Module.Count;
            return HRESULTS.S_OK;
        }

        public int SaveAllModules([MarshalAs((UnmanagedType)19)] string Path)
        {
            if (!System.IO.Directory.Exists(Path))
            {
                Exports.WriteLine("Path {0} is invalid or not created", Path);
                return HRESULTS.E_FAIL;
            }
            int total = 0;
            int errors = 0;
            foreach (var mod in Module.Modules)
            {
                try
                {
                    total++;
                    var md = new MDModule(mod);
                    if (md.SaveModule(Path) != HRESULTS.S_OK)
                    {
                        errors++;
                        Exports.WriteLine("Unable to save module {0}", mod.Name);
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    Exports.WriteLine("Unable to save module {0}", mod.Name);
                    Exports.WriteLine("{0}", ex.ToString());
                }
            }
            Exports.WriteLine("{0} modules saved, {1} errors", total - errors, errors);
            return HRESULTS.S_OK;
        }

        public int SaveModule(string Path, string ModuleName)
        {
            if (!System.IO.Directory.Exists(Path))
            {
                Exports.WriteLine("Path {0} is invalid or not created", Path);
                return HRESULTS.E_FAIL;
            }

            Module mod = new Module(ModuleName);
            if (!mod.IsValid)
            {
                Exports.WriteLine("Unable to find module {0}", ModuleName);
                return HRESULTS.E_FAIL;
            }
            string fullPath = System.IO.Path.Combine(Path, ModuleName);

            var md = new MDModule(mod);
            if (md.SaveModule(Path) != HRESULTS.S_OK)
            {
                Exports.WriteLine("Unable to save module {0}", mod.Name);
            }

            return HRESULTS.S_OK;
        }

        public List<NetExt.Shim.Module> GetModules(string Pattern, string Company, bool DebugMode,
            bool ManagedOnly, bool ExcludeMicrosoft)
        {
            List<NetExt.Shim.Module> modules = new List<NetExt.Shim.Module>();

            foreach (var mod in NetExt.Shim.Module.Modules)
            {
                if (DebugMode && (int)mod.ClrDebugType < 4)
                {
                    continue;
                }
                if (ManagedOnly && !mod.IsClr)
                {
                    continue;
                }
                if (!String.IsNullOrEmpty(Pattern) && !HeapCache.WildcardCompare(mod.Name, Pattern))
                {
                    continue;
                }
                if (!String.IsNullOrEmpty(Company) && !HeapCache.WildcardCompare(mod.CompanyName, Company))
                {
                    continue;
                }
                if (ExcludeMicrosoft && (mod.CompanyName == "Microsoft Corporation"))
                {
                    continue;
                }
                modules.Add(mod);
            }


            return modules;
        }

        public int DumpModules(string Pattern, string Company, string folderToSave, bool DebugMode,
            bool ManagedOnly, bool ExcludeMicrosoft, bool Ordered, bool IncludePath)
        {
            IEnumerable<Module> modules = GetModules(Pattern, Company, DebugMode,
            ManagedOnly, ExcludeMicrosoft);

            if (!String.IsNullOrWhiteSpace(folderToSave))
            {
                int m = 0;
                int f = 0;
                foreach (var mod in modules)
                {
                    if (!Directory.Exists(folderToSave))
                    {
                        Exports.WriteLine("Folder '{0}' does not exist. Please choose an existing folder.", folderToSave);
                        return HRESULTS.E_FAIL;
                    }
                    string fileName = Path.Combine(folderToSave, mod.Name);
                    if (File.Exists(fileName))
                    {
                        Exports.WriteLine("File '{0}' already exists. Skipping this file", fileName);
                        f++;
                    }
                    else
                    {
                        try
                        {
                            using (FileStream fs = new FileStream(fileName, FileMode.CreateNew))
                            {
                                mod.SaveToStream(fs);
                                m++;
                                Exports.WriteLine("Saved '{0}' successfully", fileName);
                            }
                        }
                        catch (Exception ex)
                        {
                            Exports.WriteLine("Failed to save '{0}' - {1}: {2}", fileName, ex.GetType().ToString(), ex.Message);
                            f++;
                        }
                    }
                }
                Exports.WriteLine("");
                Exports.WriteLine("{0} module(s) saved, {1} failed, {2} skipped by the filters", m, f, Module.Modules.Count - m);
                return HRESULTS.S_OK;

            }

            if (Ordered)
            {
                modules = from m in modules
                          orderby m.Name
                          select m;
            }

            if (DebugApi.IsTaget64Bits)
                Exports.WriteLine("{0}", "Address                      Module Version Company Name       Debug Mode Type Module Binary");
            else
                Exports.WriteLine("{0}", "Address              Module Version Company Name       Debug Mode Type Module Binary");

            int i = 0;
            foreach (var mod in modules)
            {
                Exports.WriteDml("<link cmd=\"lmv a {0:%p}\">{0:%p}</link> ", mod.BaseAddress);
                string fileName = null;
                if(IncludePath)
                   fileName = mod.FullPath;
                else
                   fileName = mod.Name;
                Exports.WriteLine(" {0,25} {1,-25} {2,-3}  {3,-3} {4}", mod.VersionInfo, mod.CompanyName, (int)mod.ClrDebugType >= 4 ? "Yes" : "No", mod.IsClr ? "CLR" : "NAT", fileName);
                i++;
                if (DebugApi.CheckControlC())
                    return HRESULTS.S_OK;

            }
            Exports.WriteLine("");
            Exports.WriteLine("{0} module(s) listed, {1} skipped by the filters", i, Module.Modules.Count - i);
            return HRESULTS.S_OK;
        }

        private static bool isExeCreated = false;
        private static string toolsPath = null;
        public static string ToolsPath { get { return toolsPath; } }
        public static void CopyTools()
        {
            if (!isExeCreated)
            {
#if DEBUG
                Exports.WriteLine("Expanding tools...");
#endif
                toolsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                try
                {
                    Directory.CreateDirectory(toolsPath);
                }
                catch (Exception ex)
                {
                    Exports.WriteLine("Failed to create tools folder at {0}", toolsPath);
                    Exports.WriteLine("Error: {0}: {1}", ex.GetType(), ex.Message);
                    return;
                }
                isExeCreated = true;
                string[] files = new string[] { "ICSharpCode_Decompiler", "ICSharpCode_NRefactory", "ICSharpCode_NRefactory_CSharp",
                    "ILSpy.exe", "Mono_Cecil", "Mono_Cecil_Pdb", "PdbRestorer.exe", "PdbRestorer_Engine", "ICSharpCode_AvalonEdit" };

                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i].Replace('_', '.');
                    if (!file.EndsWith(".exe") && !file.EndsWith(".xml"))
                        file += ".dll";
                    try
                    {
                        byte[] streamBytes = (byte[])Properties.Resources.ResourceManager.GetObject(files[i].Split('.')[0]);
                        using (Stream fileStream = File.OpenWrite(Path.Combine(toolsPath, file)))
                        {
                            DebugApi.DebugWriteLine("Copying {0}", Path.Combine(toolsPath, file));
                            fileStream.Write(streamBytes, 0, streamBytes.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        Exports.WriteLine("Failed to copy tool {0}", file);
                        Exports.WriteLine("Error: {0}: {1}", ex.GetType(), ex.Message);
                        isExeCreated = false;
                    }

                }

            }
        }

        public int MakeSource()
        {
            return MakeSourceInternal();
        }

        public int MakeSourceFromIp(ulong IPAddress)
        {
            return MakeSourceInternal(IPAddress);
        }

        public int MakeSourceInternal(ulong IP=0)
        {

            Module mod = IP == 0 ? DebugApi.ModuleFromScope : DebugApi.GetModuleFromIp(IP);
            if (!mod.IsClr)
            {
                Exports.WriteLine("Module {0} is not managed. No source will be created.", mod.Name);
                Exports.WriteLine("Move to the frame context in the stack where you want the code created (example .frame 3)");
                return HRESULTS.E_FAIL;
            }
            if (mod != null && mod.BaseAddress != 0)
            {
                CopyTools();
                if (!isExeCreated)
                    return HRESULTS.E_FAIL;

                string[] parts = mod.FullPath.Split('\\');
                string exeFolder = Path.Combine(toolsPath, "lib");
                string symFolder = Path.Combine(toolsPath, "sym");
                if (!Directory.Exists(exeFolder))
                    Directory.CreateDirectory(exeFolder);
                if (!Directory.Exists(symFolder))
                    Directory.CreateDirectory(symFolder);



                string file = Path.Combine(exeFolder, parts[parts.Length - 1]);
                string source = Path.Combine(symFolder, Path.ChangeExtension(parts[parts.Length - 1], "cs"));
                string symbol = Path.Combine(symFolder, Path.ChangeExtension(parts[parts.Length - 1], "pdb"));

                Stream fileStream = File.OpenWrite(file);
                try
                {
                    mod.SaveToStream(fileStream);
                }
                finally
                {
                    fileStream.Close();
                }

                Process decomp = new Process();
                decomp.StartInfo.FileName = Path.Combine(toolsPath, "PdbRestorer.exe");
                decomp.StartInfo.Arguments = String.Format("C# \"{0}\" \"{1}\" \"{2}\"", file, source, symbol);
                
                decomp.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                decomp.StartInfo.CreateNoWindow = true;
                decomp.Start();
                DebugApi.DebugWriteLine("running {0} {1}", decomp.StartInfo.FileName, decomp.StartInfo.Arguments);
                Exports.WriteLine("Generating source and Pdb file");
                while (!decomp.WaitForExit(1000))
                {
                    Exports.Write(".");
                }
                Exports.WriteLine("");

                if (decomp.ExitCode != 0)
                {
                    Exports.WriteLine("Failed to generate file/symbol {0:x4}", decomp.ExitCode);
                    return decomp.ExitCode;
                }

                DebugApi.AddToSourcePath(symFolder);
                DebugApi.AddToSymPath(symFolder);
                DebugApi.IgnoreSymbolMismatch();
            }
            ulong context = IP;
            if (IP == 0)
            {
                context = DebugApi.AddressFromScope;
            }
            OpenSource(context);
            return HRESULTS.S_OK;
        }

        public int OpenSource(ulong Address)
        {
            if (Address == 0)
                return HRESULTS.E_FAIL;
                
            DEBUG_STACK_FRAME nativeFrame = new DEBUG_STACK_FRAME();
            nativeFrame.InstructionOffset = Address;
            StackFrame frame = new StackFrame(nativeFrame);
            if (frame.ManagedModule == null)
            {
                Exports.WriteLine("No valid managed code at {0:%p}", nativeFrame.InstructionOffset);
                return HRESULTS.E_FAIL;
            }
            FileAndLineNumber sourceInfo = frame.ManagedSourceLocation;

            if (String.IsNullOrEmpty(sourceInfo.File))
            {
                Exports.WriteLine("No source file for managed code at {0:%p}", nativeFrame.InstructionOffset);
                return HRESULTS.E_FAIL;
            }
            string filePath = DebugApi.GetSourcePath(sourceInfo.File, frame.ManagedModule.ImageBase, sourceInfo.GetUrlBaseForSource());

            if (String.IsNullOrEmpty(filePath))
            {
                Exports.WriteLine("Unable to resolve local path for file  {0}", sourceInfo.File);
                return HRESULTS.E_FAIL;
            }


            if (!SourceWindow.HasInstance)
            {
                Thread td = new Thread(delegate(object Parameter)
                {
                    IntPtr hwd = Process.GetCurrentProcess().MainWindowHandle;
                    Application app = new Application();

                    object[] pars = Parameter as object[];
                    SourceWindow wnd = SourceWindow.GetInstance(); //
                    WindowInteropHelper helper = new WindowInteropHelper(wnd);
                    helper.Owner = hwd;
                    wnd.LoadFile((string)pars[0]);
                    if ((int)pars[1] > 0)
                        wnd.HighLightLine((string)pars[0], (int)pars[1]);


                    app.Run(wnd);


                });
                td.SetApartmentState(ApartmentState.STA);
                object[] parameters = new object[2];
                parameters[0] = filePath;
                parameters[1] = sourceInfo.Line;
                td.Start(parameters);
                // Wait for a maximum of 1 second
                for (int i = 0; i < 100; i++)
                {
                    Thread.Sleep(10); // Wait until it is rendered for the first time
                    if (SourceWindow.HasDocument)
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            SourceWindow wnd = SourceWindow.GetInstance();
                            wnd.Show();
                            wnd.LoadFile(filePath);
                            if (sourceInfo.Line > 0)
                                wnd.HighLightLine(filePath, sourceInfo.Line);

                            wnd.MoveToLine(filePath, sourceInfo.Line);
                        }), DispatcherPriority.ContextIdle);
                        break;
                    }
                }
            }
            else
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    SourceWindow wnd = SourceWindow.GetInstance();
                    wnd.Show();
                    wnd.LoadFile(filePath);
                    if (sourceInfo.Line > 0)
                        wnd.HighLightLine(filePath, sourceInfo.Line);
                    wnd.MoveToLine(filePath, sourceInfo.Line);
                }), DispatcherPriority.ContextIdle);

            }


            return HRESULTS.S_OK;


        }

        public int GetLineRange(ulong IPAddress, [Out] out IMDSourceMapEnum LineMap)
        {
            DEBUG_STACK_FRAME fr = new DEBUG_STACK_FRAME() { InstructionOffset = IPAddress };
            StackFrame frame = new StackFrame(fr);
            var sourceLocation = frame.SourceLocation;
            if (!sourceLocation.IsManaged || sourceLocation.End == 0)
            {
                LineMap = new MDEnumLineMap(0, 0);
                return HRESULTS.E_FAIL;
            }

            if (IPAddress >= sourceLocation.End) // Resolves a bug in the edge of the code
            {
                fr = new DEBUG_STACK_FRAME() { InstructionOffset = IPAddress + 1};
                frame = new StackFrame(fr);
                sourceLocation = frame.SourceLocation;
                if (!sourceLocation.IsManaged || sourceLocation.End == 0)
                {
                    LineMap = new MDEnumLineMap(0, 0);
                    return HRESULTS.E_FAIL;
                }
            }


            var lines = DebugApi.Disassemble(sourceLocation.Start, sourceLocation.End - 1);
            ulong codeending = sourceLocation.End - 1;
            if (lines.Count > 0)
                codeending = lines[lines.Count - 1].EndOffset;

            LineMap = new MDEnumLineMap(sourceLocation.Start, codeending);


            foreach (var line in lines)
            {
                MD_SourceMap map = new MD_SourceMap()
                {
                    Start = line.StartOffset,
                    End = line.EndOffset,
                    IsJump = line.IsJump,
                    IsCall = line.IsCall,
                    IsManaged = line.IsManagedCall,
                    PointTo = line.PointTo
                };
                ((MDEnumLineMap)LineMap).Add(map);
            }
            return HRESULTS.S_OK;

        }

        public int DumpMixedStack()
        {
            string childSP = "Child-SP".PadRight((int)m_target.PointerSize * 2 + 1);
            string retAddr = "RetAddr".PadRight((int)m_target.PointerSize * 2 + 1);

            Exports.WriteLine("## {0}{1} Call Site", childSP, retAddr);
            foreach (StackFrame frame in DebugApi.StackTrace)
            {
                var sourceLocation = frame.SourceLocation;
                Exports.WriteDml("<link cmd=\".frame {0:x2}{1}\">{0:x2}</link> ", frame.FrameNumber, !sourceLocation.IsManaged || String.IsNullOrWhiteSpace(sourceLocation.File) ? "" : String.Format(";!wopensource 0x{0:x}", sourceLocation.Address));
                Exports.Write(frame.ToString());
                Exports.WriteDml("{0}", frame.SourceLocation.ToString(true));
#if DEBUG
                if (sourceLocation.Start != 0)
                    Exports.WriteDml("<link cmd=\"u {0:%p} {1:%p}\"> {0:%p} {1:%p}</link> ", sourceLocation.Start, sourceLocation.End);
                else
                    Exports.Write(" [no line info]");
#endif
                Exports.WriteLine("");
            }
            return HRESULTS.S_OK;
        }

        public int GetTargetTime(out Int64 TargetTicks, out Int64 UTCTicks, out MD_NETTime TargetTime, out MD_NETTime NETTime)
        {
            var utc = DebugApi.CurrentTimeDate;
            var local = DebugApi.CurrentTimeDateLocal;
            UTCTicks = utc.Ticks;
            TargetTicks = local.Ticks;

            TargetTime = new MD_NETTime()
            {
                Bias = DebugApi.DeteBias,
                Day = local.Day,
                Month = local.Month,
                Year = local.Year,
                Hours = local.Hour,
                Minutes = local.Minute,
                Milisseconds = local.Millisecond
            };

            NETTime = new MD_NETTime()
            {
                Bias = 0,
                Day = utc.Day,
                Month = utc.Month,
                Year = utc.Year,
                Hours = utc.Hour,
                Minutes = utc.Minute,
                Milisseconds = utc.Millisecond
            };

            return TargetTime.Bias == Int32.MaxValue ? HRESULTS.E_FAIL : HRESULTS.S_OK;
        }

        public int DecimalToStr(int lo, int mid, int hi, int flags, out string DecimalStr)
        {
            int[] parts = new int[] { lo, mid, hi, flags };
            Decimal dec = new decimal(parts);
            DecimalStr = dec.ToString();
            return HRESULTS.S_OK;
        }
        
        public int DecimalToDouble(int lo, int mid, int hi, int flags, [Out] out double DecimalDouble)
        {
            int[] parts = new int[] { lo, mid, hi, flags };
            Decimal dec = new decimal(parts);
            DecimalDouble = (double)dec;
            return HRESULTS.S_OK;
        }

        public int TicksToStr(Int64 Ticks, int Bias, [Out] [MarshalAs((UnmanagedType)19)] out string DateStr)
        {
            DateTime date = new DateTime();

            DateStr = date.ToString("MM/dd/yyyy HH:mm:ss.ff");

            return HRESULTS.S_OK;
        }


    }

    [ComVisible(true)]
    [Guid("E2D9461C-0C21-417A-B4BB-95CA826C95A3")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDSourceMapEnum))]
    public class MDEnumLineMap : IMDSourceMapEnum
    {
        protected ulong start;
        protected ulong end;
        private int index;

        internal List<MD_SourceMap> mapList;

        internal MDEnumLineMap(ulong Start, ulong End)
        {
            start = Start;
            end = End;
            mapList = new List<MD_SourceMap>();
            index = 0;
        }

        internal void Add(MD_SourceMap Entry)
        {
            mapList.Add(Entry);
        }

        public int GetRange(out ulong Start, out ulong End)
        {
            Start = start;
            End = end;
            return HRESULTS.S_OK;
        }

        public int GetCount(out int pCount)
        {
            pCount = 0;
            if (mapList == null)
                return HRESULTS.E_FAIL;
            pCount = mapList.Count;
            return HRESULTS.S_OK;
        }

        public int Reset()
        {
            index = 0;
            return HRESULTS.S_OK;
        }

        public int Next(out MD_SourceMap SourceMap)
        {
            
            if (mapList == null || index >= mapList.Count)
            {
                SourceMap = new MD_SourceMap();
                return HRESULTS.E_FAIL;
            }
            SourceMap = mapList[index++];
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("FAF74A71-3B2E-42E4-B740-1E0B4EE69B2A")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDModule))]
    public class MDModule : IMDModule
    {

        internal Module module;

        public MDModule(ulong Address)
        {
            module = new Module(Address);
        }

        public MDModule(string ModuleName)
        {
            module = new Module(ModuleName);
        }

        public MDModule(int Index)
        {
            module = Module.Modules[Index];
        }

        public MDModule(Module CopyModule)
        {
            module = CopyModule;
        }
        public int GetDetails(out MD_Module ModuleDetails)
        {
            ModuleDetails  = new MD_Module();
            ModuleDetails.Address = module.BaseAddress;
            ModuleDetails.ClrDebugType = (int)module.ClrDebugType;
            ModuleDetails.ClrFlags = module.CLRFlags;
            ModuleDetails.ImageDebugType = (int)module.DebugType;
            ModuleDetails.Index = (int)module.Index;
            ModuleDetails.isValid = module.IsValid;
            ModuleDetails.Build = module.VersionInfo.Build;
            ModuleDetails.Major = module.VersionInfo.Major;
            ModuleDetails.Minor = module.VersionInfo.Minor;
            ModuleDetails.metaBuild = module.DotNetVersion.Build;
            ModuleDetails.metaMajor = module.DotNetVersion.Major;
            ModuleDetails.metaMinor = module.DotNetVersion.Minor;
            ModuleDetails.isClr = module.IsClr;

            if (module.IsValid)
                return HRESULTS.S_OK;

            return HRESULTS.E_FAIL;

        }

        public int GetOriginalName(out string OriginalName)
        {
            OriginalName = module.OriginalFilename;

            if (module.IsValid)
                return HRESULTS.S_OK;

            return HRESULTS.E_FAIL;
        }
        public int GetCopyright(out string Copyright)
        {
            Copyright = module.LegalCopyright;

            if (module.IsValid)
                return HRESULTS.S_OK;

            return HRESULTS.E_FAIL;
        }

        public int GetCompanyName(out string Company)
        {
            Company = module.CompanyName;
            if (module.IsValid)
                return HRESULTS.S_OK;

            return HRESULTS.E_FAIL;
        }

        public int GetModuleName(out string ModuleName)
        {
            ModuleName = module.Name;
            if (module.IsValid)
                return HRESULTS.S_OK;

            return HRESULTS.E_FAIL;
        }

        public int GetFullPath(out string FullPath)
        {
            FullPath = module.FullPath;
            if (module.IsValid)
                return HRESULTS.S_OK;

            return HRESULTS.E_FAIL;
        }

        public int SaveModule(string Path)
        {
            if (!module.IsValid)
                return HRESULTS.E_FAIL;

            if (!System.IO.Directory.Exists(Path))
            {
                Exports.WriteLine("Path {0} is invalid or not created", Path);
                return HRESULTS.E_FAIL;
            }
            string curPath = System.IO.Path.Combine(Path, module.Name);
            bool success = false;
            try
            {
                using (Stream fs = File.OpenWrite(curPath))
                {
                    success = module.SaveToStream(fs);
                    Exports.WriteLine("Saved '{0}'", curPath);
                }
            }
            catch (Exception ex)
            {
                Exports.WriteLine("Error when saving file {0}", curPath);
                Exports.WriteLine("{0}", ex.ToString());
                return HRESULTS.E_FAIL;
            }
            if (success)
                return HRESULTS.S_OK;
            return HRESULTS.E_FAIL;
        }
    }
    static class HRESULTS
    {
        internal const int E_FAIL = -1;
        internal const int S_OK = 0;
        internal const int S_FALSE = 1;
    }

    [ComVisible(true)]
    [Guid("45ABE3F9-AB8C-4186-919A-4CB672B452AD")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDStringEnum))]
    public class MDStringEnum : IMDStringEnum
    {
        IList<string> m_data;
        int m_curr;

        public MDStringEnum(IList<string> strings)
        {
            m_data = strings;
        }

        public int GetCount(out int pCount)
        {
            pCount = m_data.Count;
            return HRESULTS.S_OK;
        }

        public int Reset()
        {
            m_curr = 0;
            return HRESULTS.S_OK;
        }

        public int Next(out string pValue)
        {
            if (m_curr < m_data.Count)
            {
                pValue = m_data[m_curr];
                m_curr++;
                return HRESULTS.S_OK;
            }

            pValue = null;
            if (m_curr == m_data.Count)
            {
                m_curr++;
                return HRESULTS.S_FALSE;
            }

            return HRESULTS.E_FAIL;
        }
    }

    [ComVisible(true)]
    [Guid("39943864-7200-4FFC-BC08-01DF351664D0")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDInterfaceEnum))]
    public class InterfaceEnum : IMDInterfaceEnum
    {
        private IList<ClrInterface> m_data;
        private int m_curr;
        public InterfaceEnum(IList<ClrInterface> interfaces)
        {
            m_data = interfaces;
        }

        public int GetCount(out int pCount)
        {
            pCount = m_data.Count;
            return HRESULTS.S_OK;
        }

        public int Reset()
        {
            m_curr = 0;
            return HRESULTS.S_OK;
        }

        public int Next(out IMDInterface pValue)
        {
            if (m_curr < m_data.Count)
            {
                pValue = new MDInterface(m_data[m_curr]);
                m_curr++;
                return HRESULTS.S_OK;
            }

            pValue = null;
            if (m_curr == m_data.Count)
            {
                m_curr++;
                return HRESULTS.S_FALSE;
            }

            return HRESULTS.E_FAIL;
        }
    }

    [ComVisible(true)]
    [Guid("E5E7B8A3-C69F-478F-8309-4E50AD4B4A77")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDCOMInterfaceEnum))]
    public class MDCOMInterfaceEnum : IMDCOMInterfaceEnum
    {
        IList<ComInterfaceData> m_data;
        int m_curr;

        public MDCOMInterfaceEnum(IList<ComInterfaceData> data)
        {
            m_data = data;
        }

        public int GetCount(out int pCount)
        {
            pCount = m_data.Count;
            return HRESULTS.S_OK;
        }

        public int Reset()
        {
            m_curr = 0;
            return HRESULTS.S_OK;
        }

        public int Next(IMDType pType, out ulong pInterfacePtr)
        {
            if (m_curr < m_data.Count)
            {
                pType = new MDType(m_data[m_curr].Type);
                pInterfacePtr = m_data[m_curr].InterfacePointer;
                m_curr++;
                return HRESULTS.S_OK;
            }

            pType = null;
            pInterfacePtr = 0;

            if (m_curr == m_data.Count)
            {
                m_curr++;
                return HRESULTS.S_FALSE;
            }

            return HRESULTS.E_FAIL;
        }
    }

    [ComVisible(true)]
    [Guid("24F9269A-1267-40FE-9980-78CE0B90629C")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDValue))]
    public class MDValue : IMDValue
    {
        object m_value;
        ClrElementType m_cet;

        public MDValue(object value, ClrElementType cet)
        {
            m_value = value;
            m_cet = cet;

            if (m_value == null)
            {
                m_cet = ClrElementType.Unknown;
                return;
            }
            switch (m_cet)
            {
                case ClrElementType.NativeUInt:  // native unsigned int
                case ClrElementType.Pointer:
                case ClrElementType.FunctionPointer:
                    m_cet = ClrElementType.UInt64;
                    break;
                   
                case ClrElementType.String:
                    if (m_value == null)
                        m_cet = ClrElementType.Unknown;
                    break;
        
                case ClrElementType.Class:
                case ClrElementType.Array:
                case ClrElementType.SZArray:
                    m_cet = ClrElementType.Object;
                    break;
            }
        }

        public int IsNull(out int pNull)
        {
            pNull = (m_cet == ClrElementType.Unknown || m_value == null ) ? 1 : 0;
            return HRESULTS.S_OK;
        }


        public int GetElementType(out int pCET)
        {
            pCET = (int)m_cet;
            return HRESULTS.S_OK;
        }

        public int GetInt32(out int pValue)
        {
            ulong value = GetValue64();
            pValue = (int)value;
            return HRESULTS.S_OK;
        }

        private ulong GetValue64()
        {
            if (m_value is int)
                return (ulong)(int)m_value;

            else if (m_value is uint)
                return (ulong)(uint)m_value;
            
            else if (m_value is long)
                return (ulong)(long)m_value;

            return (ulong)m_value;
        }

        public int GetUInt32(out uint pValue)
        {
            ulong value = GetValue64();
            pValue = (uint)value;
            return HRESULTS.S_OK;
        }

        public int GetInt64(out long pValue)
        {
            ulong value = GetValue64();
            pValue = (long)value;
            return HRESULTS.S_OK;
        }

        public int GetUInt64(out ulong pValue)
        {
            ulong value = GetValue64();
            pValue = (ulong)value;
            return HRESULTS.S_OK;
        }

        public int GetString(out string pValue)
        {
            pValue = (string)m_value;
            return HRESULTS.S_OK;
        }


        public int GetBool(out int pBool)
        {
            if (m_value is bool)
                pBool = ((bool)m_value) ? 1 : 0;
            else
                pBool = (int)GetValue64();
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("50CF4D7E-CD6C-425E-ACE6-146F34857DE2")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDObjectEnum))]
    public class ObjectSegmentEnum : IMDObjectEnum
    {
        ClrSegment m_seg;
        ulong m_obj = 0;
        bool m_done = false;

        public ObjectSegmentEnum(ClrSegment seg)
        {
            m_seg = seg;
            if(seg != null)
                m_obj = seg.FirstObject;
        }

        public int Next(int count, ulong[] refs, out int pWrote)
        {
            if (m_obj == 0 && !m_done)
            {
                m_done = true;
                pWrote = 0;
                return HRESULTS.S_FALSE;
            }

            if (m_done)
            {
                pWrote = 0;
                return HRESULTS.E_FAIL;
            }

            int wrote = 0;

            while (wrote < count && m_obj != 0)
            {
                refs[wrote++] = m_obj;
                m_obj = m_seg.NextObject(m_obj);
            }

            pWrote = wrote;
            if (wrote < count || m_obj == 0)
            {
                m_done = true;
                return HRESULTS.S_FALSE;
            }

            return HRESULTS.S_OK;
        }

        public int GetCount(out int pCount)
        {
            pCount = 0;
            return HRESULTS.E_FAIL;
        }

        public int Clear()
        {
            return HRESULTS.E_FAIL;
        }

        public int Reset()
        {
            m_obj = 0;
            m_done = true;
            return HRESULTS.S_OK;
        }

        public int AddAddress(ulong ObjRef)
        {
            return HRESULTS.E_FAIL; 
        }
    }

    [ComVisible(true)]
    [Guid("8E3205CA-BDA3-4AD6-9BA6-177D91A712A0")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDObjectEnum))]
    public class MDObjectEnum : IMDObjectEnum
    {
        IList<ulong> m_refs;
        int m_curr;

        public MDObjectEnum()
        {
            m_refs = new List<ulong>();
            m_curr = 0;
        }

        public IList<ulong> List
        {
            get
            {
                return m_refs;
            }
        }
        

        public MDObjectEnum(IList<ulong> refs)
        {
            m_refs = refs;
        }

        public int GetCount(out int pCount)
        {
            pCount = m_refs.Count;
            return HRESULTS.S_OK;
        }

        public int Next(int count, ulong[] refs, out int pWrote)
        {
            if (m_curr == m_refs.Count)
            {
                m_curr = m_refs.Count + 1;
                pWrote = 0;
                return HRESULTS.S_FALSE;
            }
            else if (m_curr > m_refs.Count)
            {
                pWrote = 0;
                return HRESULTS.E_FAIL;
            }

            int i;
            for (i = 0; m_curr < m_refs.Count && i < count; ++i, ++m_curr)
                refs[i] = m_refs[m_curr];

            pWrote = i;
            if (i != count)
            {
                m_curr = m_refs.Count + 1;
                return HRESULTS.S_FALSE;
            }

            return HRESULTS.S_OK;
        }

        public int Reset()
        {
            m_curr = 0;
            return HRESULTS.S_OK;
        }

        public int Clear()
        {
            m_curr = 0;
            m_refs.Clear();
            return HRESULTS.S_OK;
        }

        public int AddAddress(ulong ObjRef)
        {
            m_refs.Add(ObjRef);
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("023771A8-6B4D-4FB9-B73C-450EBCDAA63F")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDAppDomainEnum))]
    public class MDAppDomainEnum : IMDAppDomainEnum
    {
        IList<ClrAppDomain> m_refs;
        int m_curr;

        public MDAppDomainEnum(IList<ClrAppDomain> refs)
        {
            m_refs = refs;
        }

        public int GetCount(out int pCount)
        {
            pCount = m_refs.Count;
            return HRESULTS.S_OK;
        }

        public int Next(out IMDAppDomain ppAppDomain)
        {
            if (m_curr < m_refs.Count)
            {
                ppAppDomain = new MDAppDomain(m_refs[m_curr++]);
                return HRESULTS.S_OK;
            }
            else if (m_curr == m_refs.Count)
            {
                m_curr++;
                ppAppDomain = null;
                return HRESULTS.S_FALSE;
            }

            ppAppDomain = null;
            return HRESULTS.E_FAIL;
        }

        public int Reset()
        {
            m_curr = 0;
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("A314C23A-E6DD-4C99-86AD-C7863E369AC5")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDReferenceEnum))]
    public class ReferenceEnum : IMDReferenceEnum
    {
        private IList<MD_Reference> m_refs;
        int m_curr;
        public ReferenceEnum(IList<MD_Reference> refs)
        {
            m_refs = refs;
        }

        public int GetCount(out int pCount)
        {
            pCount = m_refs.Count;
            return HRESULTS.S_OK;
        }

        public int Reset()
        {
            m_curr = 0;
            return HRESULTS.S_OK;
        }

        public int Next(int count, MD_Reference[] refs, out int pWrote)
        {
            if (m_curr == m_refs.Count)
            {
                m_curr = m_refs.Count + 1;
                pWrote = 0;
                return HRESULTS.S_FALSE;
            }
            else if (m_curr > m_refs.Count)
            {
                pWrote = 0;
                return HRESULTS.E_FAIL;
            }

            int i;
            for (i = 0; m_curr < m_refs.Count && i < count; ++i, ++m_curr)
                refs[i] = m_refs[m_curr];

            pWrote = i;
            if (i != count)
            {
                m_curr = m_refs.Count + 1;
                return HRESULTS.S_FALSE;
            }

            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("7D768065-C131-4EC5-AC0A-BD826A28D7A6")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDSegmentEnum))]
    public class MDSegmentEnum : IMDSegmentEnum
    {
        IList<ClrSegment> m_segments;
        int m_curr;

        public MDSegmentEnum(IList<ClrSegment> segments)
        {
            m_segments = segments;
        }

        public int GetCount(out int pCount)
        {
            pCount = m_segments.Count;
            return HRESULTS.S_OK;
        }

        public int Next(out IMDSegment ppSegment)
        {
            if (m_curr < m_segments.Count)
            {
                ppSegment = new MDSegment(m_segments[m_curr++]);
                return HRESULTS.S_OK;
            }
            else if (m_curr == m_segments.Count)
            {
                m_curr++;
                ppSegment = null;
                return HRESULTS.S_FALSE;
            }

            ppSegment = null;
            return HRESULTS.E_FAIL;
        }

        public int Reset()
        {
            m_curr = 0;
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("D9A9B29A-2CBD-4096-A865-2883B8B2D17A")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDRootEnum))]
    public class MDRootEnum : IMDRootEnum
    {
        IList<ClrRoot> m_roots;
        int m_curr = 0;

        public MDRootEnum(IList<ClrRoot> roots)
        {
            m_roots = roots;
        }

        public int GetCount(out int pCount)
        {
            pCount = m_curr;
            return HRESULTS.S_OK;
        }

        public int Next(out IMDRoot ppRoot)
        {
            ppRoot = null;
            if(m_roots == null)
                return HRESULTS.E_FAIL;
            if (m_curr < m_roots.Count)
            {
                ppRoot = new MDRoot(m_roots[m_curr]);
                return HRESULTS.S_OK;
            }
            else if (m_curr == m_roots.Count)
            {
                ppRoot = null;
                return HRESULTS.S_FALSE;
            }

            ppRoot = null;
            return HRESULTS.E_FAIL;
        }

        public int Reset()
        {
            m_curr = 0;
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("1B520581-78A3-4DB2-B677-732B312740E6")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDStackTraceEnum))]
    public class MDStackTraceEnum : IMDStackTraceEnum
    {
        IList<ClrStackFrame> m_frames;
        int m_curr = 0;

        public MDStackTraceEnum(IList<ClrStackFrame> frames)
        {
            m_frames = frames;
        }

        public int GetCount(out int pCount)
        {
            pCount = 0;
            if(m_frames == null)
                return HRESULTS.E_FAIL;
            pCount = m_frames.Count;
            return HRESULTS.S_OK;
        }

        public int Next(out ulong pIP, out ulong pSP, out string pFunction)
        {
            pIP = 0;
            pSP = 0;
            pFunction = null;
            if(m_frames == null)
                return HRESULTS.E_FAIL;
            if (m_curr < m_frames.Count)
            {
                ClrStackFrame frame = m_frames[m_curr++];
                pIP = frame.InstructionPointer;
                pSP = frame.StackPointer;
                pFunction = frame.DisplayString;
                return HRESULTS.S_OK;
            }

            m_curr++;
            pIP = 0;
            pSP = 0;
            pFunction = null;
            return (m_curr == m_frames.Count) ? HRESULTS.S_FALSE : HRESULTS.E_FAIL;
        }

        public int Reset()
        {
            m_curr = 0;
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("6DEDC2CB-2680-4D23-A945-D9C2C083A02F")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDHandleEnum))]
    public class MDHandleEnum : IMDHandleEnum
    {
        IList<ClrHandle> m_handles;
        int m_curr;

        public MDHandleEnum(IEnumerable<ClrHandle> handles)
        {
            m_handles = new List<ClrHandle>(handles);
        }

        public int GetCount(out int pCount)
        {
            pCount = 0;
            if(m_handles == null)
                return HRESULTS.E_FAIL;
            pCount = m_handles.Count();
            return HRESULTS.S_OK;
        }

        public int Next(out IMDHandle ppHandle)
        {
            ppHandle = null;
            if (m_handles == null)
                return HRESULTS.E_FAIL;
            if (m_curr < m_handles.Count)
            {
                ppHandle = new MDHandle(m_handles[m_curr++]);
                return HRESULTS.S_OK;
            }
            else if (m_curr == m_handles.Count)
            {
                m_curr++;
                ppHandle = null;
                return HRESULTS.S_FALSE;
            }

            ppHandle = null;
            return HRESULTS.E_FAIL;
        }

        public int Reset()
        {
            m_curr = 0;
            return HRESULTS.S_OK;
        }
    }

    [ComVisible(true)]
    [Guid("CF957B59-9D01-406E-A7F0-5CFD5D4924CD")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDMemoryRegionEnum))]
    public class MDMemoryRegionEnum : IMDMemoryRegionEnum
    {
        IList<ClrMemoryRegion> m_regions;
        int m_curr;

        public MDMemoryRegionEnum(IList<ClrMemoryRegion> regions)
        {
            m_regions = regions;
        }

        public int GetCount(out int pCount)
        {
            pCount = 0;
            if (m_regions == null)
                return HRESULTS.E_FAIL;
            pCount = m_regions.Count;
            return HRESULTS.S_OK;
        }

        public int Next(out IMDMemoryRegion ppRegion)
        {
            ppRegion = null;
            if(m_regions == null)
                return HRESULTS.E_FAIL;
            if (m_curr < m_regions.Count)
            {
                ppRegion = new MDMemoryRegion(m_regions[m_curr++]);
                return HRESULTS.S_OK; ;
            }
            else if (m_curr == m_regions.Count)
            {
                m_curr++;
                ppRegion = null;
                return HRESULTS.S_FALSE;
            }

            ppRegion = null;
            return HRESULTS.E_FAIL;
        }

        public int Reset()
        {
            return HRESULTS.E_FAIL;
        }
    }

    [ComVisible(true)]
    [Guid("698032FD-92E9-4F74-9C5B-C0A5F961D97E")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDThreadEnum))]
    public class MDThreadEnum : IMDThreadEnum
    {
        IList<ClrThread> m_threads;
        int m_curr = 0;
        ClrRuntime m_runtime = null;
        public MDThreadEnum(IList<ClrThread> threads, ClrRuntime runtime)
        {
            m_threads = threads;
            m_runtime = runtime;
        }

        public int GetCount(out int pCount)
        {
            pCount = 0;
            if (m_threads == null)
                return HRESULTS.E_FAIL;
            pCount = m_threads.Count;
            return HRESULTS.S_OK;
        }

        public int Next(out IMDThread ppThread)
        {
            ppThread = null;
            if(m_threads == null)
                return HRESULTS.E_FAIL;
            if (m_curr < m_threads.Count)
            {
                ppThread = new MDThread(m_threads[m_curr++], m_runtime);
                return HRESULTS.S_OK;
            }
            else if (m_curr == m_threads.Count)
            {
                m_curr++;
                ppThread = null;
                return HRESULTS.S_FALSE;
            }

            ppThread = null;
            return HRESULTS.E_FAIL;
        }

        public int Reset()
        {
            m_curr = 0;
            return HRESULTS.S_OK;
        }
    }
}
