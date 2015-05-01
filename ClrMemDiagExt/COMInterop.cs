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
            if (callBack != null)
                callBack(Message);
            //else
            //    throw new NullReferenceException("C++ plain text callback was not set");
        }

        public static void OutDml(string Message)
        {
            if (callBackDml != null)
                callBackDml(Message);
            //else
            //    throw new NullReferenceException("C++ formatted text callback was not set");

        }

        private static string netExtPath = null;

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
                AppDomain.CurrentDomain.UnhandledException += ad_UnhandledException;
                AppDomain.CurrentDomain.AssemblyResolve += ad_AssemblyResolve;
            }
#if _DEBUG
            WriteLine("Base folder = {0}\n", path);
#endif
            

            try
            {
                CLRMDActivator clrMD = new CLRMDActivator();
                clrMD.CreateFromIDebugClient(iDebugClient, out ppTarget);
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
        }

        static void ad_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
            {
                WriteLine("\nFatal Exception in NetExtShim");
            }
            if(e != null && e.ExceptionObject != null)
            {
                WriteLine("\nUnhandled exception in NetExtShim: {0}",e.ExceptionObject.GetType().ToString());

                Exception ex = e.ExceptionObject as Exception;
                if (ex != null)
                {
                    WriteLine("Message : {0}", ex.Message);
                    WriteLine("at {0}", ex.StackTrace);
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
        public void CreateFromCrashDump(string crashdump, out IMDTarget ppTarget)
        {
            ppTarget = new MDTarget(crashdump);
        }

        public void CreateFromIDebugClient(object iDebugClient, out IMDTarget ppTarget)
        {
            ppTarget = new MDTarget(iDebugClient);
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

        public void GetName(out string pName)
        {
            pName = m_heapint.Name;
        }

        public void GetBaseInterface(out IMDInterface ppBase)
        {
            if (m_heapint.BaseInterface != null)
                ppBase = new MDInterface(m_heapint.BaseInterface);
            else
                ppBase = null;
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

        public void GetHandleData(out ulong pAddr, out ulong pObjRef, out MDHandleTypes pType)
        {
            pAddr = m_handle.Address;
            pObjRef = m_handle.Object;
            pType = (MDHandleTypes)m_handle.HandleType;
        }

        public void IsStrong(out int pStrong)
        {
            pStrong = m_handle.IsStrong ? 1 : 0;
        }

        public void GetRefCount(out int pRefCount)
        {
            pRefCount = (int)m_handle.RefCount;
        }

        public void GetDependentTarget(out ulong pTarget)
        {
            pTarget = m_handle.DependentTarget;
        }

        public void GetAppDomain(out IMDAppDomain ppDomain)
        {
            if (m_handle.AppDomain != null)
                ppDomain = new MDAppDomain(m_handle.AppDomain);
            else
                ppDomain = null;
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

        public void GetRootInfo(out ulong pAddress, out ulong pObjRef, out MDRootType pType)
        {
            pAddress = m_root.Address;
            pObjRef = m_root.Object;
            pType = (MDRootType)m_root.Kind;
        }

        public void GetType(out IMDType ppType)
        {
            ppType = null;
        }

        public void GetName(out string ppName)
        {
            ppName = m_root.Name;
        }

        public void GetAppDomain(out IMDAppDomain ppDomain)
        {
            if (m_root.AppDomain != null)
                ppDomain = new MDAppDomain(m_root.AppDomain);
            else
                ppDomain = null;
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

        public void GetName(out string pName)
        {
            pName = m_appDomain.Name;
        }

        public void GetID(out int pID)
        {
            pID = m_appDomain.Id;
        }

        public void GetAddress(out ulong pAddress)
        {
            pAddress = m_appDomain.Address;
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

        public void GetSegData(out MD_SegData SegData)
        {
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
        }

        public void GetStart(out ulong pAddress)
        {
            pAddress = m_seg.Start;
        }

        public void GetEnd(out ulong pAddress)
        {
            pAddress = m_seg.End;
        }

        public void GetReserveLimit(out ulong pAddress)
        {
            pAddress = m_seg.ReservedEnd;
        }

        public void GetCommitLimit(out ulong pAddress)
        {
            pAddress = m_seg.CommittedEnd;
        }

        public void GetLength(out ulong pLength)
        {
            pLength = m_seg.Length;
        }

        public void GetProcessorAffinity(out int pProcessor)
        {
            pProcessor = m_seg.ProcessorAffinity;
        }

        public void IsLarge(out int pLarge)
        {
            pLarge = m_seg.IsLarge ? 1 : 0;
        }

        public void IsEphemeral(out int pEphemeral)
        {
            pEphemeral = m_seg.IsEphemeral ? 1 : 0;
        }

        public void GetGen0Info(out ulong pStart, out ulong pLen)
        {
            pStart = m_seg.Gen0Start;
            pLen = m_seg.Gen0Length;
        }

        public void GetGen1Info(out ulong pStart, out ulong pLen)
        {
            pStart = m_seg.Gen1Start;
            pLen = m_seg.Gen1Length;
        }

        public void GetGen2Info(out ulong pStart, out ulong pLen)
        {
            pStart = m_seg.Gen2Start;
            pLen = m_seg.Gen2Length;
        }

        public void EnumerateObjects(out IMDObjectEnum ppEnum)
        {
            List<ulong> refs = new List<ulong>();
            for (ulong obj = m_seg.FirstObject; obj != 0; obj = m_seg.NextObject(obj))
                refs.Add(obj);

            ppEnum = new MDObjectEnum(refs);
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

        public void GetRegionInfo(out ulong pAddress, out ulong pSize, out MDMemoryRegionType pType)
        {
            pAddress = m_region.Address;
            pSize = m_region.Size;
            pType = (MDMemoryRegionType)m_region.Type;
        }

        public void GetAppDomain(out IMDAppDomain ppDomain)
        {
            if (m_region.AppDomain != null)
                ppDomain = new MDAppDomain(m_region.AppDomain);
            else
                ppDomain = null;
        }

        public void GetModule(out string pModule)
        {
            pModule = m_region.Module;
        }

        public void GetHeapNumber(out int pHeap)
        {
            pHeap = m_region.HeapNumber;
        }

        public void GetDisplayString(out string pName)
        {
            pName = m_region.ToString(true);
        }

        public void GetSegmentType(out MDSegmentType pType)
        {
            pType = (MDSegmentType)m_region.GCSegmentType;
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

        public void GetThreadData([Out] out MD_ThreadData threadData)
        {
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
        }

        public void GetAddress(out ulong pAddress)
        {
            pAddress = m_thread.Address;
        }

        public void IsFinalizer(out int pIsFinalizer)
        {
            pIsFinalizer = m_thread.IsFinalizer ? 1 : 0;
        }

        public void IsAlive(out int pIsAlive)
        {
            pIsAlive = m_thread.IsAlive ? 1 : 0;
        }

        public void GetOSThreadId(out int pOSThreadId)
        {
            pOSThreadId = (int)m_thread.OSThreadId;
        }

        public void GetAppDomainAddress(out ulong pAppDomain)
        {
            pAppDomain = m_thread.AppDomain;
        }

        public void GetLockCount(out int pLockCount)
        {
            pLockCount = (int)m_thread.LockCount;
        }

        public void GetCurrentException(out IMDException ppException)
        {
            if (m_thread.CurrentException != null)
                ppException = new MDException(m_thread.CurrentException);
            else
                ppException = null;
        }

        public void GetTebAddress(out ulong pTeb)
        {
            pTeb = m_thread.Teb;
        }

        public void GetStackLimits(out ulong pBase, out ulong pLimit)
        {
            pBase = m_thread.StackBase;
            pLimit = m_thread.StackLimit;
        }

        public void EnumerateStackTrace(out IMDStackTraceEnum ppEnum)
        {
            ppEnum = new MDStackTraceEnum(m_thread.StackTrace);
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

        public void GetIUnknown(out ulong pIUnk)
        {
            pIUnk = m_rcw.IUnknown;
        }

        public void GetObject(out ulong pObject)
        {
            pObject = m_rcw.Object;
        }

        public void GetRefCount(out int pRefCnt)
        {
            pRefCnt = m_rcw.RefCount;
        }

        public void GetVTable(out ulong pHandle)
        {
            pHandle = m_rcw.VTablePointer;
        }

        public void IsDisconnected(out int pDisconnected)
        {
            pDisconnected = m_rcw.Disconnected ? 1 : 0;
        }

        public void EnumerateInterfaces(out IMDCOMInterfaceEnum ppEnum)
        {
            ppEnum = new MDCOMInterfaceEnum(m_rcw.Interfaces);
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

        public void GetIUnknown(out ulong pIUnk)
        {
            pIUnk = m_ccw.IUnknown;
        }

        public void GetObject(out ulong pObject)
        {
            pObject = m_ccw.Object;
        }

        public void GetHandle(out ulong pHandle)
        {
            pHandle = m_ccw.Handle;
        }

        public void GetRefCount(out int pRefCnt)
        {
            pRefCnt = m_ccw.RefCount;
        }

        public void EnumerateInterfaces(out IMDCOMInterfaceEnum ppEnum)
        {
            ppEnum = new MDCOMInterfaceEnum(m_ccw.Interfaces);
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

        public void GetName(out string pName)
        {
            pName = m_field.Name;
        }

        public void GetType(out IMDType ppType)
        {
            ppType = MDType.Construct(m_field.Type);
        }

        public void GetElementType(out int pCET)
        {
            pCET = (int)m_field.ElementType;
        }

        public void GetSize(out int pSize)
        {
            pSize = m_field.Size;
        }

        public void GetFieldValue(IMDAppDomain appDomain, out IMDValue ppValue)
        {
            object value = m_field.GetFieldValue((ClrAppDomain)appDomain);
            ppValue = new MDValue(value, m_field.ElementType);
        }

        public void GetFieldAddress(IMDAppDomain appDomain, out ulong pAddress)
        {
            ulong addr = m_field.GetAddress((ClrAppDomain)appDomain);
            pAddress = addr;
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

        public void GetName(out string pName)
        {
            pName = m_field.Name;
        }

        public void GetType(out IMDType ppType)
        {
            ppType = MDType.Construct(m_field.Type);
        }

        public void GetElementType(out int pCET)
        {
            pCET = (int)m_field.ElementType;
        }

        public void GetSize(out int pSize)
        {
            pSize = m_field.Size;
        }

        public void GetFieldValue(IMDAppDomain appDomain, IMDThread thread, out IMDValue ppValue)
        {
            object value = m_field.GetFieldValue((ClrAppDomain)appDomain, (ClrThread)thread);
            ppValue = new MDValue(value, m_field.ElementType);
        }

        public void GetFieldAddress(IMDAppDomain appDomain, IMDThread thread, out ulong pAddress)
        {
            pAddress = m_field.GetFieldAddress((ClrAppDomain)appDomain, (ClrThread)thread);
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

        public void GetName(out string pName)
        {
            pName = m_field.Name;
        }

        public void GetType(out IMDType ppType)
        {
            ppType = MDType.Construct(m_field.Type);
        }

        public void GetElementType(out int pCET)
        {
            pCET = (int)m_field.ElementType;
        }

        public void GetSize(out int pSize)
        {
            pSize = m_field.Size;
        }

        public void GetOffset(out int pOffset)
        {
            pOffset = m_field.Offset;
        }

        public void GetFieldValue(ulong objRef, int interior, out IMDValue ppValue)
        {
            object value = m_field.GetFieldValue(objRef, interior != 0);
            ppValue = new MDValue(value, m_field.ElementType);
        }

        public void GetFieldAddress(ulong objRef, int interior, out ulong pAddress)
        {
            pAddress = m_field.GetAddress(objRef, interior != 0);
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

        public void GetIMetadata([Out] [MarshalAs((UnmanagedType)25)] out object IMetadata)
        {
            IMetadata = m_type.Module.MetadataImport;
        }


        public void GetHeader(ulong objRef, out MD_TypeData typeData)
        {
            typeData = new MD_TypeData();
            if (m_type.BaseType != null)
                typeData.parentMT = HeapStatItem.GetMTOfType(m_type.BaseType);
            else
                typeData.parentMT = 0;
            typeData.BaseSize = (ulong)m_type.BaseSize;
            if (objRef != 0)
            {
                typeData.size = m_type.GetSize(objRef);
                if(!m_type.IsObjectReference || !m_type.Heap.GetRuntime().ReadPointer(objRef, out typeData.MethodTable))
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
                typeData.appDomain = AdHoc.GetDomainFromMT(m_type.Heap.GetRuntime(), typeData.MethodTable);
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
                    
                    ulong[] arrayData = AdHoc.GetArrayData(m_type.Heap.GetRuntime(), objRef);
                    if (m_type.ArrayComponentType == null)
                        typeData.arrayCorType = (int)arrayData[AdHoc.ARRAYCORTYPE];
                    else
                        typeData.arrayCorType = (int)m_type.ArrayComponentType.ElementType;
                    typeData.arrayElementMT = arrayData[AdHoc.ARRAYELEMENTMT];
                    typeData.arrayStart = arrayData[AdHoc.ARRAYSTART];

                    typeData.arraySize = m_type.GetArrayLength(objRef);
                }
            }
            typeData.corElementType = (int)m_type.ElementType;
            typeData.isArray = m_type.IsArray;
            typeData.isGeneric = m_type.Name.Contains('<') && m_type.Name.Contains('>') && m_type.Name[0] != '<';
            typeData.isString = m_type.IsString;
            typeData.EEClass = AdHoc.GetEEFromMT(m_type.Heap.GetRuntime(), typeData.MethodTable);
            typeData.isValueType = m_type.IsValueClass;
            typeData.module = m_type.Module.ImageBase;
            typeData.assembly = m_type.Module.AssemblyId;
            typeData.containPointers = m_type.ContainsPointers;
 
            return;
        }

        public void GetString(ulong ObjRef, [Out] [MarshalAs((UnmanagedType)19)] out string strValue)
        {
            if(ObjRef == 0)
            {
                strValue = null;
                return;
            }
            
            ClrType strType = m_type.Heap.GetObjectType(ObjRef);
            
            
            if (strType != null && strType.IsString)
                strValue = strType.GetValue(ObjRef) as String;
            else
                strValue = null;
        }

        public void GetFilename([Out] [MarshalAs((UnmanagedType)19)] out string fileName)
        {
            if (!m_type.Module.IsFile)
            {
                fileName = "(dynamic)";
                return;
            }
            fileName = m_type.Module.FileName;
        }
    

        public void GetName(out string pName)
        {
            pName = m_type.Name;
        }

        public void GetRuntimeName(ulong objRef, out string pName)
        {
            if (!m_type.IsRuntimeType)
            {
                pName = null;
                return;
            }
            ClrType runtimeType = m_type.GetRuntimeType(objRef);
            if (runtimeType != null)
            {
                pName = runtimeType.Name;
                return;
            }

            pName = null;
        }

        public void GetSize(ulong objRef, out ulong pSize)
        {
            pSize = m_type.GetSize(objRef);
        }

        public void ContainsPointers(out int pContainsPointers)
        {
            pContainsPointers = m_type.ContainsPointers ? 1 : 0;
        }

        public void GetCorElementType(out int pCET)
        {
            pCET = (int)m_type.ElementType;
        }

        public void GetBaseType(out IMDType ppBaseType)
        {
            ppBaseType = Construct(m_type.BaseType);
        }

        public void GetArrayComponentType(out IMDType ppArrayComponentType)
        {
            ppArrayComponentType = Construct(m_type.ArrayComponentType);
        }

        public void GetCCW(ulong addr, out IMDCCW ppCCW)
        {
            if (m_type.IsCCW(addr))
                ppCCW = new MDCCW(m_type.GetCCWData(addr));
            else
                ppCCW = null;
        }

        public void GetRCW(ulong addr, out IMDRCW ppRCW)
        {
            if (m_type.IsRCW(addr))
                ppRCW = new MDRCW(m_type.GetRCWData(addr));
            else
                ppRCW = null;
        }

        public void IsArray(out int pIsArray)
        {
            pIsArray = m_type.IsArray ? 1 : 0;
        }

        public void IsFree(out int pIsFree)
        {
            pIsFree = m_type.IsFree ? 1 : 0;
        }

        public void IsException(out int pIsException)
        {
            pIsException = m_type.IsException ? 1 : 0;
        }

        public void IsEnum(out int pIsEnum)
        {
            pIsEnum = m_type.IsEnum ? 1 : 0;
        }

        public void GetEnumElementType(out int pValue)
        {
            pValue = (int)m_type.GetEnumElementType();
        }

        public void GetEnumNames(out IMDStringEnum ppEnum)
        {
            ppEnum = new MDStringEnum(m_type.GetEnumNames().ToArray());
        }

        public void GetEnumValueInt32(string name, out int pValue)
        {
            if (!m_type.TryGetEnumValue(name, out pValue))
                new InvalidOperationException("Mismatched type.");
        }

        public void GetAllFieldsDataRawCount(out int pCount)
        {
            pCount = m_type.Fields.Count + m_type.StaticFields.Count
                + m_type.ThreadStaticFields.Count;
        }

        public int GetAllFieldsDataRaw(int valueType, int count, MD_FieldData[] fields, out int pNeeded)
        {
            int total =  m_type.Fields.Count + m_type.StaticFields.Count
                + m_type.ThreadStaticFields.Count;
            if (fields == null || count == 0 || count < total)
            {
                pNeeded = total;
                return 1; // S_FALSE
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

            return 0; // S_OK
        
        }

        public void GetFieldCount(out int pCount)
        {
            pCount = m_type.Fields.Count;
        }

        public void GetField(int index, out IMDField ppField)
        {
            ppField = new MDField(m_type.Fields[index]);
        }

        public int GetRawFieldAddress(ulong obj, int interior, int index, [Out] out ulong address)
        {

            var fields = CacheFieldInfo.Fields(m_type);
            address = 0;
            if (index < fields.Count)
            {
                if (!fields[index].IsStatic && !fields[index].IsThreadStatic)
                {
                    ClrInstanceField instField = fields[index].BackField as ClrInstanceField;
                    if (instField != null)
                    {
                        address = instField.GetAddress(obj, interior != 0);
                        return 0; // S_OK
                    }
                    return 1; // S_FALSE

                }
                if (fields[index].IsThreadStatic)
                {
                    ClrThreadStaticField threadStat = fields[index].BackField as ClrThreadStaticField;
                    if (threadStat != null)
                    {
                        foreach (var thread in m_type.Heap.GetRuntime().Threads)
                        {
                            foreach (var domain in AdHoc.GetDomains(m_type.Heap.GetRuntime()))
                            {
                                try
                                {
                                    address = threadStat.GetAddress(domain, thread);
                                    if (address != 0)
                                    {
                                        return 0; // S_OK
                                    }
                                }
                                catch
                                {
                                    // this may be expected
                                }
                            }
                        }
                    }
                    return 1; // S_FALSE
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
                        ulong domainAddr = AdHoc.GetDomainFromMT(m_type.Heap.GetRuntime(), mt);
                        if (domainAddr != 0)
                        {
                            domain = AdHoc.GetDomainByAddress(m_type.Heap.GetRuntime(), domainAddr);
                            if (domain != null)
                            {
                                address = stat.GetAddress(domain);
                                if (address != 0)
                                        return 0; // S_OK
                            }
                            foreach(var d in AdHoc.GetDomains(m_type.Heap.GetRuntime()))
                            {
                                address = stat.GetAddress(d);
                                if (address != 0)
                                {
                                    //Exports.WriteLine("\nFrom Managed: {0:x16}={1} \n\n", address,
                                    //    stat.GetValue(d));
                                    if(stat.GetValue(d) != null)
                                        return 0; // S_OK
                                }
                            }
                        }

                    }
                    

                    return 1; // S_FALSE
                }

                return 1; //S_FALSE
            }



            address = 0;
            return 1; // S_FALSE
        }

        public void GetRawFieldTypeAndName(int index, out string pType, out string pName)
        {
            var fields = CacheFieldInfo.Fields(m_type);

            if (index < fields.Count)
            {
                pType = fields[index].TypeName;
                pName = fields[index].Name;
                return;
            }

            pType = String.Empty;
            pName = String.Empty;
        }

        public void GetEnumName(ulong value, out string enumString)
        {
            enumString = AdHoc.GetEnumName(m_type, value);
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
                    fields[i].value = field.GetFieldAddress(obj, interior != 0);
                }
                else
                {
                    object value = field.GetFieldValue(obj, interior != 0);

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

        public void GetStaticFieldCount(out int pCount)
        {
            pCount = m_type.StaticFields.Count;
        }

        public void GetStaticField(int index, out IMDStaticField ppStaticField)
        {
            ppStaticField = new MDStaticField(m_type.StaticFields[index]);
        }

        public void GetThreadStaticFieldCount(out int pCount)
        {
            pCount = m_type.ThreadStaticFields.Count;
        }

        public void GetThreadStaticField(int index, out IMDThreadStaticField ppThreadStaticField)
        {
            ppThreadStaticField = new MDThreadStaticField(m_type.ThreadStaticFields[index]);
        }

        public void GetArrayLength(ulong objRef, out int pLength)
        {
            pLength = m_type.GetArrayLength(objRef);
        }

        public void GetArrayElementAddress(ulong objRef, int index, out ulong pAddr)
        {
            pAddr = m_type.GetArrayElementAddress(objRef, index);
        }

        public void GetArrayElementValue(ulong objRef, int index, out IMDValue ppValue)
        {
            object value = m_type.GetArrayElementValue(objRef, index);
            ClrElementType elementType = m_type.ArrayComponentType != null ? m_type.ArrayComponentType.ElementType : ClrElementType.Unknown;
            ppValue = new MDValue(value, elementType);
        }


        public void EnumerateReferences(ulong objRef, out IMDReferenceEnum ppEnum)
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
        }

        public void EnumerateInterfaces(out IMDInterfaceEnum ppEnum)
        {
            ppEnum = new InterfaceEnum(m_type.Interfaces);
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

        void IMDException.GetGCHeapType(out IMDType ppType)
        {
            ppType = new MDType(m_ex.Type);
        }

        void IMDException.GetErrorMessage(out string pMessage)
        {
            pMessage = m_ex.Message;
        }

        void IMDException.GetObjectAddress(out ulong pAddress)
        {
            pAddress = m_ex.Address;
        }

        void IMDException.GetInnerException(out IMDException ppException)
        {
            if (m_ex.Inner != null)
                ppException = new MDException(m_ex.Inner);
            else
                ppException = null;
        }

        void IMDException.GetHRESULT(out int pHResult)
        {
            pHResult = m_ex.HResult;
        }

        void IMDException.EnumerateStackFrames(out IMDStackTraceEnum ppEnum)
        {
            ppEnum = new MDStackTraceEnum(m_ex.StackTrace);
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

        public void GetExceptionObject(ulong addr, out IMDException ppExcep)
        {
            ppExcep = new MDException(m_heap.GetExceptionObject(addr));
        }

        public void EnumerateRoots(out IMDRootEnum ppEnum)
        {
            ppEnum = new MDRootEnum(new List<ClrRoot>(m_heap.EnumerateRoots()));
        }

        public void EnumerateSegments(out IMDSegmentEnum ppEnum)
        {
            ppEnum = new MDSegmentEnum(m_heap.Segments);
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

        public void DumpException(ulong ObjRef)
        {
            var exception = m_heap.GetExceptionObject(ObjRef);
            if (exception == null)
            {
                Exports.WriteLine("No expeception found at {0:%p}", ObjRef);
                Exports.WriteLine("");
                return;
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
            Exports.WriteLine("{0}", DumpStack(exception.StackTrace, m_heap.GetRuntime().PointerSize));
            Exports.WriteLine("HResult: {0:x4}", exception.HResult);
            Exports.WriteLine("");
        }


        public void DumpAllExceptions(IMDObjectEnum Exceptions)
        {
            Dictionary<string, List<ulong>> allExceptions = new Dictionary<string, List<ulong>>();
            foreach (var obj in ((MDObjectEnum)Exceptions).List)
            {
                ClrException ex = m_heap.GetExceptionObject(obj);
                if (ex != null)
                {
                    if (Exports.isInterrupted())
                        return;
                    string key = String.Format("{0}\0{1}\0{2}", ex.Type.Name, ex.Message, DumpStack(ex.StackTrace, m_heap.GetRuntime().PointerSize, true));
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
        }

        void m_runtime_RuntimeFlushed(ClrRuntime runtime)
        {
            isFlushedCalled = true;
        }

        public void GetCommonMethodTable(out MD_CommonMT CommonMT)
        {

            AdHoc.GetCommonMT(m_runtime, out CommonMT.StringMethodTable, out CommonMT.ArrayMethodTable,
                out CommonMT.FreeMethodTable);
        }

        public void IsServerGC(out int pServerGC)
        {
            pServerGC = m_runtime.ServerGC ? 1 : 0;
        }

        public void GetHeapCount(out int pHeapCount)
        {
            pHeapCount = m_runtime.HeapCount;
        }

        public void PrintFieldVisibility(ClrField field, bool isStatic = false)
        {
            Exports.Write("{0}", field.IsInternal ? "internal " : field.IsProtected ? "protected " : field.IsPrivate ? "private " : field.IsPublic ? "public " : "undefinedvisibility ");
            if (isStatic)
                Exports.Write("static ");
            Exports.Write("{0} ", field.Type == null ? "object" : field.Type.Name);
            Exports.WriteLine("{0};", field.Name);
        }

        public void DumpClass(ulong MethodTable)
        {


            ClrType type = AdHoc.GetTypeFromMT(m_runtime, MethodTable);
            if (type == null)
            {
                Exports.WriteLine("No type with Method Table {0:%p}", MethodTable);
                Exports.WriteLine("");
                return;
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
                    return;
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
                    return;
                Exports.Write("\t");
                PrintFieldVisibility(field, true);
            }

            foreach (var field in type.ThreadStaticFields)
            {
                if (Exports.isInterrupted())
                    return;
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
                    return;
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
                    return;

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


        }

        public void DumpDomains()
        {
            var domains = AdHoc.GetDomains(m_runtime);
            if (domains == null || domains.Length < 2)
            {
                Exports.WriteLine("Unable to get Application Domains. This is not expected.");
                return;
            }

            if (m_runtime.PointerSize == 8)
                Exports.WriteLine("Address          Domain Name                                                 Modules Base Path & Config");
            else
                Exports.WriteLine("Address  Domain Name                                                 Modules Base Path & Config");


            for (int i = 0; i < domains.Length; i++)
            {
                if (Exports.isInterrupted())
                    return;

                Exports.Write("{0:%p} ", domains[i].Address);
                Exports.Write("{0, -60} ", i == 0 ? "System" : i == 1 ? "Shared" : domains[i].Name);
                Exports.Write("{0,6:#,#} ", domains[i].Modules.Count);
                if (!String.IsNullOrEmpty(domains[i].ApplicationBase)) Exports.Write("Base Path: {0} ", domains[i].ApplicationBase);
                if (!String.IsNullOrEmpty(domains[i].ConfigurationFile)) Exports.Write("Config: {0} ", domains[i].ConfigurationFile);
                Exports.WriteLine("");
            }


        }

        public void DumpHandles(int GroupOnly,string filterByType, string filterByObjType)
        {
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
                    return;
                if (!categories.ContainsKey(handle.HandleType.ToString()))
                {
                    categories[handle.HandleType.ToString()] = 0;
                }
                categories[handle.HandleType.ToString()]++;
                ClrType obj = m_runtime.GetHeap().GetObjectType(handle.Object);

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
        }

        public static void AddIfTrue(ref StringBuilder Sb, bool IsTrue, string StrToAdd)
        {
            if (!IsTrue) return;
            if (Sb.Length > 0) Sb.Append('|');
            Sb.Append(StrToAdd);

        }

        public void DumpThreads()
        {

            if (m_runtime.PointerSize == 8)
                Exports.WriteLine("   Id OSId Address          Domain           Allocation Start:End              COM  GC Type  Locks Type / Status             Last Exception");
            else
                Exports.WriteLine("   Id OSId Address  Domain   Alloc Start:End   COM  GC Type  Locks Type / Status             Last Exception");

            foreach (var thread in m_runtime.Threads)
            {
                if (Exports.isInterrupted())
                {
                    return;
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
        }



        public int ReadVirtual(ulong addr, byte[] buffer, int requested, out int pRead)
        {
            int read;
            bool success = m_runtime.ReadMemory(addr, buffer, requested, out read);

            pRead = (int)read;
            return success ? 0 : -1;
        }

        public int ReadPtr(ulong addr, out ulong pValue)
        {
            bool success = m_runtime.ReadPointer(addr, out pValue);
            return success ? 1 : 0;
        }

        public void Flush()
        {
            m_runtime.Flush();
        }

        public void IsFlush(out int isFlushed)
        {
            isFlushed = isFlushedCalled ? 1 : 0;
        }

        public void GetHeap(out IMDHeap ppHeap)
        {
            ppHeap = new MDHeap(m_runtime.GetHeap());
        }

        public void EnumerateAppDomains(out IMDAppDomainEnum ppEnum)
        {
            ppEnum = new MDAppDomainEnum(AdHoc.GetDomains(m_runtime));
        }

        public void EnumerateThreads(out IMDThreadEnum ppEnum)
        {
            ppEnum = new MDThreadEnum(m_runtime.Threads, m_runtime);
        }

        public void EnumerateFinalizerQueue(out IMDObjectEnum ppEnum)
        {
            ppEnum = new MDObjectEnum(new List<ulong>(m_runtime.EnumerateFinalizerQueue()));
        }

        public void EnumerateGCHandles(out IMDHandleEnum ppEnum)
        {
            ppEnum = new MDHandleEnum(m_runtime.EnumerateHandles());
        }

        public void EnumerateMemoryRegions(out IMDMemoryRegionEnum ppEnum)
        {
            ppEnum = new MDMemoryRegionEnum(new List<ClrMemoryRegion>(m_runtime.EnumerateMemoryRegions()));
        }

        public void GetArraySizeByMT(ulong MethodTable, out ulong BaseSize, out ulong ComponentSize)
        {
            TypeBasicInfo bi = AdHoc.GetArrayDataSimple(m_runtime, MethodTable);
            BaseSize = bi.BaseSize;
            ComponentSize = bi.ComponentSize;
        }

        public void GetNameForMT(ulong MethodTable, [Out] [MarshalAs((UnmanagedType)19)] out string pTypeName)
        {
            pTypeName = AdHoc.GetNameForMT(m_runtime, MethodTable);
        }

        public void GetTypeByMT(ulong addr, out IMDType ppType)
        {
            ppType = new MDType(AdHoc.GetTypeFromMT(m_runtime, addr));
        }

        public void GetMethodNameByMD(ulong addr, out string pMethodName)
        {
            var method = m_runtime.GetMethodByAddress(addr);
            if (method == null)
            {
                pMethodName = null;
                return;
            }
            pMethodName = method.GetFullSignature();
        }

        public int GetOSThreadIDByAddress(ulong ThreadAddress, [Out] out uint OSThreadID)
        {
            OSThreadID = 0;
            try
            {
                OSThreadID = AdHoc.GetOSThreadIdByAddress(m_runtime, ThreadAddress);
            }
            finally
            {

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

        public void GetRuntimeVersion(out string pVersion)
        {
            pVersion = m_info.ToString();
        }

        public void GetDacLocation(out string pLocation)
        {
            pLocation = m_info.TryGetDacLocation();
        } 

        public void GetDacRequestData(out int pTimestamp, out int pFilesize)
        {
            pTimestamp = (int)m_info.DacInfo.TimeStamp;
            pFilesize = (int)m_info.DacInfo.FileSize;
        }

        public void GetDacRequestFilename(out string pRequestFileName)
        {
            pRequestFileName = m_info.DacInfo.FileName;
        }
    }

    [ComVisible(true)]
    [Guid("05957304-2C21-43EA-B5FE-A5D42084B5A7")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IMDTarget))]
    public class MDTarget : IMDTarget
    {
        DataTarget m_target;

        public MDTarget(string crashdump)
        {
            // TODO: Complete member initialization
            m_target = DataTarget.LoadCrashDump(crashdump);
        }

        public MDTarget(object iDebugClient)
        {
            m_target = DataTarget.CreateFromDebuggerInterface((IDebugClient)iDebugClient);
        }

        public void GetRuntimeCount(out int pCount)
        {
            pCount = m_target.ClrVersions.Count;
        }

        public void GetRuntimeInfo(int num, out IMDRuntimeInfo ppInfo)
        {
            ppInfo = new MDRuntimeInfo(m_target.ClrVersions[num]);
        }

        public void GetPointerSize(out int pPointerSize)
        {
            pPointerSize = (int)m_target.PointerSize;
        }

        public void CreateRuntimeFromDac(string dacLocation, out IMDRuntime ppRuntime)
        {
            ppRuntime = new MDRuntime(m_target.CreateRuntime(dacLocation));
        }

        public void CreateRuntimeFromIXCLR(object ixCLRProcess, out IMDRuntime ppRuntime)
        {
            ppRuntime = new MDRuntime(m_target.CreateRuntime(ixCLRProcess));
        }

        internal const string DownloadUrl = "http://netext.codeplex.com/";

        internal static Version GetOnlineVersion()
        {
            Version codeplex = new Version(0, 0, 0, 0);

            try
            {
                WebClient client = new WebClient();
                string text = client.DownloadString(DownloadUrl);
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

        public void CreateEnum(out IMDObjectEnum ppEnum)
        {
            ppEnum = new MDObjectEnum();
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

        public void GetCount(out int pCount)
        {
            pCount = m_data.Count;
        }

        public void Reset()
        {
            m_curr = 0;
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

        public void GetCount(out int pCount)
        {
            pCount = m_data.Count;
        }

        public void Reset()
        {
            m_curr = 0;
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

        public void GetCount(out int pCount)
        {
            pCount = m_data.Count;
        }

        public void Reset()
        {
            m_curr = 0;
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
                m_cet = ClrElementType.Unknown;

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

        public void IsNull(out int pNull)
        {
            pNull = (m_cet == ClrElementType.Unknown || m_value == null ) ? 1 : 0;
        }


        public void GetElementType(out int pCET)
        {
            pCET = (int)m_cet;
        }

        public void GetInt32(out int pValue)
        {
            ulong value = GetValue64();
            pValue = (int)value;
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

        public void GetUInt32(out uint pValue)
        {
            ulong value = GetValue64();
            pValue = (uint)value;
        }

        public void GetInt64(out long pValue)
        {
            ulong value = GetValue64();
            pValue = (long)value;
        }

        public void GetUInt64(out ulong pValue)
        {
            ulong value = GetValue64();
            pValue = (ulong)value;
        }

        public void GetString(out string pValue)
        {
            pValue = (string)m_value;
        }


        public void GetBool(out int pBool)
        {
            if (m_value is bool)
                pBool = ((bool)m_value) ? 1 : 0;
            else
                pBool = (int)GetValue64();
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

        public void GetCount(out int pCount)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            m_obj = 0;
            m_done = true;
        }

        public void AddAddress(ulong ObjRef)
        {
            throw new Exception("Not implemented");
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

        public void GetCount(out int pCount)
        {
            pCount = m_refs.Count;
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

        public void Reset()
        {
            m_curr = 0;
        }

        public void Clear()
        {
            m_curr = 0;
            m_refs.Clear();
        }

        public void AddAddress(ulong ObjRef)
        {
            m_refs.Add(ObjRef);
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

        public void GetCount(out int pCount)
        {
            pCount = m_refs.Count;
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

        public void Reset()
        {
            m_curr = 0;
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

        public void GetCount(out int pCount)
        {
            pCount = m_refs.Count;
        }

        public void Reset()
        {
            m_curr = 0;
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

        public void GetCount(out int pCount)
        {
            pCount = m_segments.Count;
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

        public void Reset()
        {
            m_curr = 0;
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

        public void GetCount(out int pCount)
        {
            pCount = m_curr;
        }

        public int Next(out IMDRoot ppRoot)
        {
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

        public void Reset()
        {
            m_curr = 0;
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

        public void GetCount(out int pCount)
        {
            pCount = m_frames.Count;
        }

        public int Next(out ulong pIP, out ulong pSP, out string pFunction)
        {
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

        public void Reset()
        {
            m_curr = 0;
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

        public void GetCount(out int pCount)
        {
            pCount = m_handles.Count();
        }

        public int Next(out IMDHandle ppHandle)
        {
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

        public void Reset()
        {
            m_curr = 0;
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

        public void GetCount(out int pCount)
        {
            pCount = m_regions.Count;
        }

        public int Next(out IMDMemoryRegion ppRegion)
        {
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

        public void Reset()
        {
            throw new NotImplementedException();
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

        public void GetCount(out int pCount)
        {
            pCount = m_threads.Count;
        }

        public int Next(out IMDThread ppThread)
        {
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

        public void Reset()
        {
            m_curr = 0;
        }
    }
}
