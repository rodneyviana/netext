/*=========================================================================================================
  The code in this file is heavily based on a public proof-of-concept sample application from
    Microsoft.Diagnostics.Runtime's developer Lee Culver
============================================================================================================*/

using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace NetExt.Shim
{
    public class ClrPrimitiveValue : DynamicObject
    {
        object m_value;
        ClrElementType m_type;

        public ClrPrimitiveValue(object value, ClrElementType type)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            m_value = value;
            m_type = type;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (binder.Type == typeof(string))
                result = m_value.ToString();
            else
                result = m_value;

            return true;
        }

        public object GetValue()
        {
            return m_value;
        }
    }
}
