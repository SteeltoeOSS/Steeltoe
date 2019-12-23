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

using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration
{
    public interface IServicesInfo
    {
        /// <summary>
        /// Retrieves a list of all service instances for all service types
        /// </summary>
        /// <returns>A complete list of service instances known to the application</returns>
        IEnumerable<Service> GetServicesList();

        /// <summary>
        /// Retrieves a list of all service instances of a given service type
        /// </summary>
        /// <param name="serviceType">String value that identifies the service type. May be platform/broker/version dependent</param>
        /// <remarks>Sample values include: p-mysql, azure-mysql-5-7, p-configserver, p.configserver</remarks>
        /// <returns>A list of service instances configured under the given type</returns>
        IEnumerable<Service> GetInstancesOfType(string serviceType);
    }
}
