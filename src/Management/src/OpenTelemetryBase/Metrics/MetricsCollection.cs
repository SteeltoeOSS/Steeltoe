// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Steeltoe.Management.OpenTelemetry.Metrics;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class MetricsCollection<T>
    : ConcurrentDictionary<string, T>
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