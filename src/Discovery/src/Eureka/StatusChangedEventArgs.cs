// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

public class StatusChangedEventArgs : EventArgs
{
    public InstanceStatus Previous { get; private set; }

    public InstanceStatus Current { get; private set; }

    public string InstanceId { get; private set; }

    public StatusChangedEventArgs(InstanceStatus previous, InstanceStatus current, string instanceId)
    {
        Previous = previous;
        Current = current;
        InstanceId = instanceId;
    }
}
