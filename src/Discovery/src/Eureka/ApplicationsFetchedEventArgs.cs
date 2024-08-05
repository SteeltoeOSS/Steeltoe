// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Provides data for the <see cref="EurekaDiscoveryClient.ApplicationsFetched" /> event.
/// </summary>
public sealed class ApplicationsFetchedEventArgs : EventArgs
{
    public ApplicationInfoCollection Applications { get; }

    public ApplicationsFetchedEventArgs(ApplicationInfoCollection applications)
    {
        ArgumentNullException.ThrowIfNull(applications);

        Applications = applications;
    }
}
