using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace JBlam.HarClient
{
    static class CollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> content)
        {
            foreach (var item in content)
            {
                collection.Add(item);
            }
        }
        public static void AddRange(this HttpHeaders collection, IEnumerable<KeyValuePair<string, IEnumerable<string>>> content)
        {
            foreach (var item in content)
            {
                collection.Add(item.Key, item.Value);
            }
        }
    }
}
