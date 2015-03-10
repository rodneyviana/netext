using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Diagnostics.RuntimeExt;


//
// Helper by Rodney Viana added to Create some helpers
//
namespace Microsoft.Diagnostics.RuntimeExt
{

    public delegate bool CacheCallBack(uint Count);

    public class FieldInfo
    {
        public string TypeName { get; internal set; }
        public string Name {get; internal set;}
        public ulong MethodTable {get; internal set;}
        public int Offset {get; private set;}
        public bool IsStatic { get; internal set; }
        public bool IsThreadStatic { get; internal set; }
        public uint Token { get; internal set; }
        public ulong Module {get; internal set; }



        public FieldInfo(ClrField Field)
        {
            TypeName = Field.Type.Name;
            Name = Field.Name;
            MethodTable = HeapStatItem.GetMTOfType(Field.Type);
            Offset = Field.Offset;
            IsStatic = false;
            IsThreadStatic = false;
            Token = Field.Type.MetadataToken;
 
        }
    }

    public class HeapStatItem
    {
        private HeapCache heap;
        private CacheInfo info;

        public static ulong GetMTOfType(ClrType Type)
        {
            return 0;
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

    public class CacheInfo
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

        public HeapCache(ClrRuntime Runtime, CacheCallBack CacheUpdate=null,uint Interval=1000000)
        {
            runtime = Runtime;
            cache = null;
            if (runtime == null)
            {
                IsValidHeap = true;
                return;
            }
            heap = runtime.GetHeap();
            if (!heap.CanWalkHeap)
            {
                IsValidHeap = true;

                return;
            }
            interval = Interval;
            callBack = CacheUpdate;
            IsValidHeap = false;


        }

        protected void EnsureCache()
        {
            // cache is created. No worries
            if (cache != null) return;
            total = 0;
            cache = new Dictionary<string, CacheInfo>();
            foreach (var obj in heap.EnumerateObjects())
            {
                string name = heap.GetObjectType(obj).Name;

                CacheInfo info;
                if (!cache.TryGetValue(name, out info))
                {
                    info = new CacheInfo();

                    cache[name] = info;
                }
                ulong size = heap.GetObjectType(obj).GetSize(obj);
                info.Size += size;
                info.SizeSquared += size * size;
                if (size > info.MaxSize) info.MaxSize = size;
                if (size < info.MinSize) info.MinSize = size;
                info.Cache.Add(obj);
                total++;
                if((total % interval) == 0)
                    if (callBack != null)
                    {
                        if (!callBack(total))
                        {
                            cache = null;
                            return;
                        }
                    }
            }
        }

        public static bool WildcardCompare(string Text, string Pattern)
        {
            if (String.IsNullOrEmpty(Pattern))
                return true;
            string[] parts = Pattern.Split('*');
            if (parts.Length > 0)
            {
                if (parts[0].Length > 0 && !Text.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase)) return false;
                if (parts[parts.Length - 1].Length > 0 && !Text.EndsWith(parts[parts.Length - 1], StringComparison.OrdinalIgnoreCase)) return false;

                for (int i = 1; i < parts.Length - 1; i++)
                {
                    if (Text.IndexOf(parts[0], StringComparison.OrdinalIgnoreCase) == -1) return false;
                }
            }
            else
            {
                return String.Compare(Text, Pattern, true) == 0;
            }

            return true;
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
            ClrInstanceField field;
            for (int i = 0; i < fields.Length - 1; i++)
            {
                field = type.GetFieldByName(fields[i]);
                // At this level we cannot have a simple value
                if (!field.IsObjectReference())
                    return null;
                address = (ulong)field.GetFieldValue(address);
                if (address == 0) return null;
                type = heap.GetObjectType(address);

            }
            field = type.GetFieldByName(fields[fields.Length - 1]);
            object value = null;
            value = field.GetFieldValue(address);
            if (field.Type.IsEnum)
                value = field.Type.GetEnumName(value);


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

        public IEnumerable<ThreadObj> EnumerateStackObj(uint OsThreadId=0, string TypePattern = "")
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
