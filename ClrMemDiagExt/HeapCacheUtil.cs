/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetExt.Shim;
using System.Reflection;
using System.ComponentModel;

namespace NetExt.HeapCacheUtil
{

    public delegate bool CacheCallBack(uint Count);

    public class TypeBasicInfo
    {
        public ulong MethodTable = 0;
        public ulong ComponentSize = 0;
        public ulong BaseSize = 0;

    }

    public class AdHoc
    {
        public static object RunMethod(object Source, string Method, params object[] Params)
        {
            BindingFlags bindingFlags = BindingFlags.Instance | 
                    BindingFlags.NonPublic;
            Type t = Source.GetType();
            var methods = t.GetMethods(bindingFlags);
            MethodInfo mInfo = null;
            if (methods.Length > 1)
            {
                foreach (var m in methods)
                {
                    if (m.Name == Method && m.GetParameters().Length == Params.Length)
                    {
                        mInfo = m;
                        break;
                    }
                }
            } else
                mInfo = t.GetMethod(Method, bindingFlags);
            if (mInfo == null)
                mInfo = t.BaseType.GetMethod(Method, bindingFlags);
            if (mInfo == null)
                throw new IndexOutOfRangeException("Invalid method in Hacks.RunMethod");
            return mInfo.Invoke(Source, Params);
            
        }
    
        public static void GetCommonMT(ClrRuntime Runtime, out ulong StringMT, out ulong ArrayMT, out ulong FreeMT)
        {
            
            StringMT = 0;
            ArrayMT = 0;
            FreeMT = 0;
            try
            {

                StringMT = (ulong)GetMember(Runtime, "StringMethodTable");
                ArrayMT = (ulong)GetMember(Runtime, "ArrayMethodTable");
                FreeMT = (ulong)GetMember(Runtime, "FreeMethodTable");
            }
            catch
            {
            }
        }


        public static ClrType GetTypeFromMT(ClrRuntime Runtime, ulong MethodTable)
        {
            return Runtime.Heap.GetTypeByMethodTable(MethodTable);
            //return RunMethod(Runtime.GetHeap(), "GetGCHeapType", (ulong)MethodTable, (ulong)0, (ulong)0) as ClrType;
        }

        public static string GetNameForMT(ClrRuntime Runtime, ulong MethodTable)
        {
            ClrType tp = Runtime.Heap.GetTypeByMethodTable(MethodTable);
            if (tp == null)
                return null;

            string name = tp.Name;  //RunMethod(Runtime, "GetNameForMT", MethodTable) as string;
            if (!String.IsNullOrEmpty(name))
            {
                name.Replace('+', '_');
            }
            return name;
        }


        public static uint GetOSThreadIdByAddress(ClrRuntime Runtime, ulong Address)
        {
            return Runtime.Threads.First(t => t.Address == Address).OSThreadId;
            /*
            uint OSThreadID = 0;
            if (Address == 0)
                return 0;
            object thread = RunMethod(Runtime, "GetThread", Address);
            OSThreadID = (uint)GetMember(thread, "OSThreadID");
            return OSThreadID;
             */
        }

        public static void GetThreadAllocationLimits(ClrRuntime Runtime, ulong Address, out ulong AllocContextPtr, out ulong AllocContextLimit)
        {
            
            //AllocPtr
            //AllocLimit
            AllocContextPtr = 0;
            AllocContextLimit = 0;
            if (Address == 0)
                return;
            object thread = RunMethod(Runtime, "GetThread", Address);
            if (thread == null)
                return;
            AllocContextPtr = (ulong)GetMember(thread, "AllocPtr");
            AllocContextLimit = (ulong)GetMember(thread, "AllocLimit");
        }

        public static ClrAppDomain[] GetDomains(ClrRuntime Runtime)
        {
            ClrAppDomain system = Runtime.SystemDomain;
            ClrAppDomain shared = Runtime.SharedDomain;
            /*
            ulong systemDomain = 0;
            ulong sharedDomain = 0;
            GetSystemAndSharedAddress(Runtime, out systemDomain, out sharedDomain);

            ClrAppDomain system = RunMethod(Runtime, "InitDomain", systemDomain) as ClrAppDomain ;
            ClrAppDomain shared = RunMethod(Runtime, "InitDomain", sharedDomain) as ClrAppDomain;
            */
            List<ClrAppDomain> domains = new List<ClrAppDomain>();
            domains.Add((ClrAppDomain)system);


            domains.Add((ClrAppDomain)shared);
            domains.AddRange(Runtime.AppDomains);
            return domains.ToArray();
        }

        public static ClrType GetTypeFromEE(ClrRuntime Runtime, ulong EEClass)
        {
            ClrHeap heap = Runtime.Heap;
            //ClrType type = null;
            ulong mt = heap.GetMethodTableByEEClass(EEClass);
            if (mt == 0)
                return null;
            return heap.GetTypeByMethodTable(mt);
            /*
            int max = heap.TypeIndexLimit;
            if (max < 1)
            {
                var objList = heap.EnumerateObjects().GetEnumerator();
                if (objList.MoveNext())
                {
                    type = heap.GetObjectType(objList.Current);
                }
            } else
                type = Runtime.GetHeap().GetTypeByIndex(0);
            if (type == null)
                throw new NullReferenceException("There is no object in the heap");
            ClrType EEType = (ClrType)RunMethod(type, "ConstructObjectType", EEClass);
            return EEType;
             */
        }



        public static string GetEnumName(ClrType type, ulong enumValue)
        {
            if (type == null || !type.IsEnum)
                return "#INVALIDENUMTYPE#";
            object value = enumValue;
            List<string> namesFound = new List<string>();
            ClrElementType enumType = ClrElementType.Unknown;
            try
            {
                enumType = type.GetEnumElementType();
            }
            catch
            {
                return "#INVALIDENUMTYPE#";
            }
            byte[] raw = BitConverter.GetBytes(enumValue);

            object gvalue = enumValue;
            switch (enumType)
            {
                case ClrElementType.Int16:
                    gvalue = BitConverter.ToInt16(raw, 0);
                    break;
                case ClrElementType.Int32:
                    gvalue = BitConverter.ToInt32(raw, 0);
                    break;
                case ClrElementType.Int64:
                    gvalue = BitConverter.ToInt64(raw, 0);
                    break;
                case ClrElementType.UInt16:
                    gvalue = BitConverter.ToUInt16(raw, 0);
                    break;
                case ClrElementType.UInt32:
                    gvalue = BitConverter.ToUInt32(raw, 0);
                    break;
                case ClrElementType.UInt64:
                    gvalue = BitConverter.ToUInt64(raw, 0);
                    break;
                default:
                    gvalue = 0UL;
                    break;
            }

            if (!String.IsNullOrEmpty(type.GetEnumName(gvalue)))
            {
                return type.GetEnumName(gvalue);
            }
            var names = type.GetEnumNames();
            foreach (var name in names)
            {
                if (type.TryGetEnumValue(name, out value))
                {
                    byte[] rawValue = null;
                    switch (enumType)
                    {
                        case ClrElementType.Int16:
                            rawValue = BitConverter.GetBytes((short)value);
                            break;
                        case ClrElementType.Int32:
                            rawValue = BitConverter.GetBytes((int)value);
                            break;
                        case ClrElementType.Int64:
                            rawValue = BitConverter.GetBytes((long)value);
                            break;
                        case ClrElementType.UInt16:
                            rawValue = BitConverter.GetBytes((ushort)value);
                            break;
                        case ClrElementType.UInt32:
                            rawValue = BitConverter.GetBytes((uint)value);
                            break;
                        case ClrElementType.UInt64:
                            rawValue = BitConverter.GetBytes((ulong)value);
                            break;
                        default:
                            rawValue = BitConverter.GetBytes((ulong)0UL);
                            break;
                    }
                    byte[] raw64 = BitConverter.GetBytes((ulong)0UL);
                    for (int i = 0; i < rawValue.Length; i++)
                    {
                        raw64[i] = rawValue[i];
                    }
                    ulong u = BitConverter.ToUInt64(raw64,0);
                    if ((enumValue & u) == u && u != 0)
                    {
                        namesFound.Add(name);
                    }
                }
            }
            if (namesFound.Count == 0)
                return String.Empty;
            return String.Join("|", namesFound.ToArray());

        }


        public static ulong GetEEFromMT(ClrRuntime Runtime, ulong MethodTable)
        {
            return Runtime.Heap.GetEEClassByMethodTable(MethodTable);

            /*
            object obj = RunMethod(Runtime, "GetMethodTableData", MethodTable);
            if (obj == null)
                return 0;
            ulong? EEClass = GetMember(obj, "EEClass") as ulong?;
            if (EEClass == null)
                return 0;
            return (ulong)EEClass;
             */
        }
        public const int ARRAYSTART = 0;
        public const int ARRAYELEMENTMT = 1;
        public const int ARRAYCORTYPE = 2;


        public static ulong[] GetArrayData(ClrRuntime Runtime, ulong Address)
        {
            ulong[] array = new ulong[3] { 0, 0, 0 };
            if (Runtime == null || Address == 0)
                return array;
            object obj = RunMethod(Runtime, "GetObjectData", Address);
            if (obj == null)
                return array;
            ulong? start = null;
            ulong? mt = null;
            ClrElementType? corType = null;
            try
            {

                start = GetMember(obj, "DataPointer") as ulong?;
                mt = GetMember(obj, "ElementTypeHandle") as ulong?;
                corType = GetMember(obj, "ElementType") as ClrElementType?;

            }
            finally
            {
                if(start != null)
                    array[ARRAYSTART] = (ulong)start;
                if(mt != null)
                    array[ARRAYELEMENTMT] = (ulong)mt;
                if(corType != null)
                    array[ARRAYCORTYPE] = (ulong)corType;
            }
            return array;

        }

        //
        //  Return enough to expedite indexing
        //
        public static TypeBasicInfo GetArrayDataSimple(ClrRuntime Runtime, ulong MethodTable)
        {
            TypeBasicInfo bi = new TypeBasicInfo();
            bi.MethodTable = MethodTable;
            if (Runtime == null || MethodTable == 0)
                return bi;
            object obj = RunMethod(Runtime, "GetMethodTableData", MethodTable);
            if (obj == null)
                return bi;
            uint? ComponentSize = null;
            uint? BaseSize = null;
            try
            {
                ComponentSize = GetMember(obj, "ComponentSize") as uint?;
                BaseSize = GetMember(obj, "BaseSize") as uint?;
            }
            finally
            {
                
                if (ComponentSize != null)
                    bi.ComponentSize = (uint)ComponentSize;
                if (BaseSize != null)
                    bi.BaseSize = (uint)BaseSize;
                
            }

            return bi;
        }


        //
        //  Use GetArrayData instead
        //
        public static ulong GetArrayElementTypeMT(ClrRuntime Runtime, ulong MethodTable)
        {
            object obj = RunMethod(Runtime, "GetMethodTableData", MethodTable);
            if (obj == null)
                return 0;
            ulong? MTofElement = null;
            try
            {
                MTofElement = GetMember(obj, "ElementTypeHandle") as ulong?;
            }
            catch
            {
                return 0;
            }
            if(MTofElement == null)
                return 0;
            return (ulong)MTofElement;
        }

        public static void GetSystemAndSharedAddress(ClrRuntime Runtime, out ulong AppDomainSystem, out ulong AppDomainShared)
        {

            AppDomainSystem = Runtime.SystemDomain.Address;
            AppDomainShared = Runtime.SharedDomain.Address;
            /*
            object store = RunMethod(Runtime, "GetAppDomainStoreData");
            object system = GetMember(store, "SharedDomain") as ulong?;
            object shared = GetMember(store, "SystemDomain") as ulong?;
            if (shared != null)
                AppDomainShared = (ulong)shared;
            if (system != null)
                AppDomainSystem = (ulong)system;
            */
        }

        public static ulong GetDomainFromMT(ClrRuntime Runtime, ulong MethodTable)
        {
            if (MethodTable == 0)
                return 0;
            object module = RunMethod(Runtime, "GetModuleForMT", MethodTable);
            if (module == null)
                return 0;

            object moduleData = RunMethod(Runtime, "GetModuleData", module);

            if (moduleData == null)
                return 0;
            object assembly = GetMember(moduleData, "Assembly");
            if (assembly == null)
                return 0;
            object assemblyData = RunMethod(Runtime, "GetAssemblyData", 0UL, assembly);
            if (assemblyData == null)
                return 0;
            object domain = GetMember(assemblyData, "ParentDomain");
            if (domain == null)
                return 0;
            return (ulong)domain;
            
        }
        public static ulong[] GetDomainsAddr(ClrRuntime runtime, ulong Address)
        {
            
            List<ulong> domains = new List<ulong>();
            if (Address == 0)
                return domains.ToArray();

            foreach (var domain in AdHoc.GetDomains(runtime))
            {
                
                foreach (var module in domain.Modules)
                {
                    if (module.ImageBase == Address)
                        domains.Add(module.AssemblyId);
                }

            }

            return domains.ToArray();
        }

        public static ClrAppDomain GetDomainByAddress(ClrRuntime Runtime, ulong Address)
        {
            var domain = GetDomains(Runtime).First(t => t.Address == Address);
            return domain;
            /*
            if (Address == 0)
                return null;
            ClrAppDomain domain = RunMethod(Runtime, "InitDomain", Address) as ClrAppDomain;
            return domain;
             */
        }

        public static object GetMember(object Source, string Field /*, Type castType=null */)
        {
            string[] fields = Field.Split('.');
            object curr = Source;
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;


            bool succeeded = false;


            foreach(string field in fields)
            {
                Type t = curr.GetType();
                succeeded = false;
                FieldInfo fInfo = t.GetField(field, bindingFlags);
                if (fInfo != null)
                {
                    curr = fInfo.GetValue(curr);
                    succeeded = true;
                    continue;
                }

                PropertyInfo pInfo = t.GetProperty(field, bindingFlags);
                if (pInfo != null)
                {
                    curr = pInfo.GetValue(curr, null);
                    succeeded = true;
                    continue;
                }

                return new System.IndexOutOfRangeException();
            }
            if (succeeded) return curr;
            return new System.ArgumentNullException();

        }
    }

    public class CacheFieldInfo
    {
        public string TypeName { get; internal set; }
        public string Name { get; internal set; }
        public ulong MethodTable { get; internal set; }
        public int Offset { get; private set; }
        public bool IsStatic { get; internal set; }
        public bool IsThreadStatic { get; internal set; }
        public uint Token { get; internal set; }
        public ulong Module { get; internal set; }
        public ulong Assembly { get; internal set; }
        public ClrField BackField { get; internal set; }
        public int Size { get; internal set; }
        public int CorElementType { get; internal set; }
        public bool IsArray { get; internal set; }
        public bool IsValueType { get; internal set; }
        public bool IsGeneric { get; internal set; }
        public int Index { get; internal set; }
        public bool IsEnum { get; internal set; }
        public bool IsString { get; internal set; }

        public static string GetFieldTypeName(ClrType Type)
        {
            if (Type == null || Type.MetadataToken == 0)
                return "System.Object";

            return Type.Name == null ? "<UNKNOWN>"+Type.MethodTable.ToString("x16") : Type.Name.Replace('+','_');
        }

        public static string AdjustFieldName(string FieldName)
        {
            if (String.IsNullOrEmpty(FieldName))
                return "**INVALIDFIELD**";
            if (FieldName.IndexOfAny(new char[] { '<', '>', '+', '$' }) >= 0)
                return FieldName.Replace('<', '_').Replace('>', '_').Replace('+', '_').Replace('$','_');
            return FieldName;
        }

        public string GetEnumName(ulong enumValue)
        {
            object value = enumValue;
            List<string> namesFound = new List<string>();

            if(!String.IsNullOrEmpty(BackField.Type.GetEnumName(value)))
            {
                return BackField.Type.GetEnumName(value);
            }
            var names = BackField.Type.GetEnumNames();
            foreach(var name in names)
            {

                if (BackField.Type.TryGetEnumValue(name, out value))
                {
                    ulong u = (ulong)value;
                    if ((enumValue & u) == u && u != 0)
                    {
                        namesFound.Add(name);
                    }
                }
            }
            if(namesFound.Count == 0)
                return String.Empty;
            return String.Join("|", namesFound.ToArray());

        }

        public CacheFieldInfo(ClrInstanceField Field, int FieldIndex)
        {
            IsStatic = false;
            IsThreadStatic = false;

            TypeName = GetFieldTypeName(Field.Type);
            Name = AdjustFieldName(Field.Name);
            MethodTable = HeapStatItem.GetMTOfType(Field.Type);
            Offset = Field.Offset;
            Token = Field.Type == null ? 0 : Field.Type.MetadataToken;
            Module = Field.Type == null || Field.Type.Module == null ? 0 : Field.Type.Module.ImageBase;
            Assembly = Field.Type == null || Field.Type.Module == null ? 0 : Field.Type.Module.AssemblyId;
            BackField = Field;
            Size = Field.Size;
            CorElementType = (int)Field.ElementType;
            IsArray = Field.Type == null ? false : Field.Type.IsArray;
            IsValueType = Field.Type == null ? false : Field.Type.IsValueClass || Field.Type.IsPrimitive;
            IsGeneric = Field.Type == null ? true : Field.Type.Name.Contains('<') && Field.Type.Name.Contains('>') && Field.Type.Name[0] != '<';
            Index = FieldIndex;
            IsEnum = Field.Type == null ? false : Field.Type.IsEnum;
            IsString = Field.Type == null ? false : Field.Type.IsString;
        }

        public CacheFieldInfo(ClrStaticField Field, int FieldIndex)
        {
            IsStatic = true;
            IsThreadStatic = false;

            TypeName = GetFieldTypeName(Field.Type);
            Name = AdjustFieldName(Field.Name);
            MethodTable = HeapStatItem.GetMTOfType(Field.Type);
            Offset = Field.Offset;
            Token = Field.Type == null ? 0 : Field.Type.MetadataToken;
            Module = Field.Type == null || Field.Type.Module == null ? 0 : Field.Type.Module.ImageBase;
            Assembly = Field.Type == null || Field.Type.Module == null ? 0 : Field.Type.Module.AssemblyId;
            BackField = Field;
            Size = Field.Size;
            CorElementType = (int)Field.ElementType;
            IsArray = Field.Type == null ? false : Field.Type.IsArray;
            IsValueType = Field.Type == null ? false : Field.Type.IsValueClass || Field.Type.IsPrimitive;
            IsGeneric = Field.Type == null ? true : Field.Type.Name.Contains('<') && Field.Type.Name.Contains('>') && Field.Type.Name[0] != '<';
            Index = FieldIndex;
            IsEnum = Field.Type == null ? false : Field.Type.IsEnum;
            IsString = Field.Type == null ? false : Field.Type.IsString;

        }

        public CacheFieldInfo(ClrThreadStaticField Field, int FieldIndex)
        {
            IsStatic = true;
            IsThreadStatic = true;

            TypeName = GetFieldTypeName(Field.Type);
            Name = AdjustFieldName(Field.Name);
            MethodTable = HeapStatItem.GetMTOfType(Field.Type);
            Offset = Field.Offset;
            Token = Field.Type == null ? 0 : Field.Type.MetadataToken;
            Module = Field.Type == null || Field.Type.Module == null ? 0 : Field.Type.Module.ImageBase;
            Assembly = Field.Type == null || Field.Type.Module == null ? 0 : Field.Type.Module.AssemblyId;
            BackField = Field;
            Size = Field.Size;
            CorElementType = (int)Field.ElementType;
            IsArray = Field.Type == null ? false : Field.Type.IsArray;
            IsValueType = Field.Type == null ? false : Field.Type.IsValueClass || Field.Type.IsPrimitive;
            IsGeneric = Field.Type == null ? true : Field.Type.Name.Contains('<') && Field.Type.Name.Contains('>') && Field.Type.Name[0] != '<';
            Index = FieldIndex;
            IsEnum = Field.Type == null ? false : Field.Type.IsEnum;
            IsString = Field.Type == null ? false : Field.Type.IsString;
        }

        public static IList<CacheFieldInfo> Fields(ClrType Type)
        {
            List<CacheFieldInfo> fields = new List<CacheFieldInfo>();
            int i = 0;
            foreach (ClrInstanceField instField in Type.Fields)
            {
                fields.Add(new CacheFieldInfo(instField, i++));
            }
            i = 0;
            foreach (ClrStaticField statField in Type.StaticFields)
            {
                fields.Add(new CacheFieldInfo(statField, i++));
            }
            i = 0;
            foreach (ClrThreadStaticField tsField in Type.ThreadStaticFields)
            {
                fields.Add(new CacheFieldInfo(tsField, i++));
            }
            return fields;

        }

    }

    public class HeapStatItem
    {
        private HeapCache heap;
        private CacheInfo info;


        public static ulong GetMTOfType(ClrType Type)
        {
            if (Type == null)
                return 0UL;
            return Type.MethodTable;
            /*
            object temp = AdHoc.GetMember(Type, "_handle");

            if (temp as ulong? == null)
            {
                temp = AdHoc.GetMember(Type.Heap.Runtime, "ObjectMethodTable");
                if(temp as ulong? == null)
                    return 0; ;
            }
            return (ulong)temp;
            */
        }

        internal HeapStatItem(string Name, HeapCache Cache)
        {
            this.Name = Name;
            this.heap = Cache;
            this.info = Cache.cache[Name];
        }

        public string Name { get; internal set; }


        public string TypeName { get; internal set; }
        public ulong TotalSize
        {
            get
            {

                return info.Size;
            }
        }
        public UInt64 TotalSizeSquared
        {
            get
            {

                return info.SizeSquared;
            }
        }

        public ulong Count { get { return (ulong)info.Cache.Count; } }
        public ulong MinSize
        {
            get
            {
                return info.MinSize;
            }
        }
        public ulong MaxSize
        {
            get
            {
                return info.MaxSize;
            }
        }

        public ulong Average
        {
            get
            {
                return info.Size / (ulong)info.Cache.Count;
            }
        }

        public ulong StdDeviation
        {
            get
            {
                return (ulong)Math.Ceiling(Math.Sqrt(
               ((ulong)info.Cache.Count * info.SizeSquared - info.Size * info.Size) /
               (double)((info.Cache.Count > 1) ? (info.Cache.Count * (info.Cache.Count - 1)) : 1)
                ));

            }

        }

        public IList<ulong> Addresses
        {
            get
            {
                return info.Cache;
            }
        }

        public IEnumerable<string> Interfaces
        {
            get
            {
                ulong addr = Addresses[0];
                return heap.EnumerateInterfaces(addr);
            }
        }

        public IEnumerable<string> InheritanceChain
        {
            get
            {
                ulong addr = Addresses[0];
                return heap.InheritanceChain(addr);
            }
        }

        public bool IsImplementationOf(string TypePattern)
        {
            ulong addr = Addresses[0];
            return heap.IsImplementationOf(addr, TypePattern);
        }

        public bool IsDerivedOf(string TypePattern)
        {
            ulong addr = Addresses[0];
            return heap.IsDerivedOf(addr, TypePattern);
        }

        public bool IsDerivedOrImplementationOf(string TypePattern)
        {
            ulong addr = Addresses[0];
            return heap.IsDerivedOrImplementationOf(addr, TypePattern);
        }

    }

    public enum SPRequestType
    {
        SPWeb,
        SPSite,
        SPRequest
    }

    public class SPRequestObj
    {
        public SPRequestObj(SPRequestType SPType, ulong Obj, ulong Request, int Thread)
        {
            this.Type = SPType;
            this.Address = Obj;
            this.SPRequest = Request;
            this.ThreadID = Thread;
        }
        public SPRequestType Type { get; set; }
        public ulong Address { get; set; }
        public ulong SPRequest { get; set; }
        public int ThreadID { get; set; }
    }

    public class ThreadObj
    {
        public string TypeName { get; internal set; }
        public ulong Address { get; internal set; }
        public bool IsAlive { get; internal set; }
        public uint OSThreadId { get; internal set; }
        public bool IsPossibleFalsePositive { get; internal set; }
    }

    class CacheInfo
    {
        public List<ulong> Cache = new List<ulong>();
        public ulong Size = 0;
        public ulong SizeSquared = 0;
        public ulong MaxSize = 0;
        public ulong MinSize = ulong.MaxValue;

    }

    public class HeapCache
    {
        internal Dictionary<string, CacheInfo> cache;


        protected ClrRuntime runtime;
        protected ClrHeap heap;

        public ClrHeap Heap
        {
            get
            { return heap; }
        }

        public bool IsValidHeap
        {
            get;
            internal set;
        }

        private CacheCallBack callBack;
        private uint interval;
        private uint total;

        public HeapCache(ClrRuntime Runtime, CacheCallBack CacheUpdate = null, uint Interval = 1000000)
        {
            runtime = Runtime;
            cache = null;
            if (runtime == null)
            {
                IsValidHeap = true;
                return;
            }
            heap = runtime.Heap;
            if (!heap.CanWalkHeap)
            {
                IsValidHeap = true;

                return;
            }
            interval = Interval;
            callBack = CacheUpdate;
            IsValidHeap = false;


        }

        public void Flush()
        {
            cache.Clear();
            cache = null;
        }

        public void EnsureCache()
        {
            // cache is created. No worries
            if (cache != null) return;
            total = 0;
            cache = new Dictionary<string, CacheInfo>();
            foreach (var obj in heap.EnumerateObjectAddresses())
            {
                string name = heap.GetObjectType(obj).Name;

                CacheInfo info;
                if (!cache.TryGetValue(name, out info))
                {
                    info = new CacheInfo();

                    cache[name] = info;
                }
                //ulong size = heap.GetObjectType(obj).GetSize(obj);
                //info.Size += size;
                //info.SizeSquared += size * size;
                //if (size > info.MaxSize) info.MaxSize = size;
                //if (size < info.MinSize) info.MinSize = size;
                info.Cache.Add(obj);
                total++;
                if ((total % interval) == 0)
                    if (callBack != null)
                    {
                        if (!callBack(total))
                        {
                            cache = null;
                            return;
                        }
                    }
            }
            if (callBack != null)
            {
                if (!callBack(total))
                {
                    cache = null;
                    return;
                }
            }

        }

        public static bool WildcardCompare(string Text, string Pattern, bool IsCase = false)
        {
            if (String.IsNullOrEmpty(Pattern))
                return true;
            if (String.IsNullOrEmpty(Text))
                return false;

            return DebugApi.MatchPattern(Text, Pattern, IsCase);
            /*
            string[] parts = Pattern.Split('*');
            if (parts.Length > 0)
            {
                if (parts[0].Length > 0 && !Text.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase)) return false;
                if (parts[parts.Length - 1].Length > 0 && !Text.EndsWith(parts[parts.Length - 1], StringComparison.OrdinalIgnoreCase)) return false;

                for (int i = 1; i < parts.Length - 1; i++)
                {
                    if (Text.IndexOf(parts[i], StringComparison.OrdinalIgnoreCase) == -1) return false;
                }
            }
            else
            {
                return String.Compare(Text, Pattern, true) == 0;
            }
            */
            //return true;
        }
        //
        // Use empty string for all objects
        //
        public IEnumerable<ulong> EnumerateObjectsOfType(string TypePattern = "")
        {
            EnsureCache();

            foreach (string str in cache.Keys)
            {
                if (WildcardCompare(str, TypePattern))
                    foreach (ulong obj in cache[str].Cache) yield return obj;
            }


        }

        public IEnumerable<dynamic> EnumerateDynamicObjectsOfType(string TypePattern = "")
        {
            EnsureCache();
            foreach (string str in cache.Keys)
            {
                if (WildcardCompare(str, TypePattern))
                    foreach (ulong obj in cache[str].Cache) yield return heap.GetDynamicObject(obj);
            }

        }

        public dynamic GetDinamicFromAddress(ulong Address)
        {
            return heap.GetDynamicObject(Address);
        }

        public IEnumerable<HeapStatItem> GetExceptions()
        {
            foreach (HeapStatItem item in EnumerateTypesStats())
            {
                if (item.IsDerivedOf("System.Exception"))
                    yield return item;
            }
        }

        public IEnumerable<string> EnumerateInterfaces(ulong Address)
        {
            ClrType tp = heap.GetObjectType(Address);

            foreach (ClrInterface intfc in tp.Interfaces)
            {
                yield return intfc.Name;
            }
        }

        public IEnumerable<string> InheritanceChain(ulong Address)
        {
            ClrType tp = heap.GetObjectType(Address);

            do
            {

                tp = tp.BaseType;
                if (tp != null) yield return tp.Name;
            } while (tp != null && tp.Name != "System.Object");
        }

        public bool IsImplementationOf(ulong Address, string TypePattern)
        {
            if (String.IsNullOrEmpty(TypePattern))
            {
                throw new ArgumentException("TypePattern cannot be empty or null");
            }

            foreach (string str in EnumerateInterfaces(Address))
            {
                if (WildcardCompare(str, TypePattern))
                    return true;
            }
            return false;
        }

        public bool IsDerivedOf(ulong Address, string TypePattern)
        {
            if (String.IsNullOrEmpty(TypePattern))
            {
                throw new ArgumentException("TypePattern cannot be empty or null");
            }

            foreach (string str in InheritanceChain(Address))
            {
                if (WildcardCompare(str, TypePattern))
                    return true;
            }
            return false;
        }

        public bool IsDerivedOrImplementationOf(ulong Address, string TypePattern)
        {
            return IsImplementationOf(Address, TypePattern) || IsDerivedOf(Address, TypePattern);
        }

        public IEnumerable<HeapStatItem> EnumerateTypesStats()
        {
            EnsureCache();
            foreach (string str in cache.Keys)
            {
                HeapStatItem item = new HeapStatItem(str, this);
                yield return item;
            }
        }

        public object GetFieldValue(ulong Address, string FieldName, ClrType TheType = null)
        {
            string[] fields = FieldName.Split('.');

            ulong address = Address;
            ClrType type = TheType;
            if (type == null)
            {
                type = heap.GetObjectType(Address);
            }
            ClrField field;
            CacheFieldInfo fInfo;
            for (int i = 0; i < fields.Length - 1; i++)
            {
                fInfo = CacheFieldInfo.Fields(TheType).First(x => x.Name == fields[i]);
                field = fInfo == null ? null : fInfo.BackField;
                // At this level we cannot have a simple value
                if (!field.IsObjectReference)
                    return GetDinamicFromAddress(Address);

                if (fInfo.IsStatic)
                {
                    //address = (ulong)((ClrStaticField)field).GetValue(field);
                    return null;

                }
                else
                    if (fInfo.IsThreadStatic)
                    {
                        //address = (ulong)((ClrThreadStaticField)field).GetValue(field);
                        return null;
                    }
                    else
                    {
                        address = (ulong)((ClrInstanceField)field).GetValue(address);
                    }
                if (address == 0) return null;
                type = heap.GetObjectType(address);

            }
            fInfo = CacheFieldInfo.Fields(TheType).First(x => x.Name == fields[fields.Length - 1]);
            if (fInfo.IsStatic)
                return "NOT IMPLEMENTED";
            field = fInfo.BackField;
            object value = null;
            
            value = ((ClrInstanceField)field).GetValue(address);
            //if (field.Type.IsEnum)
            //    value = field.Type.GetEnumName(value);


            return value;
        }

        public Dictionary<string, object> GetFields(ulong Address, string Fields)
        {
            string[] fields = Fields.Split(new char[] { ' ', ',' });
            var type = heap.GetObjectType(Address);
            Dictionary<string, object> fieldDict = new Dictionary<string, object>();
            foreach (string field in fields)
            {
                if (!String.IsNullOrWhiteSpace(field))
                    fieldDict[field] = GetFieldValue(Address, field, type);
            }
            return fieldDict;
        }

        public IEnumerable<ThreadObj> EnumerateStackObj(uint OsThreadId = 0, string TypePattern = "")
        {
            foreach (ClrThread thread in runtime.Threads)
            {
                if (OsThreadId == 0 || OsThreadId == thread.OSThreadId)
                {
                    foreach (ClrRoot root in thread.EnumerateStackObjects())
                    {
                        string typeName = root.Type.Name;
                        if (WildcardCompare(typeName, TypePattern))
                        {
                            ThreadObj obj = new ThreadObj();
                            obj.Address = root.Address;
                            obj.IsPossibleFalsePositive = root.IsPossibleFalsePositive;
                            obj.OSThreadId = thread.OSThreadId;
                            obj.Address = thread.Address;
                            obj.TypeName = typeName;
                            obj.IsAlive = thread.IsAlive;
                            yield return obj;
                        }
                    }
                }
            }

        }

    }
}
