// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Provisioning
{
    /// <summary>
    /// Represents a ProducerDestination that provides the information about the destination
    /// that is physically provisioned through a provisioning provider
    /// </summary>
    public interface IProducerDestination
    {
        /// <summary>
        /// Gets the destination name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Provides the destination name for a given partition.
        /// </summary>
        /// <param name="partition">the partition to find a name for</param>
        /// <returns>the destination name for the partition</returns>
        string GetNameForPartition(int partition);
    }
}
