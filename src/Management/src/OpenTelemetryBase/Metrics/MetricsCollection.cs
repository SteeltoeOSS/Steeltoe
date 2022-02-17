using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.OpenTelemetry.Metrics
{
    public class MetricsCollection<T> //TODO: Make internal?
         : Dictionary<string, T>
         where T : new()
    {
        public MetricsCollection()
        {
        }

        public new T this[string key]
        {
            get
            {
                if (!ContainsKey(key))
                {
                    base[key] = new T();
                }

                return base[key];
            }
        }
    }
}
