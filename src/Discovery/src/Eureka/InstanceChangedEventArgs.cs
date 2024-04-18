// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Provides access to the new and previous <see cref="InstanceInfo" /> when <see cref="EurekaApplicationInfoManager.Instance" /> has changed.
/// </summary>
internal sealed class InstanceChangedEventArgs : EventArgs
{
    public InstanceInfo NewInstance { get; }
    public InstanceInfo PreviousInstance { get; }

    public InstanceChangedEventArgs(InstanceInfo newInstance, InstanceInfo previousInstance)
    {
        ArgumentGuard.NotNull(newInstance);
        ArgumentGuard.NotNull(previousInstance);

        NewInstance = newInstance;
        PreviousInstance = previousInstance;
    }
}
