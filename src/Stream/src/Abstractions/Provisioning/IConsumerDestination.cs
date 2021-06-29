// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Provisioning
{
    /// <summary>
    /// Represents a ConsumerDestination that provides the information about the destination
    /// that is physically provisioned through a provisioning provider
    /// </summary>
    public interface IConsumerDestination
    {
        /// <summary>
        /// Gets the destination name
        /// </summary>
        string Name { get; }
    }
}
