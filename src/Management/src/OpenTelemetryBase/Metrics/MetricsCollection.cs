// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Management.OpenTelemetry.Metrics;

public class MetricsCollection<T>
    : Dictionary<string, T>
    where T : new()
{
    internal MetricsCollection()
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
