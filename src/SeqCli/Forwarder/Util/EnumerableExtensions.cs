using System;
using System.Collections.Generic;

namespace Seq.Forwarder.Util
{
    static class EnumerableExtensions
    {
        public static Dictionary<TKey, TValue> ToDictionaryDistinct<T, TKey, TValue>(
            this IEnumerable<T> enumerable, Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
        where TKey: notnull
        {
            var result = new Dictionary<TKey, TValue>();
            foreach (var e in enumerable)
            {
                result[keySelector(e)] = valueSelector(e);
            }
            return result;
        }
    }
}