/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
  
  Part of this code is based on a public proof-of-concept sample application from
    Microsoft.Diagnostics.Runtime's developer Lee Culver
============================================================================================================*/


using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Diagnostics.Runtime;

namespace NetExt.Shim
{
    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.GuidAttribute("41CBFB96-45E4-4F2C-9002-82FD2ECD585F")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDTarget
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetRuntimeCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetRuntimeInfo(int num, [Out] [MarshalAs((UnmanagedType)28)] out IMDRuntimeInfo ppInfo);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetPointerSize([Out] out int pPointerSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int CreateRuntimeFromDac([MarshalAs((UnmanagedType)19)] string dacLocation, [Out] [MarshalAs((UnmanagedType)28)] out IMDRuntime ppRuntime);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int CreateRuntimeFromIXCLR([MarshalAs((UnmanagedType)25)] Object ixCLRProcess, [MarshalAs((UnmanagedType)28)] out IMDRuntime ppRuntime);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int CompareVersion(int Major, int Minor, int Build, int Revision);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int OpenDownloadPage();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int CreateEnum([Out] [MarshalAs((UnmanagedType)28)] out IMDObjectEnum ppEnum);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetCurrentTime([Out] out Int64 TargetTime, [Out] out Int64 UtcTime);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int DumpTime();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetModuleByAddress(ulong Address, [Out] IMDModule Module);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetModuleByName([MarshalAs((UnmanagedType)19)] string ModuleName, [Out] IMDModule Module);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetModuleByIndex(int Index, [Out] IMDModule Module);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetContextModule([Out] IMDModule Module);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetModuleCount([Out] int Count);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int SaveAllModules([MarshalAs((UnmanagedType)19)] string Path);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int SaveModule([MarshalAs((UnmanagedType)19)] string Path, [MarshalAs((UnmanagedType)19)] string ModuleName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int DumpModules([MarshalAs(UnmanagedType.LPStr)] string Pattern, [MarshalAs(UnmanagedType.LPStr)] string Company,
            [MarshalAs(UnmanagedType.LPStr)] string folderToSave, bool DebugMode,
            bool ManagedOnly, bool ExcludeMicrosoft, bool Ordered, bool IncludePath);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int MakeSource();
    }

    [ComVisible(true)]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.GuidAttribute("7D3553BC-BB68-403D-B353-A47A684E7763")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDModule
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetDetails([Out] out MD_Module ModuleDetails);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetCopyright([Out] [MarshalAs((UnmanagedType)19)] out string Copyright);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetCompanyName([Out] [MarshalAs((UnmanagedType)19)] out string Company);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetModuleName([Out] [MarshalAs((UnmanagedType)19)] out string ModuleName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetOriginalName([Out] [MarshalAs((UnmanagedType)19)] out string OriginalName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetFullPath([Out] [MarshalAs((UnmanagedType)19)] out string FullPath);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int SaveModule([MarshalAs((UnmanagedType)19)] string Path);


    }
    [ComVisible(true)]
    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
    public struct MD_Module
    {

        public ulong Address;
        public int ClrDebugType;
        public CorFlags ClrFlags;
        public bool isValid;
        public bool isClr;
        public int ImageDebugType;
        public int Index;
        public int Major;
        public int Minor;
        public int Build;
        public int metaMajor;
        public int metaMinor;
        public int metaBuild;

    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("4AADFA25-0486-48EB-9338-D4B39E23AF82")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDRuntimeInfo
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetRuntimeVersion([Out] [MarshalAs((UnmanagedType)19)] out string pVersion);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetDacLocation([Out] [MarshalAs((UnmanagedType)19)] out string pVersion);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetDacRequestData([Out] out int pTimestamp, [Out] out int pFilesize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetDacRequestFilename([Out] [MarshalAs((UnmanagedType)19)] out string pRequestFileName);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.GuidAttribute("2900B785-981D-4A6F-82DC-AE7B9DA08EA2")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDRuntime
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetCommonMethodTable([Out] out MD_CommonMT CommonMT);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int IsServerGC([Out] out int pServerGC);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetHeapCount([Out] out int pHeapCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int DumpClass(ulong MethodTable);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int DumpDomains();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int DumpHandles(int GroupOnly, [MarshalAs((UnmanagedType)19)] string filterByType, [MarshalAs((UnmanagedType)19)] string filterByObjType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int DumpThreads();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int ReadVirtual(ulong addr, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] byte[] buffer, int requested, [Out] out int pRead);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int ReadPtr(ulong addr, [Out] out ulong pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int Flush();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int IsFlush([Out] out int isFlushed);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetHeap([Out] [MarshalAs((UnmanagedType)28)] out IMDHeap ppHeap);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int EnumerateAppDomains([Out] [MarshalAs((UnmanagedType)28)] out IMDAppDomainEnum ppEnum);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int EnumerateThreads([Out] [MarshalAs((UnmanagedType)28)] out IMDThreadEnum ppEnum);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int EnumerateFinalizerQueue([Out] [MarshalAs((UnmanagedType)28)] out IMDObjectEnum ppEnum);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int EnumerateGCHandles([Out] [MarshalAs((UnmanagedType)28)] out IMDHandleEnum ppEnum);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int EnumerateMemoryRegions([Out] [MarshalAs((UnmanagedType)28)] out IMDMemoryRegionEnum ppEnum);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetTypeByMT(ulong addr, [Out] [MarshalAs((UnmanagedType)28)] out IMDType ppType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetArraySizeByMT(ulong MethodTable, [Out] out ulong BaseSize, [Out] out ulong ComponentSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetNameForMT(ulong MethodTable, [Out] [MarshalAs((UnmanagedType)19)] out string pTypeName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetMethodNameByMD(ulong addr, [Out] [MarshalAs((UnmanagedType)19)] out string pMethodName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetOSThreadIDByAddress(ulong ThreadAddress, [Out] out uint OSThreadID);
        

    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("B53137DF-FC18-4470-A0D9-8EE4F829C970")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDHeap
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int GetObjectType(ulong addr, [Out] [MarshalAs((UnmanagedType)28)] out IMDType ppType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetExceptionObject(ulong addr, [Out] [MarshalAs((UnmanagedType)28)] out IMDException ppExcep);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int EnumerateRoots([Out] [MarshalAs((UnmanagedType)28)] out IMDRootEnum ppEnum);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int EnumerateSegments([Out] [MarshalAs((UnmanagedType)28)] out IMDSegmentEnum ppEnum);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int DumpAllExceptions([MarshalAs((UnmanagedType)28)] IMDObjectEnum ppEnum);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int DumpException(ulong ObjRef);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int DumpXml(ulong ObjRef);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetXmlString(ulong ObjRef, [Out] [MarshalAs((UnmanagedType)19)] out string XmlString);

    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("BCA27E5A-5226-4CB6-AE95-239560E4FD71")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDAppDomainEnum
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int Reset();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int Next([Out] [MarshalAs((UnmanagedType)28)] out IMDAppDomain ppAppDomain);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("CA6A4D0A-7770-4254-A072-DA72A6CC1A72")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDThreadEnum
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int Reset();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int Next([Out] [MarshalAs((UnmanagedType)28)] out IMDThread ppThread);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.GuidAttribute("61119B5D-53DE-443C-911B-EB0607555451")]
    public interface IMDObjectEnum
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int Reset();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Clear();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int Next(int count, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] ulong[] objs, [Out] out int pWrote);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int AddAddress(ulong ObjRef);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("32742CA6-56E7-438D-8FE8-1D30BE2F1F86")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDHandleEnum
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int Reset();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int Next([Out] [MarshalAs((UnmanagedType)28)] out IMDHandle ppHandle);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("9B68F32F-0DDD-452B-88F1-972496F44210")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDMemoryRegionEnum
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int Reset();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int Next([Out] [MarshalAs((UnmanagedType)28)] out IMDMemoryRegion ppRegion);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("FF5B59F4-07A0-4D7C-8B59-69338EECEA16")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDType
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetIMetadata([Out] [MarshalAs((UnmanagedType)25)] out object IMetadata);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetHeader(ulong objRef, [Out] out MD_TypeData typeData);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetName([Out] [MarshalAs((UnmanagedType)19)] out string pName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetRuntimeName(ulong objRef, [Out] [MarshalAs((UnmanagedType)19)] out string pName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetSize(ulong objRef, [Out] out ulong pSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int ContainsPointers([Out] out int pContainsPointers);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCorElementType([Out] out int pCET);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetString(ulong ObjRef, [Out] [MarshalAs((UnmanagedType)19)] out string strValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetFilename([Out] [MarshalAs((UnmanagedType)19)] out string fileName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetBaseType([Out] [MarshalAs((UnmanagedType)28)] out IMDType ppBaseType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetArrayComponentType([Out] [MarshalAs((UnmanagedType)28)] out IMDType ppArrayComponentType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCCW(ulong addr, [Out] [MarshalAs((UnmanagedType)28)] out IMDCCW ppCCW);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetRCW(ulong addr, [Out] [MarshalAs((UnmanagedType)28)] out IMDRCW ppRCW);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int IsArray([Out] out int pIsArray);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int IsFree([Out] out int pIsFree);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int IsException([Out] out int pIsException);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int IsEnum([Out] out int pIsEnum);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetEnumElementType([Out] out int pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetEnumNames([Out] [MarshalAs((UnmanagedType)28)] out IMDStringEnum ppEnum);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetEnumValueInt32([MarshalAs((UnmanagedType)19)] string name, [Out] out int pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetFieldCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetField(int index, [Out] [MarshalAs((UnmanagedType)28)] out IMDField ppField);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetEnumName(ulong value, [Out] [MarshalAs((UnmanagedType)19)] out string enumString);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int GetFieldData(ulong obj, int interior, int count, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2)] MD_FieldData[] fields, [Out] out int pNeeded);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetAllFieldsDataRawCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetRawFieldAddress(ulong obj, int interior, int index, [Out] out ulong address);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetRawFieldTypeAndName(int index, [Out] [MarshalAs((UnmanagedType)19)] out string pType, [Out] [MarshalAs((UnmanagedType)19)] out string pName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        int GetAllFieldsDataRaw(int valueType, int count, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] MD_FieldData[] fields, [Out] out int pNeeded);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetStaticFieldCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetStaticField(int index, [Out] [MarshalAs((UnmanagedType)28)] out IMDStaticField ppStaticField);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetThreadStaticFieldCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetThreadStaticField(int index, [Out] [MarshalAs((UnmanagedType)28)] out IMDThreadStaticField ppThreadStaticField);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetArrayLength(ulong objRef, [Out] out int pLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetArrayElementAddress(ulong objRef, int index, [Out] out ulong pAddr);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetArrayElementValue(ulong objRef, int index, [Out] [MarshalAs((UnmanagedType)28)] out IMDValue ppValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int EnumerateReferences(ulong objRef, [Out] [MarshalAs((UnmanagedType)28)] out IMDReferenceEnum ppEnum);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int EnumerateInterfaces([Out] [MarshalAs((UnmanagedType)28)] out IMDInterfaceEnum ppEnum);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.GuidAttribute("E3F30A85-0DBB-44C3-AA3E-460CCF31A2F1")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDException
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetGCHeapType([Out] [MarshalAs((UnmanagedType)28)] out IMDType ppType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetErrorMessage([Out] [MarshalAs((UnmanagedType)19)] out string pMessage);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetObjectAddress([Out] out ulong pAddress);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetInnerException([Out] [MarshalAs((UnmanagedType)28)] out IMDException ppException);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetHRESULT([Out] [MarshalAs((UnmanagedType)45)] out int pHResult);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int EnumerateStackFrames([Out] [MarshalAs((UnmanagedType)28)] out IMDStackTraceEnum ppEnum);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("90783F46-39C8-480A-BD7C-0D89FA97D5AD")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDRootEnum
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int Reset();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int Next([Out] [MarshalAs((UnmanagedType)28)] out IMDRoot ppRoot);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.GuidAttribute("18851D2F-A705-492C-9A80-202F39300E80")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDSegmentEnum
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int Reset();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int Next([Out] [MarshalAs((UnmanagedType)28)] out IMDSegment ppSegment);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.GuidAttribute("5A73920A-F8C5-4FCB-A725-8564F41BB055")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDCCW
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetIUnknown([Out] out ulong pIUnk);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetObject([Out] out ulong pObject);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetHandle([Out] out ulong pHandle);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetRefCount([Out] out int pRefCnt);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int EnumerateInterfaces([Out] [MarshalAs((UnmanagedType)28)] out IMDCOMInterfaceEnum ppEnum);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("192249CE-6A17-4307-BA70-7AA871682128")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDRCW
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetIUnknown([Out] out ulong pIUnk);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetObject([Out] out ulong pObject);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetRefCount([Out] out int pRefCnt);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetVTable([Out] out ulong pHandle);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int IsDisconnected([Out] out int pDisconnected);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int EnumerateInterfaces([Out] [MarshalAs((UnmanagedType)28)] out IMDCOMInterfaceEnum ppEnum);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.GuidAttribute("B53B96FD-F8E3-4B86-AA5A-ECA1B6352840")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDStringEnum
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int Reset();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int Next([Out] [MarshalAs((UnmanagedType)19)] out string pValue);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("823556FD-FFC5-4139-8D84-D9B72E835D2F")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDField
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetName([Out] [MarshalAs((UnmanagedType)19)] out string pName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetType([Out] [MarshalAs((UnmanagedType)28)] out IMDType ppType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetElementType([Out] out int pCET);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetSize([Out] out int pSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetOffset([Out] out int pOffset);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetFieldValue(ulong objRef, int interior, [Out] [MarshalAs((UnmanagedType)28)] out IMDValue ppValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetFieldAddress(ulong objRef, int interior, [Out] out ulong pAddress);
    }

    [ComVisible(true)]
    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
    public struct MD_SegData
    {

        //
        // Summary:
        //     The address of the end of memory committed for the segment (this may be longer
        //     than Length).
        public ulong CommittedEnd;
        //
        // Summary:
        //     The end address of the segment. All objects in this segment fall within Start
        //     <= object < End.
        public ulong End;
        //
        // Summary:
        //     If it is possible to move from one object to the 'next' object in the segment.
        //     Then FirstObject returns the first object in the heap (or null if it is not
        //     possible to walk the heap.
        public ulong FirstObject;
        //
        // Summary:
        //     The length of the gen0 portion of this segment.
        public ulong Gen0Length;
        //
        // Summary:
        //     Ephemeral heap sements have geneation 0 and 1 in them. Gen 1 is always above
        //     Gen 2 and Gen 0 is above Gen 1. This property tell where Gen 0 start in memory.
        //     Note that if this is not an Ephemeral segment, then this will return End
        //     (which makes Gen 0 empty for this segment)
        public ulong Gen0Start;
        //
        // Summary:
        //     The length of the gen1 portion of this segment.
        public ulong Gen1Length;
        //
        // Summary:
        //     The start of the gen1 portion of this segment.
        public ulong Gen1Start;
        //
        // Summary:
        //     The length of the gen2 portion of this segment.
        public ulong Gen2Length;
        //
        // Summary:
        //     The start of the gen2 portion of this segment.
        public ulong Gen2Start;
        //
        // Summary:
        //     Returns true if this segment is the ephemeral segment (meaning it contains
        //     gen0 and gen1 objects).
        public bool IsEphemeral;
        //
        // Summary:
        //     Returns true if this is a segment for the Large Object Heap. False otherwise.
        //      Large objects (greater than 85,000 bytes in size), are stored in their own
        //     segments and only collected on full (gen 2) collections.
        public bool IsLarge;
        //
        // Summary:
        //     The number of bytes in the segment.
        public ulong Length;
        //
        // Summary:
        //     The processor that this heap is affinitized with. In a workstation GC, there
        //     is no processor affinity (and the return value of this property is undefined).
        //     In a server GC each segment has a logical processor in the PC associated
        //     with it. This property returns that logical processor number (starting at
        //     0).
        public int ProcessorAffinity;
        //
        // Summary:
        //     The address of the end of memory reserved for the segment, but not committed.
        public ulong ReservedEnd;
        //
        // Summary:
        //     The start address of the segment. All objects in this segment fall within
        //     Start <= object < End.
        public ulong Start;
    }

    [ComVisible(true)]
    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
    public struct MD_ThreadData
    {
        
        // Summary:
        //     The address of the underlying datastructure which makes up the Thread object.
        //     This serves as a unique identifier.
        public ulong Address;
        //
        // Summary:
        //     The AppDomain the thread is running in.
        public ulong AppDomain;

        //
        // Summary:
        //     Returns the exception currently on the thread. Note that this field may be
        //     null. Also note that this is basically the "last thrown exception", and may
        //     be stale...meaning the thread could be done processing the exception but
        //     a crash dump was taken before the current exception was cleared off the field.
        public ulong CurrentException;
        //
        // Summary:
        //     The suspension state of the thread according to the runtime.
        public int GcMode;
        //
        // Summary:
        //     Returns true if this thread was aborted.
        public int IsAborted;
        //
        // Summary:
        //     Returns true if an abort was requested for this thread (such as Thread.Abort,
        //     or AppDomain unload).
        public int IsAbortRequested;
        //
        // Summary:
        //     Returns true if the thread is alive in the process, false if this thread
        //     was recently terminated.
        public int IsAlive;
        //
        // Summary:
        //     Returns true if this thread is a background thread. (That is, if the thread
        //     does not keep the managed execution environment alive and running.)
        public int IsBackground;
        //
        // Summary:
        //     Returns true if the Clr runtime called CoIntialize for this thread.
        public int IsCoInitialized;
        //
        // Summary:
        //     Returns if this thread is the debugger helper thread.
        public int IsDebuggerHelper;
        //
        // Summary:
        //     Returns true if the debugger has suspended the thread.
        public int IsDebugSuspended;
        //
        // Summary:
        //     Returns true if this is the finalizer thread.
        public int IsFinalizer;
        //
        // Summary:
        //     Returns if this thread is a GC thread. If the runtime is using a server GC,
        //     then there will be dedicated GC threads, which this will indicate. For a
        //     runtime using the workstation GC, this flag will only be true for a thread
        //     which is currently running a GC (and the background GC thread).
        public int IsGC;
        //
        // Summary:
        //     Returns true if the GC is attempting to suspend this thread.
        public int IsGCSuspendPending;
        //
        // Summary:
        //     Returns true if the thread is a COM multithreaded apartment.
        public int IsMTA;
        //
        // Summary:
        //     Returns true if this thread is currently the thread shutting down the runtime.
        public int IsShutdownHelper;
        //
        // Summary:
        //     Returns true if this thread is in a COM single threaded apartment.
        public int IsSTA;
        //
        // Summary:
        //     Returns if this thread currently suspending the runtime.
        public int IsSuspendingEE;
        //
        // Summary:
        //     Returns true if this thread is a threadpool IO completion port.
        public int IsThreadpoolCompletionPort;
        //
        // Summary:
        //     Returns true if this is the threadpool gate thread.
        public int IsThreadpoolGate;
        //
        // Summary:
        //     Returns true if this thread is a threadpool timer thread.
        public int IsThreadpoolTimer;
        //
        // Summary:
        //     Returns true if this is a threadpool wait thread.
        public int IsThreadpoolWait;
        //
        // Summary:
        //     Returns true if this is a threadpool worker thread.
        public int IsThreadpoolWorker;
        //
        // Summary:
        //     Returns true if this thread was created, but not started.
        public int IsUnstarted;
        //
        // Summary:
        //     Returns true if the user has suspended the thread (using Thread.Suspend).
        public int IsUserSuspended;
        //
        // Summary:
        //     The number of managed locks (Monitors) the thread has currently entered but
        //     not left.  This will be highly inconsistent unless the process is stopped.
        public uint LockCount;
        //
        // Summary:
        //     The managed thread ID (this is equivalent to System.Threading.Thread.ManagedThreadId
        //     in the target process).
        public int ManagedThreadId;
        //
        // Summary:
        //     The OS thread id for the thread.
        public uint OSThreadId;
        //
        // Summary:
        //     The base of the stack for this thread, or 0 if the value could not be obtained.
        public ulong StackBase;
        //
        // Summary:
        //     The limit of the stack for this thread, or 0 if the value could not be obtained.
        public ulong StackLimit;
        //
        // Summary:
        //     The TEB (thread execution block) address in the process.
        public ulong Teb;
        //
        // Summary:
        //     The number of objects currently blocking this thread
        public int BlockingObjectsCount;
        //
        // Summary:
        //     Address where the allocation starts in the thread
        public ulong AllocationStart;
        //
        // Summary:
        //     Maximum Address for allocation in the thread
        public ulong AllocationLimit;

    }

    [ComVisible(true)]
    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
    public struct MD_CommonMT
    {
        public ulong FreeMethodTable;
        public ulong StringMethodTable;
        public ulong ArrayMethodTable;
    }


    [ComVisible(true)]
    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
    public struct MD_FieldData
    {
        public ulong MethodTable;
        public int corElementType;
        public uint token;
        public int offset;
        public int size;
        public bool isStatic;
        public bool isThreadStatic;
        public bool isArray;
        public bool isValueType;
        public bool isGeneric;
        public bool isString;
        public bool isEnum;
        public int index;
        public int generalIndex;
        public ulong module;
        public ulong value;
    }

    [ComVisible(true)]
    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
    public struct MD_TypeData
    {
        public ulong MethodTable;
        public ulong parentMT;
        public ulong EEClass;
        public ulong BaseSize;      // Always populated
        public ulong size;          // Only populated when acquired from an object address
        public int instanceFieldsCount;
        public int staticFieldsCount;
        public int heap;            // Only populated when acquired from an object address
        public int generation;      // Only populated when acquired from an object address
        public ulong module;
        public int debuggingFlag;
        public ulong assembly;
        public ulong appDomain;
        public uint token;
        public int rank;
        public int arraySize;       // Only populated when acquired from an object address
        public ulong arrayStart;    // Only populated when acquired from an object address
        public int elementSize;     // Only populated when acquired from an object address
        public int arrayCorType;
        public ulong arrayElementMT;
        public int corElementType;
        public bool isStatic;
        public bool isThreadStatic;
        public bool isArray;
        public bool isValueType;
        public bool isGeneric;
        public bool isString;
        public bool isFree;
        public bool containPointers;
        public bool isCCW;
        public bool isRCW;
        public bool isRuntimeType;
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("0E65FD09-D7A6-4B45-BEAC-76A002F52525")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDStaticField
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetName([Out] [MarshalAs((UnmanagedType)19)] out string pName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetType([Out] [MarshalAs((UnmanagedType)28)] out IMDType ppType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetElementType([Out] out int pCET);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetSize([Out] out int pSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetFieldValue([MarshalAs((UnmanagedType)28)] IMDAppDomain appDomain, [Out] [MarshalAs((UnmanagedType)28)] out IMDValue ppValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetFieldAddress([MarshalAs((UnmanagedType)28)] IMDAppDomain appDomain, [Out] out ulong pAddress);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("809B99EF-7E7C-4351-BE76-9DCA2624D53E")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDThreadStaticField
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetName([Out] [MarshalAs((UnmanagedType)19)] out string pName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetType([Out] [MarshalAs((UnmanagedType)28)] out IMDType ppType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetElementType([Out] out int pCET);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetSize([Out] out int pSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetFieldValue([MarshalAs((UnmanagedType)28)] IMDAppDomain appDomain, [MarshalAs((UnmanagedType)28)] IMDThread thread, [Out] [MarshalAs((UnmanagedType)28)] out IMDValue ppValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetFieldAddress([MarshalAs((UnmanagedType)28)] IMDAppDomain appDomain, [MarshalAs((UnmanagedType)28)] IMDThread thread, [Out] out ulong pAddress);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("B72A6495-EE8A-4491-A01D-295B36A730F2")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDValue
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int IsNull(out int pNull);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetElementType(out int pCET);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetInt32(out int pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetUInt32(out UInt32 pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetInt64(out Int64 pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetUInt64(out ulong pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetString([MarshalAs((UnmanagedType)19)] out string pValue);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetBool(out int pBool);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("35DE7FD4-902C-4CC5-9C87-A7C9632B4B4A")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDReferenceEnum
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int Reset();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int Next(int count, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] MD_Reference[] refs, [Out] out int pWrote);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("723BC010-963F-11E1-A8B0-0800200C9A66")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDInterfaceEnum
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int Reset();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int Next([Out] [MarshalAs((UnmanagedType)28)] out IMDInterface ppValue);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("E630B609-502A-43F7-8F88-1598F21F2848")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDCOMInterfaceEnum
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int Reset();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int Next([MarshalAs((UnmanagedType)28)] IMDType pType, out ulong pInterfacePtr);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.GuidAttribute("6558598B-772F-4686-8F14-72C565072171")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDAppDomain
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetName([Out] [MarshalAs((UnmanagedType)19)] out string pName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetID([Out] out int pID);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetAddress([Out] out ulong pAddress);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("A64706CC-C45D-481B-A4B4-4E3D04D27F91")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDThread
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetThreadData([Out] out MD_ThreadData threadData);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetAddress([Out] out ulong pAddress);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int IsFinalizer([Out] out int IsFinalizer);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int IsAlive([Out] out int IsAlive);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetOSThreadId([Out] out int pOSThreadId);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetAppDomainAddress([Out] out ulong pAppDomain);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetLockCount([Out] out int pLockCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCurrentException([Out] [MarshalAs((UnmanagedType)28)] out IMDException ppException);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetTebAddress([Out] out ulong pTeb);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetStackLimits([Out] out ulong pBase, [Out] out ulong pLimit);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int EnumerateStackTrace([Out] [MarshalAs((UnmanagedType)28)] out IMDStackTraceEnum ppEnum);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("04DF4D19-7BFB-4535-BE6C-4115AB5F313D")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDStackTraceEnum
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCount([Out] out int pCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int Reset();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        [PreserveSig]
        int Next([Out] out ulong pIP, [Out] out ulong pSP, [Out] [MarshalAs((UnmanagedType)19)] out string pFunction);
    }

    [ComVisible(true)]
    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
    public struct MD_Reference
    {
        public ulong address;
        public int offset;
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("31980D20-963F-11E1-A8B0-0800200C9A66")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDInterface
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetName([Out] [MarshalAs((UnmanagedType)19)] out string pName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetBaseInterface([Out] [MarshalAs((UnmanagedType)28)] out IMDInterface ppBase);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.GuidAttribute("CF7DD882-7CF9-4F13-91C3-3E102CEDA943")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDRoot
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetRootInfo([Out] out ulong pAddress, [Out] out ulong pObjRef, [Out] out MDRootType pType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetType([Out] [MarshalAs((UnmanagedType)28)] out IMDType ppType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetName([Out] [MarshalAs((UnmanagedType)19)] out string ppName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetAppDomain([Out] [MarshalAs((UnmanagedType)28)] out IMDAppDomain ppDomain);
    }

    [ComVisible(true)]
    public enum MDRootType
    {
        MDRoot_StaticVar,
        MDRoot_ThreadStaticVar,
        MDRoot_LocalVar,
        MDRoot_Strong,
        MDRoot_Weak,
        MDRoot_Pinning,
        MDRoot_Finalizer,
        MDRoot_AsyncPinning,
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("62CE62BA-066A-4FBD-B150-B4E5EAA080EE")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDSegment
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetSegData([Out] out MD_SegData SegData);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetStart([Out] out ulong pAddress);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetEnd([Out] out ulong pAddress);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetReserveLimit([Out] out ulong pAddress);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetCommitLimit([Out] out ulong pAddress);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetLength([Out] out ulong pLength);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetProcessorAffinity([Out] out int pProcessor);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int IsLarge([Out] out int pLarge);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int IsEphemeral([Out] out int pEphemeral);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetGen0Info([Out] out ulong pStart, [Out] out ulong pLen);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetGen1Info([Out] out ulong pStart, [Out] out ulong pLen);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetGen2Info([Out] out ulong pStart, [Out] out ulong pLen);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int EnumerateObjects([Out] [MarshalAs((UnmanagedType)28)] out IMDObjectEnum ppEnum);
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.GuidAttribute("FDB00A49-0584-4C24-95A4-B5CB08917596")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDHandle
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetHandleData([Out] out ulong pAddr, [Out] out ulong pObjRef, [Out] out MDHandleTypes pType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int IsStrong([Out] out int pStrong);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetRefCount([Out] out int pRefCount);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetDependentTarget([Out] out ulong pTarget);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetAppDomain([Out] [MarshalAs((UnmanagedType)28)] out IMDAppDomain ppDomain);
    }

    [ComVisible(true)]
    public enum MDHandleTypes
    {
        MDHandle_WeakShort,
        MDHandle_WeakLong,
        MDHandle_Strong,
        MDHandle_Pinned,
        MDHandle_Variable,
        MDHandle_RefCount,
        MDHandle_Dependent,
        MDHandle_AsyncPinned,
        MDHandle_SizedRef,
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.GuidAttribute("8E5310DF-0F3A-4456-ACC4-52DEE8754767")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDMemoryRegion
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetRegionInfo([Out] out ulong pAddress, [Out] out ulong pSize, [Out] out MDMemoryRegionType pType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetAppDomain([Out] [MarshalAs((UnmanagedType)28)] out IMDAppDomain ppDomain);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetModule([Out] [MarshalAs((UnmanagedType)19)] out string pModule);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetHeapNumber([Out] out int pHeap);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetDisplayString([Out] [MarshalAs((UnmanagedType)19)] out string pName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int GetSegmentType([Out] out MDSegmentType pType);
    }


    [ComVisible(true)]
    public enum MDMemoryRegionType
    {
        MDRegion_LowFrequencyLoaderHeap,
        MDRegion_HighFrequencyLoaderHeap,
        MDRegion_StubHeap,
        MDRegion_IndcellHeap,
        MDRegion_LookupHeap,
        MDRegion_ResolveHeap,
        MDRegion_DispatchHeap,
        MDRegion_CacheEntryHeap,
        MDRegion_JITHostCodeHeap,
        MDRegion_JITLoaderCodeHeap,
        MDRegion_ModuleThunkHeap,
        MDRegion_ModuleLookupTableHeap,
        MDRegion_GCSegment,
        MDRegion_ReservedGCSegment,
        MDRegion_HandleTableChunk,
    }

    [ComVisible(true)]
    public enum MDSegmentType
    {
        MDSegment_Ephemeral,
        MDSegment_Regular,
        MDSegment_LargeObject,
    }

    //[Guid("5DB635A4-8BEB-4353-9674-F8A47104E125")]
    [ComVisible(true)]
    [System.Runtime.InteropServices.GuidAttribute("5DC19835-504C-47AF-B96B-06AF1A737AE9")]
    [System.Runtime.InteropServices.TypeLibTypeAttribute((System.Runtime.InteropServices.TypeLibTypeFlags)128)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)1)]
    public interface IMDActivator
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int CreateFromCrashDump([MarshalAs((UnmanagedType)19)] string crashdump, [Out] [MarshalAs((UnmanagedType)28)] out IMDTarget ppTarget);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int CreateFromIDebugClient([MarshalAs((UnmanagedType)25)] Object iDebugClient, [Out] [MarshalAs((UnmanagedType)28)] out IMDTarget ppTarget);
    }

}
