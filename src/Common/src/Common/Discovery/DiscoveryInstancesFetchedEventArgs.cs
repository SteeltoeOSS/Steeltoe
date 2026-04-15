// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Discovery;

/// <summary>
/// Provides data for the <see cref="IDiscoveryClient.InstancesFetched" /> event.
/// </summary>
public sealed class DiscoveryInstancesFetchedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the updated list of service instances, grouped by service ID.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<IServiceInstance>> InstancesByServiceId { get; }

    public DiscoveryInstancesFetchedEventArgs(IReadOnlyDictionary<string, IReadOnlyList<IServiceInstance>> instancesByServiceId)
    {
        ArgumentNullException.ThrowIfNull(instancesByServiceId);

        InstancesByServiceId = instancesByServiceId;
    }
}
