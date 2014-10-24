using System;
using System.Collections.Generic;
using System.Text;

namespace Discouser
{
    static class Utility
    {
        /// <summary>
        /// Tuple.Create for kvp
        /// </summary>
        public static KeyValuePair<TName, TValue> KeyValuePair<TName, TValue>(TName name, TValue value)
        {
            return new KeyValuePair<TName, TValue>(name, value);
        }
    }
}
