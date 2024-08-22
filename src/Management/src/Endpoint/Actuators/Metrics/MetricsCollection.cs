// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics;

internal sealed class MetricsCollection<T> : ConcurrentDictionary<string, T>
{
    public MetricsCollection()
    {
    }

    public MetricsCollection(IEnumerable<KeyValuePair<string, T>> source)
        : base(source)
    {
    }
}
