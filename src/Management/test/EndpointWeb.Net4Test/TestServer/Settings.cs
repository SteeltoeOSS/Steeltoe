// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.EndpointWeb.Test
{
    public class Settings : Dictionary<string, string>
    {
        public Settings()
        {
        }

        public Settings(IDictionary<string, string> dictionary)
            : base(dictionary)
        {
        }

        public Settings Merge(Settings setttings)
        {
            var merged = new Settings(this);

            setttings?.ToList().ForEach((item) =>
            {
                if (merged.ContainsKey(item.Key))
                {
                    merged[item.Key] = item.Value;
                }
                else
                {
                    merged.Add(item.Key, item.Value);
                }
            });
            return merged;
        }
    }
}
