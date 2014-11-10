using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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

        /// <summary>
        /// Break a list of items into chunks of a specific size
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> source, int chunksize)
        {
            while (source.Any())
            {
                yield return source.Take(chunksize);
                source = source.Skip(chunksize);
            }
        }
    }
}
