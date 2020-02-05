// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
