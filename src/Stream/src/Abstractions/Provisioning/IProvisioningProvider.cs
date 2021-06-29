// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Config;

namespace Steeltoe.Stream.Provisioning
{
    /// <summary>
    /// Provisioning SPI that allows the users to provision destinations such as queues and
    /// topics. This SPI will allow the binders to be separated from any provisioning concerns
    /// and only focus on setting up endpoints for sending/receiving messages.
    /// </summary>
    public interface IProvisioningProvider
    {
        /// <summary>
        /// Creates middleware destination on the physical broker for the producer to send data. The implementation is middleware-specific.
        /// </summary>
        /// <param name="name">the name of the producer destination</param>
        /// <param name="options">the producer options</param>
        /// <returns>the provisioned destination</returns>
        IProducerDestination ProvisionProducerDestination(string name, IProducerOptions options);

        /// <summary>
        /// Creates the middleware destination on the physical broker for the consumer to consume data.The implementation is middleware-specific.
        /// </summary>
        /// <param name="name">the name of the consumer destination</param>
        /// <param name="group">the consumer group</param>
        /// <param name="properties">the consumer options</param>
        /// <returns>the provisioned destination</returns>
        IConsumerDestination ProvisionConsumerDestination(string name, string group, IConsumerOptions properties);
    }
}
