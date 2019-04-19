using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Steeltoe.Management.Census.Utils
{
    [Obsolete("Use OpenCensus project packages")]
    public static class Collections
    {
        public static string ToString<K, V>(IDictionary<K, V> dict)
        {
            if (dict == null)
            {
                return "null";
            }
            StringBuilder sb = new StringBuilder();
            foreach (var kvp in dict)
            {
                sb.Append(kvp.Key.ToString());
                sb.Append("=");
                sb.Append(kvp.Value.ToString());
                sb.Append(" ");
            }
            return sb.ToString();
        }

        public static string ToString<V>(IList<V> list)
        {
            if (list == null)
            {
                return "null";
            }
            StringBuilder sb = new StringBuilder();
            foreach (var val in list)
            {
                if (val != null)
                {
                    sb.Append(val.ToString());
                    sb.Append(" ");
                }
            }
            return sb.ToString();
        }

        public static bool AreEquivalent<T>(IList<T> c1, IList<T> c2)
        {
            var c1Dist = c1.Distinct();
            var c2Dist = c2.Distinct();
            return c1.Count == c2.Count && c1Dist.Count() == c2Dist.Count() && c1Dist.Intersect(c2Dist).Count() == c1Dist.Count();
        }
    }
    
}

  