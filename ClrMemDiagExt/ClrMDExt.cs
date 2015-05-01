/*=========================================================================================================
  The code in this file is heavily based on a public proof-of-concept sample application from
    Microsoft.Diagnostics.Runtime's developer Lee Culver
============================================================================================================*/

using Microsoft.Diagnostics.Runtime;
using System.Linq;

namespace NetExt.Shim
{
    public static class ClrMemDiagExtensions
    {
        public static dynamic GetDynamicObject(this ClrHeap heap, ulong addr)
        {
            var type = heap.GetObjectType(addr);
            if (type == null)
                return null;

            return new ClrObject(heap, type, addr);
        }

        public static dynamic GetDynamicClass(this ClrHeap heap, string typeName)
        {
            ClrType type = (from t in heap.EnumerateTypes()
                               where t != null && t.Name == typeName
                               select t).FirstOrDefault();

            if (type == null)
                return null;

            return new ClrDynamicClass(heap, type);
        }
    }
}