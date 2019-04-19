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

using Consul;
using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Consul.Discovery
{
    /// <summary>
    /// A Consul Discovery client
    /// </summary>
    public interface IConsulDiscoveryClient : IDiscoveryClient, IDisposable
    {
        /// <summary>
        /// Get all the instances for the given service id
        /// </summary>
        /// <param name="serviceId">the service to lookup</param>
        /// <param name="queryOptions">any Consul query options to use</param>
        /// <returns>list of found service instances</returns>
        IList<IServiceInstance> GetInstances(string serviceId, QueryOptions queryOptions = null);

        /// <summary>
        /// Get all the instances from the Consul catalog
        /// </summary>
        /// <param name="queryOptions">any Consul query options to use</param>
        /// <returns>list of found service instances</returns>
        IList<IServiceInstance> GetAllInstances(QueryOptions queryOptions = null);

        /// <summary>
        /// Get all of the services from the Consul catalog
        /// </summary>
        /// <param name="queryOptions">any Consul query options to use</param>
        /// <returns>list of found services</returns>
        IList<string> GetServices(QueryOptions queryOptions = null);
    }
}
