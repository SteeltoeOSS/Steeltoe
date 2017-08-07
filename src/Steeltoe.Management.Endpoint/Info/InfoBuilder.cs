using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Info
{
    public class InfoBuilder : IInfoBuilder
    {
        private Dictionary<string, object> info = new Dictionary<string, object>();

        public Dictionary<string, object> Build()
        {
            return info;
        }

        public IInfoBuilder WithInfo(string key, object value)
        {
            if (!string.IsNullOrEmpty(key))
                info[key] = value;
            return this;
        }

        public IInfoBuilder WithInfo(Dictionary<string, object> items)
        {
            if (items != null)
            {
                foreach (var pair in items)
                {
                    info[pair.Key] = pair.Value;
                }
            }
            return this;
        }
    }
}
